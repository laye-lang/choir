using System.Diagnostics;
using System.Runtime.InteropServices;

using Choir.CommandLine;
using Choir.Driver.Options;
using Choir.Front.Laye;
using Choir.Front.Laye.Codegen;
using Choir.Front.Laye.Sema;
using Choir.Front.Laye.Syntax;

using LLVMSharp.Interop;

namespace Choir.Driver;

public sealed class LayecHighLevelDriver
    : BaseLayeDriver<LayecHighLevelDriverOptions, BaseLayeCompilerDriverArgParseState>
{
    private const string DriverVersion = @"{0} version 0.1.0";

    private const string DriverOptions = @"Compiles a list of Laye source files of the same module into a module object file

Usage: {0} [options] file...

Options:
    --help                   Display this information
    --version                Display compiler version information
    --verbose                Emit additional information about the compilation to stderr
    --color <arg>            Specify how compiler output should be colored
                             one of: 'auto', 'always', 'never'

    -i                       Read source text from stdin rather than a list of source files
    --file-kind <kind>       Specify the kind of subsequent input files, or 'default' to infer it from the extension
                             one of: 'default', 'laye', 'module'
    -o <path>                Override the output module object file path
                             To emit output to stdout, specify a path of '-'
                             default: '<module-name>.mod'
    --emit-llvm              Emit LLVM IR instead of Assembler when compiling with `--compile`.

    --no-corelib             Do not link against the the default Laye core libraries
                             This also implies '--no-stdlib'

    -L <lib-dir>             Adds <dir> to the library search list.
                             Directories are searched in the order they are provided, and values
                             provided through the CLI are searched after built-in and environment-
                             specified search paths.

    --lex                    Only read tokens from the source files, then exit
    --parse                  Only lex and parse the source files, then exit
    --sema                   Only lex, parse and analyse the source files, then exit
    --codegen                Only lex, parse, analyse and generate code for the source files, then exit
    --compile                Only lex, parse, analyse, generate and emit code for the source files, then exit

    --tokens                 Print token information to stderr when used alongside `--lex`
    --ast                    Print ASTs to stderr when used alongside `--parse` or `--sema`
    --no-lower               Do not lower the AST during semantic analysis when used alongside `--sema`
    --ir                     Print IR to stderr when used alongside `--codegen`";

    public static int RunWithArgs(StreamingDiagnosticWriter diag, string[] args, string programName = "layec")
    {
        var options = LayecHighLevelDriverOptions.Parse(diag, new CliArgumentIterator(args));
        if (diag.HasIssuedErrors)
            return 1;

        if (options.ShowVersion)
        {
            Console.WriteLine(string.Format(DriverVersion, programName));
            return 0;
        }

        if (options.ShowHelp)
        {
            Console.WriteLine(string.Format(DriverOptions, programName));
            return 0;
        }

        if (options.ModuleSourceFiles.Count == 0)
        {
            diag.Error("No input source files.");
            diag.Flush();
            return 1;
        }

        var driver = Create(diag, options);
        int exitCode = driver.Execute();

        return exitCode;
    }

    public static LayecHighLevelDriver Create(DiagnosticWriter diag, LayecHighLevelDriverOptions options, string programName = "layec")
    {
        return new LayecHighLevelDriver(programName, diag, options);
    }

    public static LayecHighLevelDriver Create(ChoirContext context, LayecHighLevelDriverOptions options, string programName = "layec")
    {
        return new LayecHighLevelDriver(programName, options, context);
    }

    private LayecHighLevelDriver(string programName, DiagnosticWriter diag, LayecHighLevelDriverOptions options)
        : base(programName, diag, options)
    {
    }

    internal LayecHighLevelDriver(string programName, LayecHighLevelDriverOptions options, ChoirContext context)
        : base(programName, context, options)
    {
    }

    private FileInfo? FindModuleBinaryFile(string libraryName)
    {
        string libraryFileName = $"{libraryName}.mod";
        foreach (var libDir in Options.LibrarySearchPaths)
        {
            var modFile = libDir.ChildFile(libraryFileName);
            if (modFile.Exists) return modFile;
        }

        Context.Diag.Error($"Could not find Laye module file '{libraryFileName}'.");
        return null;
    }

    public override int Execute()
    {
        Context.LogVerbose(string.Format(DriverVersion, ProgramName));

        DirectoryInfo[] allLibrarySearchPaths = [.. CollectBuiltInLibrarySearchPaths(), .. Options.LibrarySearchPaths];
        Options.LibrarySearchPaths.Clear();
        Options.LibrarySearchPaths.AddRange(allLibrarySearchPaths.Where(dir => dir.Exists).Select(d => d.Canonical()).Distinct());

        Context.LogVerbose("Configured library search paths:");
        foreach (var libDir in Options.LibrarySearchPaths)
            Context.LogVerbose("  " + libDir.FullName);

        if (!Options.NoCoreLibrary)
        {
            if (FindModuleBinaryFile("core") is { } corelibFileInfo)
                Options.BinaryDependencyFiles.Add(corelibFileInfo);
            else Context.Assert(Context.HasIssuedError, "Should have errored when the compiler could not find a library it ships with.");
        }

        Context.LogVerbose("Input module binaries:");
        foreach (var file in Options.BinaryDependencyFiles)
            Context.LogVerbose($"  {file.FullName}");

        if (Context.HasIssuedError)
            return 1;

        var sourceFiles = Options.ModuleSourceFiles.Select(Context.GetSourceFile).ToArray();
        var sourceHeaders = sourceFiles.Select(Parser.ParseModuleUnitHeader).ToArray();

        var sourceModule = CreateSourceModuleFromLayeSourceFiles(null, Options.ModuleSourceFiles);
        if (sourceModule is null)
        {
            Context.Assert(Context.HasIssuedError, "Should have errored if we didn't create a source module");
            return 1;
        }

        var modules = ResolveModuleDependencyOrder([], [], [sourceModule], [.. Options.BinaryDependencyFiles], [.. Options.LibrarySearchPaths]);
        
        var dependencyInfos = modules.TakeWhile(m => !m.Equals(sourceModule)).Cast<BinaryModuleInfo>().ToArray();

        var dependencies = new LayeModule[dependencyInfos.Length];
        for (int i = 0; i < dependencies.Length; i++)
        {
            var di = dependencyInfos[i];
            var prev = dependencies.Take(i)
                .Where(d => di.DependencyNames.Contains(d.ModuleName));

            dependencies[i] = LayeModule.DeserializeFromObject(Context, [.. prev], di.ModuleFile);
        }

        var module = new LayeModule(Context, sourceFiles, dependencies);
        var syntaxPrinter = new SyntaxPrinter(Context, false);
        var semaPrinter = new SemaPrinter(Context, false);

        #region Total Driver Flow

        if (Options.DriverStage == DriverStage.Lex)
            return LexOnly();

        var unitDecls = new SyntaxDeclModuleUnit[module.SourceFiles.Count];
        for (int i = 0; i < module.SourceFiles.Count; i++)
        {
            var sourceFile = module.SourceFiles[i];
            unitDecls[i] = Parser.ParseModuleUnit(sourceFile);
        }

        if (Context.Diag.HasIssuedErrors) return 1;

        string[] declaredModuleNames = unitDecls.Select(decl => decl.Header.ModuleName).Where(name => name is not null).Cast<string>().Distinct().ToArray();
        if (declaredModuleNames.Length == 1)
            module.ModuleName = declaredModuleNames[0];
        else if (declaredModuleNames.Length != 0)
        {
            Context.Diag.Error("Source files do not all declare the same module.");
            return 1;
        }

        if (Options.DriverStage == DriverStage.Parse)
            return ParseOnly();

        Sema.AnalyseModule(module, unitDecls);

        if (Context.Diag.HasIssuedErrors) return 1;

        if (Options.DriverStage == DriverStage.Sema)
            return SemaOnly();

        var llvmModule = LayeCodegen.GenerateIR(module);

        if (Context.Diag.HasIssuedErrors) return 1;

        if (Options.DriverStage == DriverStage.Codegen)
            return CodegenOnly();

        if (Options.DriverStage == DriverStage.Compile)
            return CompileOnly();

        try
        {
            string tempFilePath = Path.GetTempFileName();
            EmitLLVMModuleToFile(tempFilePath, LLVMCodeGenFileType.LLVMObjectFile);

            if (Options.OutputFilePath == "-")
            {
                using var stdout = Console.OpenStandardOutput();
                stdout.Write(File.ReadAllBytes(tempFilePath));
                File.Delete(tempFilePath);
            }
            else File.Move(tempFilePath, GetOutputFilePath(isObject: true), true);
        }
        catch (Exception ex)
        {
            Context.Diag.ICE($"Failed to generate LLVM IR text: {ex.Message}");
            return 1;
        }

        return 0;

        #endregion

        #region Specific Driver Stage Implementations

        int LexOnly()
        {
            var tokens = new Dictionary<SourceFile, SyntaxToken[]>();
            for (int i = 0; i < module.SourceFiles.Count; i++)
            {
                var sourceFile = module.SourceFiles[i];
                var lexer = new Lexer(sourceFile);

                var unitTokens = new List<SyntaxToken>();
                SyntaxToken token;
                do
                {
                    token = lexer.ReadToken();
                    unitTokens.Add(token);
                } while (token.Kind != TokenKind.EndOfFile);
            }

            if (Context.Diag.HasIssuedErrors) return 1;

            if (Options.PrintTokens)
            {
                for (int i = 0; i < module.SourceFiles.Count; i++)
                {
                    if (i > 0 && Options.PrintTokens) Console.Error.WriteLine();

                    var sourceFile = module.SourceFiles[i];
                    Console.Error.WriteLine($"{sourceFile.FilePath}");

                    foreach (var token in tokens[sourceFile])
                        syntaxPrinter.PrintToken(token);
                }
            }

            return 0;
        }

        int ParseOnly()
        {
            if (Options.PrintAst)
            {
                for (int i = 0; i < unitDecls.Length; i++)
                {
                    if (i > 0) Console.Error.WriteLine();

                    Console.Error.WriteLine($"{unitDecls[i].SourceFile.FilePath}");
                    syntaxPrinter.PrintSyntax(unitDecls[i]);
                }
            }

            return 0;
        }

        int SemaOnly()
        {
            if (Options.PrintAst)
            {
                semaPrinter.PrintModule(module);
            }

            return 0;
        }

        int CodegenOnly()
        {
            if (Options.PrintIR)
            {
                llvmModule.Dump();
            }

            return 0;
        }

        int CompileOnly()
        {
            try
            {
                string tempFilePath = Path.GetTempFileName();
                if (Options.AssemblerFormat == AssemblerFormat.Assembler)
                    EmitLLVMModuleToFile(tempFilePath, LLVMCodeGenFileType.LLVMAssemblyFile);
                else PrintLLVMModuleToFile(tempFilePath);

                if (Options.OutputFilePath == "-")
                {
                    Console.Write(File.ReadAllText(tempFilePath));
                    File.Delete(tempFilePath);
                }
                else File.Move(tempFilePath, GetOutputFilePath(isObject: false), true);
            }
            catch (Exception ex)
            {
                Context.Diag.ICE($"Failed to emit assembler file: {ex.Message}");
                return 1;
            }

            return 0;
        }

        string GetOutputFilePath(bool isObject)
        {
            Context.Assert(Options.OutputFilePath != "-", "Handle output to stdio separately.");

            string? objectFilePath = Options.OutputFilePath;
            if (objectFilePath is null)
            {
                string objectFileName = module.ModuleName is LayeConstants.ProgramModuleName ? "a" : module.ModuleName;
                if (isObject)
                    objectFilePath = $"{objectFileName}.mod";
                else objectFilePath = Options.AssemblerFormat == AssemblerFormat.LLVM ? $"{objectFileName}.ll" : $"{objectFileName}.s";
            }

            return objectFilePath;
        }

        void PrintLLVMModuleToFile(string outputFilePath)
        {
            File.Delete(outputFilePath);
            if (!llvmModule.TryPrintToFile(outputFilePath, out string printError))
            {
                Context.Diag.ICE($"Failed to print LLVM IR to file '{outputFilePath}': {printError}");
            }
        }

        void EmitLLVMModuleToFile(string outputFilePath, LLVMCodeGenFileType fileType = LLVMCodeGenFileType.LLVMObjectFile)
        {
            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();

            var target = LLVMTargetRef.GetTargetFromTriple(LLVMTargetRef.DefaultTriple);
            var machine = target.CreateTargetMachine(LLVMTargetRef.DefaultTriple, "generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelNone, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);

            if (!machine.TryEmitToFile(llvmModule, outputFilePath, fileType, out string message))
            {
                Context.Diag.ICE($"LLVM Emit to File Error: {message}");
            }
        }

        #endregion
    }
}
