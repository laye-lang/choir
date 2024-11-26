using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Diagnostics.Metrics;

using Choir.CommandLine;
using Choir.Front.Laye;
using Choir.Front.Laye.Codegen;
using Choir.Front.Laye.Sema;
using Choir.Front.Laye.Syntax;

using LLVMSharp.Interop;

namespace Choir.Driver;

public sealed class LayecDriver
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

    --no-corelib             Do not link against the the default Laye core library
                             This also implies '--no-stdlib'
    --no-stdlib              Do not link against the default Laye standard library

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
        var options = LayecDriverOptions.Parse(diag, new CliArgumentIterator(args));
        if (diag.HasIssuedErrors)
        {
            diag.Flush();
            return 1;
        }

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

        var driver = Create(diag, options);
        int exitCode = driver.Execute();

        diag.Flush();
        return exitCode;
    }

    public static LayecDriver Create(DiagnosticWriter diag, LayecDriverOptions options, string programName = "layec")
    {
        return new LayecDriver(programName, diag, options);
    }

    public string ProgramName { get; }
    public LayecDriverOptions Options { get; }
    public ChoirContext Context { get; }

    private LayecDriver(string programName, DiagnosticWriter diag, LayecDriverOptions options)
    {
        ProgramName = programName;
        Options = options;
        Context = new(diag, ChoirTarget.X86_64, options.OutputColoring);
    }

    public void LogVerbose(string message)
    {
        if (!Options.ShowVerboseOutput)
            return;

        Console.Error.WriteLine(message);
    }

    private bool LoadDependencies(out LayeModule[] dependencies)
    {
        dependencies = [];

        var moduleHeaders = Options.BinaryDependencyFiles
            .Select(file => LayeModule.DeserializeHeaderFromObject(Context, file)).ToList();

        for (int i = 0; i < moduleHeaders.Count; i++)
        {
            if (moduleHeaders[i].ModuleName is null)
            {
                Context.Diag.Error("Detected circular dependencies.");
                return false;
            }
        }

        var moduleNamesToDependencies = moduleHeaders
            .ToDictionary(header => header.ModuleName!, header => header.DependencyNames);

        var moduleNamesToFiles = Options.BinaryDependencyFiles.Zip(moduleHeaders)
            .ToDictionary(pair => pair.Second.ModuleName!, pair => pair.First);

        HashSet<string> allModuleNames = [];
        foreach ((string? moduleName, string[] dependencyNames) in moduleHeaders)
        {
            allModuleNames.Add(moduleName!);
            foreach (string dependencyName in dependencyNames)
                allModuleNames.Add(dependencyName);
        }

        List<(string From, string To)> dependencyEdges = [];
        foreach ((string? moduleName, string[] dependencyNames) in moduleHeaders)
        {
            foreach (string dependencyName in dependencyNames)
                dependencyEdges.Add((moduleName!, dependencyName));
        }

        var sortResult = TopologicalSort.Sort(allModuleNames, dependencyEdges);
        if (sortResult.CircularDependencies)
        {
            // TODO(local): report discovered circular dependencies
            Context.Diag.Error("Detected circular dependencies.");
            return false;
        }

        dependencies = new LayeModule[sortResult.Sorted.Length];
        for (int i = 0; i < dependencies.Length; i++)
        {
            string moduleName = sortResult.Sorted[i];
            if (!moduleNamesToFiles.TryGetValue(moduleName, out var moduleFile))
            {
                Context.Diag.ICE($"A dependency (module '{moduleName}') did not have an associated binary file.");
                return false;
            }

            if (!moduleNamesToDependencies.TryGetValue(moduleName, out string[]? moduleDependencyNames))
            {
                Context.Diag.ICE($"A dependency (module '{moduleName}') did not have an associated dependencies.");
                return false;
            }

            var moduleDependencies = dependencies.Take(i)
                .Where(d => moduleDependencyNames.Contains(d.ModuleName!))
                .ToArray();

            Context.Assert(moduleDependencies.Length == moduleDependencyNames.Length, $"Failed to map a list of dependency names ({(string.Join(", ", moduleDependencyNames.Select(n => $"'{n}'")))}) to their loaded modules.");
            dependencies[i] = LayeModule.DeserializeFromObject(Context, moduleDependencies, moduleFile);
        }

        return true;
    }

    public int Execute()
    {
        LogVerbose(string.Format(DriverVersion, ProgramName));

        if (!LoadDependencies(out var dependencies))
            return 1;

        var sourceFiles = Options.ModuleSourceFiles.Select(Context.GetSourceFile);

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

        Sema.AnalyseModule(module, unitDecls, []);

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

            if (Options.ObjectFilePath == "-")
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
                    Console.Error.WriteLine($"{sourceFile.FileInfo.FullName}");

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

                    Console.Error.WriteLine($"{unitDecls[i].SourceFile.FileInfo.FullName}");
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

                if (Options.ObjectFilePath == "-")
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
            Context.Assert(Options.ObjectFilePath != "-", "Handle output to stdio separately.");

            string? objectFilePath = Options.ObjectFilePath;
            if (objectFilePath is null)
            {
                string objectFileName = module.ModuleName ?? "a";
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
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllAsmParsers();
            LLVM.InitializeAllAsmPrinters();

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

public sealed record class LayecDriverOptions
{
    /// <summary>
    /// The `--version` flag.
    /// When specified, the driver prints the program version, then exits.
    /// </summary>
    public bool ShowVersion { get; set; }

    /// <summary>
    /// The `--help` flag.
    /// When specified, the driver prints the help text, then exits.
    /// </summary>
    public bool ShowHelp { get; set; }

    /// <summary>
    /// The `--verbose` flag.
    /// Allows emitting verbose information about the compilation to stderr.
    /// </summary>
    public bool ShowVerboseOutput { get; set; }

    /// <summary>
    /// True if the compiler output should be colored.
    /// Determined by the `--color` flag.
    /// </summary>
    public bool OutputColoring { get; set; }

    /// <summary>
    /// Every input source file is treated as a Laye source file of the same semantic module.
    /// `layec` will validate that every source file begins with a `module` directive and that the directives specify the same module name.
    /// The only exception to this rule is that executables may not specify a module name, so long as every program source file does the same.
    /// Since `layec` does not know which other modules you are building and linking against, this is not strictly enforced.
    /// You *may* build modules manually which have no module declarations in them using `layec`, though it is discouraged.
    /// A compiler driver or build system may emit an error if you explicitly output libraries which do not have module directives in them.
    /// 
    /// Other files may be passed to `layec` if they are of known binary output formats so that library information can be extracted from them and are stored separately.
    /// </summary>
    public List<FileInfo> ModuleSourceFiles { get; } = [];

    /// <summary>
    /// Additional binary files that the input source code expects to link against in the future.
    /// `layec` will read Laye metadata from these files to perform smenatic analysis against.
    /// </summary>
    public List<FileInfo> BinaryDependencyFiles { get; } = [];

    /// <summary>
    /// The `-i` flag.
    /// Specifies that the contents of stdin are the only input source text rather than explicitly provided file paths.
    /// </summary>
    public bool ReadFromStdIn { get; set; } = false;

    /// <summary>
    /// The name of the generated object file.
    /// 
    /// `layec` can only produce object files; it is not a linker and does not delegate work to one.
    /// It is assumed the invoker, a build system or a compiler driver will delegate work to a linker.
    /// </summary>
    public string? ObjectFilePath { get; set; }

    /// <summary>
    /// The format of assembler output when compiling with the `--compile` flag.
    /// Defaults to traditional assembler code, but can be switched to LLVM IR with the `--emit-llvm` flag.
    /// </summary>
    public AssemblerFormat AssemblerFormat { get; set; } = AssemblerFormat.Assembler;

    /// <summary>
    /// The `--no-corelib` flag.
    /// Disables linking to the Laye core library, requiring the programmer to provide their own implementation.
    /// `layec` does not handle linking itself, but it does ensure the default libraries are referenced by default and they are expected to be available when linking occurs.
    /// </summary>
    public bool NoCoreLibrary { get; set; }

    /// <summary>
    /// The `--no-stdlib` flag.
    /// Disables linking to the Laye standard library, requiring the programmer to provide their own implementation if desired.
    /// `layec` does not handle linking itself, but it does ensure the default libraries are referenced by default and they are expected to be available when linking occurs.
    /// </summary>
    public bool NoStandardLibrary { get; set; }

    /// <summary>
    /// Through what stage the driver should run.
    /// By default, a call to `layec` will generate an object file for the target system.
    /// Only the <see cref="DriverStage.Lex"/> (--lex), <see cref="DriverStage.Parse"/> (--parse), <see cref="DriverStage.Sema"/>, (--sema), <see cref="DriverStage.Codegen"/> (--codegen), <see cref="DriverStage.Compile"/> (--compile) and <see cref="DriverStage.Assemble"/> stages are supported.
    /// When a specific driver stage is selected, any alternate output forms it supports may also be set; any alternate output form which does not apply to the set driver stage is ignored.
    /// </summary>
    public DriverStage DriverStage { get; set; } = DriverStage.Assemble;

    /// <summary>
    /// The `--tokens` compiler flag, determining if tokens should be printed when running only the <see cref="DriverStage.Lex"/> stage.
    /// </summary>
    public bool PrintTokens { get; set; } = false;

    /// <summary>
    /// The `--ast` compiler flag, determining if the AST should be printed when running only the <see cref="DriverStage.Parse"/> or <see cref="DriverStage.Sema"/> stages.
    /// </summary>
    public bool PrintAst { get; set; } = false;

    /// <summary>
    /// The `--ir` compiler flag, determining if the IR should be printed when running only the <see cref="DriverStage.Codegen"/> stage.
    /// </summary>
    public bool PrintIR { get; set; } = false;

    /// <summary>
    /// The `--no-lower` compiler flag, determining if lowering should be skipped only during the <see cref="DriverStage.Sema"/> stage.
    /// </summary>
    public bool NoLower { get; set; } = false;

    public static LayecDriverOptions Parse(DiagnosticWriter diag, CliArgumentIterator args)
    {
        var options = new LayecDriverOptions();

        if (args.RemainingCount == 0)
        {
            options.ShowHelp = true;
            return options;
        }

        var currentFileType = InputFileLanguage.Default;
        var outputColoring = Driver.OutputColoring.Auto;

        while (args.Shift(out string arg))
        {
            switch (arg)
            {
                default:
                {
                    var inputFileInfo = new FileInfo(arg);
                    if (!inputFileInfo.Exists)
                        diag.Error($"No such file or directory '{arg}'.");

                    var inputFileType = currentFileType;
                    if (inputFileType == InputFileLanguage.Default)
                    {
                        string inputFileExtension = inputFileInfo.Extension;
                        switch (inputFileExtension.ToLower())
                        {
                            case ".laye": inputFileType = InputFileLanguage.LayeSource; break;
                            case ".mod": inputFileType = InputFileLanguage.LayeModule; break;

                            default:
                            {
                                diag.Error($"File extension '{inputFileExtension}' not recognized; use `--file-kind <kind> to manually specify.");
                                inputFileType = InputFileLanguage.LayeModule;
                            } break;
                        }
                    }

                    if (inputFileType == InputFileLanguage.LayeSource)
                        options.ModuleSourceFiles.Add(inputFileInfo);
                    else
                    {
                        Debug.Assert(inputFileType == InputFileLanguage.LayeModule);
                        options.BinaryDependencyFiles.Add(inputFileInfo);
                    }
                } break;

                case "--help": options.ShowHelp = true; break;
                case "--version": options.ShowVersion = true; break;
                case "--verbose": options.ShowVerboseOutput = true; break;

                case "--color":
                {
                    if (!args.Shift(out string? color))
                        diag.Error($"Argument to '{arg}' is missing; expected 1 value.");
                    else
                    {
                        switch (color.ToLower())
                        {
                            default: diag.Error($"Color mode '{color}' not recognized."); break;
                            case "auto": outputColoring = Driver.OutputColoring.Auto; break;
                            case "always": outputColoring = Driver.OutputColoring.Always; break;
                            case "never": outputColoring = Driver.OutputColoring.Never; break;
                        }
                    }
                } break;

                case "-i": options.ReadFromStdIn = true; break;

                case "--file-kind":
                {
                    if (!args.Shift(out string? fileKind))
                        diag.Error($"Argument to '{arg}' is missing; expected 1 value.");
                    else
                    {
                        switch (fileKind)
                        {
                            default: diag.Error($"File kind '{fileKind}' not recognized."); break;

                            case "laye": currentFileType = InputFileLanguage.LayeSource; break;
                            case "module": currentFileType = InputFileLanguage.LayeModule; break;
                        }
                    }
                } break;
                
                case "-o":
                {
                    if (!args.Shift(out string? outputPath))
                        diag.Error($"Argument to '{arg}' is missing; expected 1 value.");
                    else options.ObjectFilePath = outputPath;
                } break;

                case "--emit-llvm": options.AssemblerFormat = AssemblerFormat.LLVM; break;

                case "--no-corelib": options.NoCoreLibrary = true; break;
                case "--no-stdlib": options.NoStandardLibrary = true; break;

                case "--lex": options.DriverStage = DriverStage.Lex; break;
                case "--parse": options.DriverStage = DriverStage.Parse; break;
                case "--sema": options.DriverStage = DriverStage.Sema; break;
                case "--codegen": options.DriverStage = DriverStage.Codegen; break;
                case "--compile": options.DriverStage = DriverStage.Compile; break;

                case "--tokens": options.PrintTokens = true; break;
                case "--ast": options.PrintAst = true; break;
                case "--no-lower": options.NoLower = true; break;
                case "--ir": options.PrintIR = true; break;
            }
        }

        if (options.NoCoreLibrary)
            options.NoStandardLibrary = true;

        if (options.ModuleSourceFiles.Count == 0)
        {
            if (!options.ShowHelp && !options.ShowVersion)
                diag.Error("No input source files.");
        }

        if (outputColoring == Driver.OutputColoring.Auto)
            outputColoring = Console.IsErrorRedirected ? Driver.OutputColoring.Never : Driver.OutputColoring.Always;
        options.OutputColoring = outputColoring == Driver.OutputColoring.Always;

        return options;
    }
}
