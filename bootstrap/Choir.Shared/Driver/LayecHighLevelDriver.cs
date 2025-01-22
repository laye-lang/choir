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

    public string ProgramName { get; }
    public LayecHighLevelDriverOptions Options { get; }
    public ChoirContext Context { get; }

    private LayecHighLevelDriver(string programName, DiagnosticWriter diag, LayecHighLevelDriverOptions options)
    {
        ProgramName = programName;
        Options = options;

        var abi = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ChoirAbi.WindowxX64 : ChoirAbi.SysV;
        Context = new(diag, ChoirTarget.X86_64, abi, options.OutputColoring)
        {
            EmitVerboseLogs = options.ShowVerboseOutput,
            OmitSourceTextInModuleBinary = options.OmitSourceTextInModuleBinary,
        };
    }

    internal LayecHighLevelDriver(string programName, LayecHighLevelDriverOptions options, ChoirContext context)
    {
        ProgramName = programName;
        Options = options;
        Context = context;
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

    private bool LoadDependencies(SyntaxDeclModuleUnitHeader[] sourceHeaders, out LayeModule[] dependencies)
    {
        dependencies = [];

        HashSet<string> unitDependencies = [];
        string unitName = sourceHeaders.FirstOrDefault()?.ModuleName ?? ".program";

        for (int i = 0; i < sourceHeaders.Length; i++)
        {
            var sourceHeaderSyntax = sourceHeaders[i];

            foreach (var import in sourceHeaderSyntax.ImportDeclarations)
                unitDependencies.Add(import.ModuleNameText);
        }

        var dependencyFiles = new List<FileInfo>(Options.BinaryDependencyFiles);

    redo_dependency_checks:;
        var moduleHeaders = dependencyFiles
            .Select(file => LayeModule.DeserializeHeaderFromObject(Context, file)).ToArray();

        for (int i = 0; i < moduleHeaders.Length; i++)
        {
            if (moduleHeaders[i].ModuleName == ".program")
            {
                Context.Diag.Error("A program module cannot be a dependency.");
                return false;
            }
        }

        string[] allDependencyNames = [.. unitDependencies, .. moduleHeaders.SelectMany(mh => mh.DependencyNames)];
        string[] missingDependencyNames = allDependencyNames
            .Where(n => !dependencyFiles.Any(df => Path.GetFileNameWithoutExtension(df.Name) == n))
            .ToArray();

        foreach (string missing in missingDependencyNames)
        {
            if (FindModuleBinaryFile(missing) is { } missingModuleBinary)
                dependencyFiles.Add(missingModuleBinary);
        }

        if (Context.HasIssuedError)
            return false;

        if (missingDependencyNames.Length != 0)
            goto redo_dependency_checks;

        var moduleNamesToDependencies = moduleHeaders
            .ToDictionary(header => header.ModuleName!, header => header.DependencyNames);

        var moduleNamesToFiles = dependencyFiles.Zip(moduleHeaders)
            .ToDictionary(pair => pair.Second.ModuleName!, pair => pair.First);

        Context.LogVerbose("Build dependency files:");
        foreach (var (dep, depDeps) in moduleNamesToDependencies)
        {
            Context.LogVerbose($"  {moduleNamesToFiles[dep].FullName} [{string.Join(", ", depDeps)}]");
        }

        HashSet<string> allModuleNames = [];
        foreach (var desc in moduleHeaders)
        {
            allModuleNames.Add(desc.ModuleName);
            foreach (string dependencyName in desc.DependencyNames)
                allModuleNames.Add(dependencyName);
        }

        List<(string From, string To)> dependencyEdges = [];
        foreach (var desc in moduleHeaders)
        {
            foreach (string dependencyName in desc.DependencyNames)
                dependencyEdges.Add((desc.ModuleName, dependencyName));
        }

        var sortResult = TopologicalSort.Sort(allModuleNames, dependencyEdges);
        if (sortResult is TopologicalSortCircular<string> circular)
        {
            Context.Diag.Error($"Detected circular dependencies: from module '{circular.From}' to '{circular.To}'.");
            if (!Context.EmitVerboseLogs)
                Context.Diag.Note($"To see the modules being referred to by the compiler, add the '--verbose' command line argument.");
            return false;
        }

        if (sortResult is not TopologicalSortSuccess<string> sorted)
        {
            Context.Diag.ICE("Only circular references and sorted lists are expected after dependency sorting.");
            throw new UnreachableException();
        }

        Context.LogVerbose("Build dependency names, sorted:");
        foreach (string depname in sorted.Sorted)
        {
            Context.LogVerbose($"  {depname}");
        }

        //Context.LogVerbose("dependencies:");
        dependencies = new LayeModule[sorted.Sorted.Length];
        for (int i = 0; i < dependencies.Length; i++)
        {
            string moduleName = sorted.Sorted[i];
            if (!moduleNamesToFiles.TryGetValue(moduleName, out var moduleFile))
            {
                Context.Diag.ICE($"A dependency (module '{moduleName}') did not have an associated binary file.");
                return false;
            }

            if (!moduleNamesToDependencies.TryGetValue(moduleName, out var moduleDependencyNames))
            {
                Context.Diag.ICE($"A dependency (module '{moduleName}') did not have associated dependencies.");
                return false;
            }

            var moduleDependencies = dependencies.Take(i)
                .Where(d => moduleDependencyNames.Contains(d.ModuleName!))
                .ToArray();

            Context.Assert(moduleDependencies.Length == moduleDependencyNames.Count, $"Failed to map a list of dependency names for module '{moduleName}' ({(string.Join(", ", moduleDependencyNames.Select(n => $"'{n}'")))}) to their loaded modules. Expected {moduleDependencyNames.Count} dependencies, but found {moduleDependencies.Length}.");
            dependencies[i] = LayeModule.DeserializeFromObject(Context, moduleDependencies, moduleFile);

            //Context.LogVerbose($"  dependencies[i].ModuleName = {dependencies[i].ModuleName}");
        }

        return true;
    }

    private void CollectCompilerSearchPaths()
    {
        var builtInSearchPaths = new List<DirectoryInfo>();

        string envName = "Lib";
        string envSplit = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";
        string[] pathEntries = Environment.GetEnvironmentVariable(envName)?.Split(envSplit) ?? [];

        builtInSearchPaths.AddRange(pathEntries.Where(path => !path.IsNullOrEmpty()).Select(path => new DirectoryInfo(path)));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            builtInSearchPaths.Add(new DirectoryInfo("/usr/local/lib/laye"));
            builtInSearchPaths.Add(new DirectoryInfo("/usr/local/lib"));
            //builtInSearchPaths.Add(new DirectoryInfo($"/usr/lib/{Triple}"));
            builtInSearchPaths.Add(new DirectoryInfo("/usr/lib/laye"));
            builtInSearchPaths.Add(new DirectoryInfo("/usr/lib"));
            //builtInSearchPaths.Add(new DirectoryInfo($"/lib/{Triple}"));
        }

        string selfExePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        DirectoryInfo? searchDirectoryRoot = new DirectoryInfo(Path.GetDirectoryName(selfExePath)!);

        while (searchDirectoryRoot is not null)
        {
            builtInSearchPaths.Add(searchDirectoryRoot.ChildDirectory("lib").ChildDirectory("laye"));
            builtInSearchPaths.Add(searchDirectoryRoot.ChildDirectory("lib"));
            searchDirectoryRoot = searchDirectoryRoot.Parent;
        }

        DirectoryInfo[] allPaths = [.. builtInSearchPaths, .. Options.LibrarySearchPaths];
        Options.LibrarySearchPaths.Clear();
        Options.LibrarySearchPaths.AddRange(allPaths.Where(dir => dir.Exists).Distinct());
    }

    public int Execute()
    {
        Context.LogVerbose(string.Format(DriverVersion, ProgramName));

        CollectCompilerSearchPaths();

        Context.LogVerbose("Configured library search paths:");
        foreach (var libDir in Options.LibrarySearchPaths)
            Context.LogVerbose("  " + libDir.FullName);

        if (!Options.NoCoreLibrary)
        {
            if (FindModuleBinaryFile("core") is { } corelibFileInfo)
                Options.BinaryDependencyFiles.Add(corelibFileInfo);
            else Context.Assert(Context.HasIssuedError, "Should have errored when the compiler could not find a library it ships with.");
        }

        if (Context.HasIssuedError)
            return 1;

        var sourceFiles = Options.ModuleSourceFiles.Select(Context.GetSourceFile).ToArray();
        var sourceHeaders = sourceFiles.Select(Parser.ParseModuleUnitHeader).ToArray();

        if (!LoadDependencies(sourceHeaders, out var dependencies))
            return 1;

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
