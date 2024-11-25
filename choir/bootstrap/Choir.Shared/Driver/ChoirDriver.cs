using System.Diagnostics;
using System.Runtime.InteropServices;

using Choir.CommandLine;
using Choir.Front.Laye;
using Choir.Front.Laye.Codegen;
using Choir.Front.Laye.Sema;
using Choir.Front.Laye.Syntax;

using LLVMSharp.Interop;

namespace Choir.Driver;

public readonly struct InputFileInfo(InputFileLanguage language, FileInfo fileInfo)
{
    public readonly InputFileLanguage Language = language;
    public readonly FileInfo FileInfo = fileInfo;
}

public abstract class ChoirJob(ChoirDriver driver)
{
    public ChoirDriver Driver { get; } = driver;
    public ChoirContext Context { get; } = driver.Context;

    public abstract int Run();

    public class CompileLayeTranslationUnit(ChoirDriver driver, FileInfo[] layeFiles) : ChoirJob(driver)
    {
        private readonly FileInfo[] _layeFiles = layeFiles;

        public override int Run()
        {
            var layeFiles = _layeFiles
                .Select(Context.GetSourceFile)
                .DistinctBy(sourceFile => sourceFile.FileId)
                .ToArray();

            var tu = new TranslationUnit(Context);

            foreach (var file in layeFiles)
            {
                var module = new OldModule(file);
                tu.AddModule(module);

                Lexer.ReadTokens(module);
                if (Driver.Options.DriverStage != DriverStage.Lex)
                    Parser.ParseSyntax(module);
            }

            Context.Diag.Flush();

            if (Driver.Options.DriverStage == DriverStage.Lex)
            {
                if (Driver.Options.PrintTokens)
                {
                    var printer = new SyntaxPrinter(Context, Driver.Options.PrintFileScopes);
                    foreach (var module in tu.Modules)
                    {
                        if (Driver.Options.PrintCliFilesOnly && !Driver.Options.InputFiles.Any(f => module.SourceFile.FileInfo == f.FileInfo))
                            continue;
                        printer.PrintModuleTokens(module);
                    }
                }

                return 0;
            }

            if (Driver.Options.DriverStage == DriverStage.Parse)
            {
                if (Driver.Options.PrintAst)
                {
                    var syntaxPrinter = new SyntaxPrinter(Context, Driver.Options.PrintFileScopes);
                    foreach (var module in tu.Modules)
                    {
                        if (Driver.Options.PrintCliFilesOnly && !Driver.Options.InputFiles.Any(f => module.SourceFile.FileInfo == f.FileInfo))
                            continue;
                        if (module.HasSyntax)
                            syntaxPrinter.PrintModuleSyntax(module);
                        else syntaxPrinter.PrintModuleHeader(module);
                    }
                }

                return 0;
            }

            if (Context.HasIssuedError) return 1;

            Sema.Analyse(tu);
            Context.Diag.Flush();

            if (Driver.Options.DriverStage == DriverStage.Sema)
            {
                if (Driver.Options.PrintAst)
                {
                    var syntaxPrinter = new SyntaxPrinter(Context, Driver.Options.PrintFileScopes);
                    var semaPrinter = new SemaPrinter(Context, Driver.Options.PrintFileScopes);
                    foreach (var module in tu.Modules)
                    {
                        if (Driver.Options.PrintCliFilesOnly && !Driver.Options.InputFiles.Any(f => module.SourceFile.FileInfo == f.FileInfo))
                            continue;
                        if (module.HasSemaDecls)
                            semaPrinter.PrintModule(module);
                        else if (module.HasSyntax)
                            syntaxPrinter.PrintModuleSyntax(module);
                        else semaPrinter.PrintModuleHeader(module);
                    }
                }

                return 0;
            }

            if (Context.HasIssuedError) return 1;

            LayeCodegen.GenerateIR(tu);
            Context.Diag.Flush();

            Debug.Assert(tu.LlvmContext.HasValue);
            Debug.Assert(tu.LlvmModule.HasValue);

            var llvmContext = tu.LlvmContext.Value;
            var llvmModule = tu.LlvmModule.Value;

            if (Driver.Options.DriverStage == DriverStage.Codegen)
            {
                if (Driver.Options.PrintIR)
                    llvmModule.Dump();

                return 0;
            }

            if (Context.HasIssuedError) return 1;

            //Module[] compilationModules;
            if (Driver.Options.OutputFile == "-")
            {
                Debug.Assert(Driver.Options.InputFiles.Count == 1, "exactly one file should have been specified when requesting output to stdout.");
                Debug.Assert(Driver.Options.DriverStage == DriverStage.Compile, "when specifying output to stdout, -S (the 'compile only' flag) should have been set.");
                //var firstModule = tu.Modules.First();
                //Debug.Assert(firstModule.LlvmModule is not null);
                //compilationModules = [firstModule];
            }
            else
            {
                //Debug.Assert(tu.Modules.All(m => m.LlvmModule is not null));
                //compilationModules = tu.Modules.ToArray();
            }

#if false
            switch (Driver.Options.AssemblerFormat)
            {
                case ChoirAssemblerFormat.LLVM:
                    break;
                default:
                {
                    Context.Assert(false, $"Unsupported assembler format: {Driver.Options.AssemblerFormat}");
                    throw new UnreachableException();
                }
            }
#endif

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

            if (Driver.Options.DriverStage == DriverStage.Compile)
            {
                switch (Driver.Options.AssemblerFormat)
                {
                    case AssemblerFormat.Assembler:
                        {
                            if (Driver.Options.OutputFile == "-")
                            {
                                string tempFilePath = Path.GetTempFileName();
                                try
                                {
                                    EmitLLVMModuleToFile(tempFilePath, LLVMCodeGenFileType.LLVMAssemblyFile);
                                    Console.Write(File.ReadAllText(tempFilePath));
                                }
                                finally
                                {
                                    File.Delete(tempFilePath);
                                }
                            }
                            else if (Driver.Options.OutputFile is not null)
                                EmitLLVMModuleToFile(Driver.Options.OutputFile, LLVMCodeGenFileType.LLVMAssemblyFile);
                            else EmitLLVMModuleToFile("a.s", LLVMCodeGenFileType.LLVMAssemblyFile);
                        }
                        break;

                    case AssemblerFormat.LLVM:
                        {
                            if (Driver.Options.OutputFile == "-")
                                llvmModule.Dump();
                            else if (Driver.Options.OutputFile is not null)
                                llvmModule.PrintToFile(Driver.Options.OutputFile);
                            else llvmModule.PrintToFile("a.ll");
                        }
                        break;
                }

                return 0;
            }

            if (Driver.Options.DriverStage == DriverStage.Assemble)
            {
                if (Driver.Options.OutputFile == "-")
                {
                    string tempFilePath = Path.GetTempFileName();
                    try
                    {
                        EmitLLVMModuleToFile(tempFilePath, LLVMCodeGenFileType.LLVMAssemblyFile);
                        byte[] bytes = File.ReadAllBytes(tempFilePath);
                        using var stdout = Console.OpenStandardOutput();
                        stdout.Write(bytes);
                        stdout.Flush();
                    }
                    finally
                    {
                        File.Delete(tempFilePath);
                    }
                }
                else if (Driver.Options.OutputFile is not null)
                    EmitLLVMModuleToFile(Driver.Options.OutputFile);
                else EmitLLVMModuleToFile("a.o");

                return 0;
            }

            Debug.Assert(Driver.Options.DriverStage == DriverStage.Link);

            void LinkLLVMModuleToFile(string filePath)
            {
                string moduleTempPath = Path.Combine(Path.GetTempPath(), $"temp{DateTime.Now.Ticks}.o");
                try
                {
                    EmitLLVMModuleToFile(moduleTempPath, LLVMCodeGenFileType.LLVMObjectFile);

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var startInfo = new ProcessStartInfo("clang", ["-o", filePath, moduleTempPath]);

                        var process = Process.Start(startInfo);
                        Debug.Assert(process is not null, "whoops");
                        process.WaitForExit();

                        if (process.ExitCode != 0 || !File.Exists(filePath))
                        {
                            Context.Diag.ICE($"Failed to link to output file '{filePath}'.");
                        }
                    }
                    else
                    {
                        Context.Diag.ICE("This compiler driver is too stupid to link an executable on this operating system, sorry!!");
                    }
                }
                finally
                {
                    if (File.Exists(moduleTempPath)) File.Delete(moduleTempPath);
                }
            }

            if (Driver.Options.OutputFile == "-")
            {
                string tempFilePath = Path.GetTempFileName();
                try
                {
                    LinkLLVMModuleToFile(tempFilePath);
                    byte[] bytes = File.ReadAllBytes(tempFilePath);
                    using var stdout = Console.OpenStandardOutput();
                    stdout.Write(bytes);
                    stdout.Flush();
                }
                finally
                {
                    File.Delete(tempFilePath);
                }
            }
            else if (Driver.Options.OutputFile is not null)
                LinkLLVMModuleToFile(Driver.Options.OutputFile);
            else LinkLLVMModuleToFile($"a{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ".out")}");

            return Context.Diag.HasIssuedErrors ? 1 : 0;
        }
    }
}

public enum OutputColoring
{
    Auto,
    Always,
    Never,
}

public sealed class ChoirDriver
{
    private const string DriverVersion = @"choir 0.1.0";

    private const string DriverOptions = @"Usage: choir [options] file...
Options:
    --help                   Display this information
    --version                Display compiler version information";

    public static int RunWithArgs(DiagnosticWriter diag, string[] args)
    {
        switch (args.Length == 0 ? null : args[0])
        {
            default:
                {
                    var options = ChoirDriverOptions.Parse(diag, new CliArgumentIterator(args));
                    if (diag.HasIssuedErrors) return 1;

                    if (options.ShowVersion)
                    {
                        Console.WriteLine(DriverVersion);
                        return 0;
                    }

                    if (options.ShowHelp)
                    {
                        Console.WriteLine(DriverOptions);
                        return 0;
                    }

                    if (!options.NoStandardLibrary)
                    {
                        var selfExe = new FileInfo(typeof(Program).Assembly.Location);
                        var searchDir = selfExe.Directory;

                        DirectoryInfo? liblayeDir = null;
                        while (searchDir is not null && liblayeDir is null)
                        {
                            var checkDir = new DirectoryInfo(Path.Combine(searchDir.FullName, "lib/laye"));
                            if (checkDir.Exists)
                                liblayeDir = checkDir;
                            else searchDir = searchDir.Parent;
                        }

                        if (liblayeDir is not null)
                            options.IncludeDirectories.Add(liblayeDir.FullName);
                        else diag.Error("Could not find the Laye standard library. If this is intentional, pass the '-nostdlib' flag.");
                    }

                    var driver = Create(diag, options);
                    return driver.Execute();
                }

            case "cc":
                {
                    string[] ccArgs = args.Skip(1).ToArray();
                    var driver = new ChoirCCDriver(diag, ccArgs);
                    return driver.Execute();
                }
        }
    }

    public static ChoirDriver Create(DiagnosticWriter diag, ChoirDriverOptions options)
    {
        if (options.InputFiles.Any(inputFile => inputFile.Language == InputFileLanguage.Default))
        {
            diag.ICE("One or more input files did not get assigned a language before constructing the Choir driver.");
            throw new UnreachableException();
        }

        return new ChoirDriver(diag, options);
    }

    public ChoirDriverOptions Options { get; }
    public ChoirContext Context { get; }

    private ChoirDriver(DiagnosticWriter diag, ChoirDriverOptions options)
    {
        Options = options;
        Context = new(diag, ChoirTarget.X86_64, options.OutputColoring)
        {
            IncludeDirectories = options.IncludeDirectories,
            LibraryDirectories = options.LibraryDirectories,
        };
    }

    public int Execute()
    {
        try
        {
            var jobs = new List<ChoirJob>();

            var layeFileInfos = Options.InputFiles.Where(inputFile => inputFile.Language == InputFileLanguage.LayeSource);
            if (layeFileInfos.Any())
            {
                var layeFiles = layeFileInfos.Select(inputFile => inputFile.FileInfo).ToArray();
                var layeJob = new ChoirJob.CompileLayeTranslationUnit(this, layeFiles);
                jobs.Add(layeJob);
            }

            if (Context.HasIssuedError)
                return 1;

            foreach (var job in jobs)
            {
                int result = job.Run();
                if (result != 0)
                    return result;
            }

            return 0;
        }
        finally
        {
            Context.Diag.Flush();
        }
    }
}

public sealed class ChoirDriverOptions
{
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
    public bool ShowVerboseOutput { get; set; }
    public bool QuoteAndDoNotExecute { get; set; }

    public List<InputFileInfo> InputFiles { get; set; } = [];
    public string? OutputFile { get; set; }
    public DriverStage DriverStage { get; set; } = DriverStage.Link;
    public AssemblerFormat AssemblerFormat { get; set; } = AssemblerFormat.Assembler;
    public bool PrintTokens { get; set; }
    public bool PrintAst { get; set; }
    public bool PrintIR { get; set; }
    public bool PrintFileScopes { get; set; }
    public bool PrintCliFilesOnly { get; set; }
    public bool OutputColoring { get; set; }
    public List<string> IncludeDirectories { get; set; } = [];
    public List<string> LibraryDirectories { get; set; } = [];

    #region Linker Options

    public List<string> LinkLibraries { get; set; } = [];
    public string? EntryName { get; set; }
    public bool NoStartFiles { get; set; }
    public bool NoStandardLibrary { get; set; }
    public bool NoLibC { get; set; }

    #endregion

    public static ChoirDriverOptions Parse(DiagnosticWriter diag, CliArgumentIterator args)
    {
        var options = new ChoirDriverOptions();

        var currentFileType = InputFileLanguage.Default;
        var outputColoring = Driver.OutputColoring.Auto;

        while (args.Shift(out string arg))
        {
            if (arg == "--help")
                options.ShowHelp = true;
            else if (arg == "--version")
                options.ShowVersion = true;
            else if (arg == "-v")
                options.ShowVerboseOutput = true;
            else if (arg == "-###")
            {
                options.ShowVerboseOutput = true;
                options.QuoteAndDoNotExecute = true;
            }
            else if (arg == "-x" || arg == "--file-type")
            {
                if (!args.Shift(out string? fileType))
                    diag.Error($"argument to '{arg}' is missing (expected 1 value)");
                else
                {
                    switch (fileType)
                    {
                        default: diag.Error($"language not recognized: '{fileType}'"); break;

                        case "laye": currentFileType = InputFileLanguage.LayeSource; break;

                        case "choir": currentFileType = InputFileLanguage.Choir; break;

                        case "c": currentFileType = InputFileLanguage.C; break;
                        case "c-header": currentFileType = InputFileLanguage.C | InputFileLanguage.Header; break;
                        case "cpp-output": currentFileType = InputFileLanguage.C | InputFileLanguage.NoPreprocess; break;

                        case "c++": currentFileType = InputFileLanguage.CXX; break;
                        case "c++-header": currentFileType = InputFileLanguage.CXX | InputFileLanguage.Header; break;
                        case "c++-cpp-output": currentFileType = InputFileLanguage.CXX | InputFileLanguage.NoPreprocess; break;

                        case "objective-c": currentFileType = InputFileLanguage.ObjC; break;
                        case "objective-c-header": currentFileType = InputFileLanguage.ObjC | InputFileLanguage.Header; break;
                        case "objective-c-cpp-output": currentFileType = InputFileLanguage.ObjC | InputFileLanguage.NoPreprocess; break;

                        case "objective-c++": currentFileType = InputFileLanguage.ObjCXX; break;
                        case "objective-c++-header": currentFileType = InputFileLanguage.ObjCXX | InputFileLanguage.Header; break;
                        case "objective-c++-cpp-output": currentFileType = InputFileLanguage.ObjCXX | InputFileLanguage.NoPreprocess; break;

                        case "assembler": currentFileType = InputFileLanguage.Assembler | InputFileLanguage.NoPreprocess; break;
                        case "assembler-with-cpp": currentFileType = InputFileLanguage.Assembler; break;
                    }
                }
            }
            else if (arg == "-E")
                options.DriverStage = DriverStage.Preprocess;
            else if (arg == "-flex-only")
                options.DriverStage = DriverStage.Lex;
            else if (arg == "-fsyntax-only")
                options.DriverStage = DriverStage.Parse;
            else if (arg == "-fsema-only")
                options.DriverStage = DriverStage.Sema;
            else if (arg == "--codegen")
                options.DriverStage = DriverStage.Codegen;
            else if (arg == "-S")
                options.DriverStage = DriverStage.Compile;
            else if (arg == "-c")
                options.DriverStage = DriverStage.Assemble;
            else if (arg == "-emit-llvm")
                options.AssemblerFormat = AssemblerFormat.LLVM;
            else if (arg == "--tokens")
            {
                options.DriverStage = DriverStage.Lex;
                options.PrintTokens = true;
            }
            else if (arg == "--ast")
                options.PrintAst = true;
            else if (arg == "--ir")
                options.PrintIR = true;
            else if (arg == "--file-scopes")
                options.PrintFileScopes = true;
            else if (arg == "--print-cli-files-only")
                options.PrintCliFilesOnly = true;
            else if (arg == "-e")
            {
                if (!args.Shift(out string? entryName))
                    diag.Error($"argument to '{arg}' is missing (expected 1 value)");
                else options.EntryName = entryName;
            }
            else if (arg == "-nostartfiles")
                options.NoStartFiles = true;
            else if (arg == "--no-standard-libraries" || arg == "-nostdlib")
                options.NoStandardLibrary = true;
            else if (arg == "-nodefaultlibs")
            {
                options.NoStandardLibrary = true;
            }
            else if (arg == "-nolibc")
                options.NoLibC = true;
            else if (arg == "-I")
            {
                if (!args.Shift(out string? includeDirectory))
                    diag.Error($"argument to '{arg}' is missing (expected 1 value)");
                else options.IncludeDirectories.Add(includeDirectory);
            }
            else if (arg.StartsWith("-I"))
            {
                string includeDirectory = arg.Substring(2);
                if (string.IsNullOrWhiteSpace(includeDirectory))
                    diag.Error($"argument to '-I' is missing (expected 1 value)");
                else options.IncludeDirectories.Add(includeDirectory);
            }
            else if (arg == "-L")
            {
                if (!args.Shift(out string? libraryDirectory))
                    diag.Error($"argument to '{arg}' is missing (expected 1 value)");
                else options.LibraryDirectories.Add(libraryDirectory);
            }
            else if (arg.StartsWith("-L"))
            {
                string libraryDirectory = arg.Substring(2);
                if (string.IsNullOrWhiteSpace(libraryDirectory))
                    diag.Error($"argument to '-L' is missing (expected 1 value)");
                else options.IncludeDirectories.Add(libraryDirectory);
            }
            else if (arg == "-l")
            {
                if (!args.Shift(out string? linkLibrary))
                    diag.Error($"argument to '{arg}' is missing (expected 1 value)");
                else options.LinkLibraries.Add(linkLibrary);
            }
            else if (arg.StartsWith("-l"))
            {
                string linkLibrary = arg.Substring(2);
                if (string.IsNullOrWhiteSpace(linkLibrary))
                    diag.Error($"argument to '-l' is missing (expected 1 value)");
                else options.LinkLibraries.Add(linkLibrary);
            }
            else if (arg == "-o")
            {
                if (!args.Shift(out string? outputFilePath))
                    diag.Error($"argument to '{arg}' is missing (expected 1 value)");
                else options.OutputFile = outputFilePath;
            }
            else
            {
                var inputFileInfo = new FileInfo(arg);
                if (!inputFileInfo.Exists)
                    diag.Error($"no such file or directory: '{arg}'");
                else
                {
                    var inputFileType = currentFileType;
                    if (inputFileType == InputFileLanguage.Default)
                    {
                        string inputFileExtension = inputFileInfo.Extension;
                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && inputFileExtension is ".C" or ".CPP")
                            inputFileType = InputFileLanguage.CXX;
                        else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && inputFileExtension is ".HPP")
                            inputFileType = InputFileLanguage.CXX | InputFileLanguage.Header;
                        else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && inputFileExtension is ".M")
                            inputFileType = InputFileLanguage.ObjCXX;
                        else if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && inputFileExtension is ".S")
                            inputFileType = InputFileLanguage.Assembler;
                        else switch (inputFileExtension.ToLower())
                            {
                                case ".laye": inputFileType = InputFileLanguage.LayeSource; break;

                                case ".h": inputFileType = InputFileLanguage.C | InputFileLanguage.CXX | InputFileLanguage.ObjC | InputFileLanguage.ObjCXX | InputFileLanguage.Header; break;

                                case ".c": inputFileType = InputFileLanguage.C; break;
                                case ".i": inputFileType = InputFileLanguage.C | InputFileLanguage.NoPreprocess; break;

                                case ".cc": inputFileType = InputFileLanguage.CXX; break;
                                case ".cp": inputFileType = InputFileLanguage.CXX; break;
                                case ".cxx": inputFileType = InputFileLanguage.CXX; break;
                                case ".cpp": inputFileType = InputFileLanguage.CXX; break;
                                case ".c++": inputFileType = InputFileLanguage.CXX; break;
                                case ".ixx": inputFileType = InputFileLanguage.CXX; break;
                                case ".ccm": inputFileType = InputFileLanguage.CXX; break;
                                case ".ii": inputFileType = InputFileLanguage.CXX | InputFileLanguage.NoPreprocess; break;

                                case ".hh": inputFileType = InputFileLanguage.CXX | InputFileLanguage.Header; break;
                                case ".hp": inputFileType = InputFileLanguage.CXX | InputFileLanguage.Header; break;
                                case ".hxx": inputFileType = InputFileLanguage.CXX | InputFileLanguage.Header; break;
                                case ".hpp": inputFileType = InputFileLanguage.CXX | InputFileLanguage.Header; break;
                                case ".h++": inputFileType = InputFileLanguage.CXX | InputFileLanguage.Header; break;
                                case ".tcc": inputFileType = InputFileLanguage.CXX | InputFileLanguage.Header; break;

                                case ".m": inputFileType = InputFileLanguage.ObjC; break;
                                case ".mi": inputFileType = InputFileLanguage.ObjC | InputFileLanguage.NoPreprocess; break;

                                case ".mm": inputFileType = InputFileLanguage.ObjCXX; break;
                                case ".mii": inputFileType = InputFileLanguage.ObjCXX | InputFileLanguage.NoPreprocess; break;

                                case ".s": inputFileType = InputFileLanguage.Assembler | InputFileLanguage.NoPreprocess; break;
                                case ".sx": inputFileType = InputFileLanguage.Assembler; break;

                                default: inputFileType = InputFileLanguage.Object; break;
                            }
                    }

                    options.InputFiles.Add(new(inputFileType, inputFileInfo));
                }
            }
        }

        if (options.InputFiles.Count == 0)
        {
            if (!options.ShowHelp && !options.ShowVersion)
                diag.Error("No input files.");
        }

        if (options.OutputFile == "-")
        {
            if (options.InputFiles.Count != 1)
                diag.Error("Can only output to stdout (`-o -`) with a single input file.");

            if (options.DriverStage != DriverStage.Compile)
                diag.Error("Can only output to stdout (`-o -`) when specifying the 'compile only' flag `-S`.");
        }

        if (outputColoring == Driver.OutputColoring.Auto)
            outputColoring = Console.IsErrorRedirected ? Driver.OutputColoring.Never : Driver.OutputColoring.Always;
        options.OutputColoring = outputColoring == Driver.OutputColoring.Always;

        if (options.PrintFileScopes && options.DriverStage > DriverStage.Sema)
            diag.Error("'--file-scopes' can only be used with '--lex', '--parse' and '--sema'");

        diag.Flush();
        return options;
    }
}
