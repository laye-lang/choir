using System.Runtime.InteropServices;
using System.Text;
using Choir.CommandLine;

namespace Choir;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        bool hasIssuedError = false;
        var diag = new StreamingDiagnosticWriter(writer: Console.Error, useColor: !Console.IsErrorRedirected)
        {
            OnIssue = kind => hasIssuedError |= kind >= DiagnosticKind.Error
        };

        switch (args.Length == 0 ? null : args[0])
        {
            default:
            {
                var options = ParseChoirDriverOptions(diag, new CliArgumentIterator(args));
                if (hasIssuedError) return 1;

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

                var driver = ChoirDriver.Create(diag, options);
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

    private enum OutputColoring
    {
        Auto,
        Always,
        Never,
    }

    private static ChoirDriverOptions ParseChoirDriverOptions(DiagnosticWriter diag, CliArgumentIterator args)
    {
        var options = new ChoirDriverOptions();

        var currentFileType = InputFileLanguage.Default;
        var outputColoring = OutputColoring.Auto;

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

                        case "laye": currentFileType = InputFileLanguage.Laye; break;

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
                options.DriverStage = ChoirDriverStage.Preprocess;
            else if (arg == "--lex")
                options.DriverStage = ChoirDriverStage.Lex;
            else if (arg == "--parse")
                options.DriverStage = ChoirDriverStage.Parse;
            else if (arg == "--sema")
                options.DriverStage = ChoirDriverStage.Sema;
            else if (arg == "--codegen")
                options.DriverStage = ChoirDriverStage.Codegen;
            else if (arg == "-S")
                options.DriverStage = ChoirDriverStage.Compile;
            else if (arg == "-c")
                options.DriverStage = ChoirDriverStage.Assemble;
            else if (arg == "-emit-choir")
                options.AssemblerFormat = ChoirAssemblerFormat.Choir;
            else if (arg == "-emit-qbe")
                options.AssemblerFormat = ChoirAssemblerFormat.QBE;
            else if (arg == "-emit-llvm")
                options.AssemblerFormat = ChoirAssemblerFormat.LLVM;
            else if (arg == "--tokens")
            {
                options.DriverStage = ChoirDriverStage.Lex;
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
                                case ".laye": inputFileType = InputFileLanguage.Laye; break;

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
            diag.Error("no input files");

        if (options.OutputFile == "-")
        {
            if (options.InputFiles.Count != 1)
                diag.Error("Can only output to stdout (`-o -`) with a single input file.");

            if (options.DriverStage != ChoirDriverStage.Compile)
                diag.Error("Can only output to stdout (`-o -`) when specifying the 'compile only' flag `-S`.");
        }

        if (outputColoring == OutputColoring.Auto)
            outputColoring = Console.IsErrorRedirected ? OutputColoring.Never : OutputColoring.Always;
        options.OutputColoring = outputColoring == OutputColoring.Always;

        if (options.PrintFileScopes && options.DriverStage > ChoirDriverStage.Sema)
            diag.Error("'--file-scopes' can only be used with '--lex', '--parse' and '--sema'");

        return options;
    }

    private const string DriverVersion = @"choir 0.1.0";

    private const string DriverOptions = @"Usage: choir [options] file...
Options:
    --help                   Display this information
    --version                Display compiler version information";
}
