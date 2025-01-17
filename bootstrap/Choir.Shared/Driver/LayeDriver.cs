using System.Diagnostics;
using System.Runtime.InteropServices;

using Choir.CommandLine;

namespace Choir.Driver;

public sealed class LayeDriver
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
        var options = LayeDriverOptions.Parse(diag, new CliArgumentIterator(args));
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

    public static LayeDriver Create(DiagnosticWriter diag, LayeDriverOptions options, string programName = "laye")
    {
        return new LayeDriver(programName, diag, options);
    }

    public string ProgramName { get; }
    public LayeDriverOptions Options { get; }
    public ChoirContext Context { get; }

    private LayeDriver(string programName, DiagnosticWriter diag, LayeDriverOptions options)
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

    public int Execute()
    {
        throw new NotImplementedException();
    }
}

public sealed record class LayeDriverOptions
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
    /// Laye module directories to compile.
    /// If no directories are provided, then one of 'src', 'source' or '.' are implied, whichever is found first and also
    /// contains Laye source files.
    /// This means a call to the 'laye' driver with no explicit directories specified will try to be smart based on
    /// common conventions.
    /// </summary>
    public List<DirectoryInfo> ModuleDirectories { get; } = [];

    /// <summary>
    /// Additional binary files that the input source code expects to link against in the future.
    /// `layec` will read Laye metadata from these files to perform smenatic analysis against.
    /// </summary>
    public List<FileInfo> BinaryDependencyFiles { get; } = [];

    /// <summary>
    /// Additional library search directories.
    /// Library search directories are searched in the order they are specified.
    /// Any built-in or environment-specified library search paths are prepended to this list in the order they are identified.
    /// </summary>
    public List<DirectoryInfo> LibrarySearchPaths { get; } = [];

    /// <summary>
    /// The name of the generated binary file.
    /// 
    /// `laye` can produce executable or object files. It delegates to `layec` to produce object files
    /// and to the system linker to link into executables.
    /// </summary>
    public string? OutputFilePath { get; set; }

    /// <summary>
    /// The format of assembler output when compiling with the `--compile` flag.
    /// Defaults to traditional assembler code, but can be switched to LLVM IR with the `--emit-llvm` flag.
    /// </summary>
    public AssemblerFormat AssemblerFormat { get; set; } = AssemblerFormat.Assembler;

    /// <summary>
    /// The `--omit-source-text` flag.
    /// Disables the inclusion of source text in binary module files.
    /// </summary>
    public bool OmitSourceTextInModuleBinary { get; set; } = false;

    /// <summary>
    /// The `--distribution` flag.
    /// Enables "distribution" builds, where file paths are simplified since the end user won't have source files for the module on their system.
    /// </summary>
    public bool IsDistribution { get; set; } = false;

    /// <summary>
    /// The `--no-corelib` flag. This implies `--no-stdlib`.
    /// Disables linking to the Laye core library, requiring the programmer to provide their own implementation.
    /// `layec` does not handle linking itself, but it does ensure the default libraries are referenced by default and they are expected to be available when linking occurs.
    /// </summary>
    public bool NoCoreLibrary { get; set; }

    /// <summary>
    /// Through what stage the driver should run.
    /// By default, a call to `layec` will generate an object file for the target system.
    /// All of the <see cref="DriverStage.Lex"/> (--lex), <see cref="DriverStage.Parse"/> (--parse), <see cref="DriverStage.Sema"/>, (--sema), <see cref="DriverStage.Codegen"/> (--codegen), <see cref="DriverStage.Compile"/> (--compile), <see cref="DriverStage.Assemble"/> (-c, --assemble), and <see cref="DriverStage.Link"/> stages are supported.
    /// When a specific driver stage is selected, any alternate output forms it supports may also be set; any alternate output form which does not apply to the set driver stage is ignored.
    /// </summary>
    public DriverStage DriverStage { get; set; } = DriverStage.Link;

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

    public static LayeDriverOptions Parse(DiagnosticWriter diag, CliArgumentIterator args)
    {
        var options = new LayeDriverOptions();

        var currentFileType = InputFileLanguage.Default;
        var outputColoring = Driver.OutputColoring.Auto;

        while (args.Shift(out string arg))
        {
            switch (arg)
            {
                default:
                {
                    var inputDirectoryInfo = new DirectoryInfo(arg);
                    if (inputDirectoryInfo.Exists)
                    {
                        options.ModuleDirectories.Add(inputDirectoryInfo);
                        break;
                    }

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
                    {
                        diag.Error($"Laye source files ('{arg}') are not accepted by the Laye build tool/compiler driver.");
                        diag.Note("To compile Laye with this tool, pass 0 or more directory paths containing Laye source files;\neach directory represents a Laye module, a collection of its immediate child source files.");
                        diag.Note("If no directory paths are given as input, this tool will search for some common directories automatically.");
                    }
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

                case "-o":
                {
                    if (!args.Shift(out string? outputPath))
                        diag.Error($"Argument to '{arg}' is missing; expected 1 value.");
                    else options.OutputFilePath = outputPath;
                } break;

                case "-L":
                {
                    if (!args.Shift(out string? libDir) || libDir.IsNullOrEmpty())
                        diag.Error($"Argument to '{arg}' is missing; expected 1 (non-empty) value.");
                    else options.LibrarySearchPaths.Add(new DirectoryInfo(libDir));
                } break;

                case "--emit-llvm": options.AssemblerFormat = AssemblerFormat.LLVM; break;

                case "--omit-source-text": options.OmitSourceTextInModuleBinary = true; break;
                case "--distribution": options.IsDistribution = true; break;

                case "--no-corelib": options.NoCoreLibrary = true; break;

                case "--lex": options.DriverStage = DriverStage.Lex; break;
                case "--parse": options.DriverStage = DriverStage.Parse; break;
                case "--sema": options.DriverStage = DriverStage.Sema; break;
                case "--codegen": options.DriverStage = DriverStage.Codegen; break;
                case "--compile": options.DriverStage = DriverStage.Compile; break;
                case "--assemble":
                case "-c": options.DriverStage = DriverStage.Assemble; break;

                case "--tokens": options.PrintTokens = true; break;
                case "--ast": options.PrintAst = true; break;
                case "--no-lower": options.NoLower = true; break;
                case "--ir": options.PrintIR = true; break;
            }
        }

        if (options.ModuleDirectories.Count == 0)
        {
            if (!options.ShowHelp && !options.ShowVersion)
            {
                DirectoryInfo[] predefinedDirs = [
                    new("src"),
                    new("source"),
                    new("."),
                ];

                foreach (var dir in predefinedDirs)
                {
                    if (dir.Exists && dir.EnumerateFiles().Any(f => f.Extension == ".laye"))
                    {
                        options.ModuleDirectories.Add(dir);
                        break;
                    }
                }
            }
        }

        if (options.ModuleDirectories.Count == 0)
            options.ShowHelp = true;

        if (outputColoring == Driver.OutputColoring.Auto)
            outputColoring = Console.IsErrorRedirected ? Driver.OutputColoring.Never : Driver.OutputColoring.Always;
        options.OutputColoring = outputColoring == Driver.OutputColoring.Always;

        return options;
    }
}
