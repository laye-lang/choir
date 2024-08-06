using System.Runtime.InteropServices;
using Choir.CommandLine;

namespace Choir;

public static class Program
{
    public static int Main(string[] args)
    {
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
            if (arg == "-x" || arg == "--file-type")
            {
                if (!args.Shift(out var fileType))
                    diag.Error($"argument to '{arg}' is missing (expected 1 value)");
                else
                {
                    switch (fileType)
                    {
                        default: diag.Error($"language not recognized: '{fileType}'"); break;
                        case "laye": currentFileType = InputFileLanguage.Laye; break;
                        case "c": currentFileType = InputFileLanguage.C; break;
                        case "cpp": currentFileType = InputFileLanguage.CPP; break;
                        case "choir": currentFileType = InputFileLanguage.Choir; break;
                    }
                }
            }
            else if (arg == "--lex")
                options.DriverStage = ChoirDriverStage.Lex;
            else if (arg == "--parse")
                options.DriverStage = ChoirDriverStage.Parse;
            else if (arg == "--sema")
                options.DriverStage = ChoirDriverStage.Sema;
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
                        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && inputFileExtension == ".C")
                            inputFileType = InputFileLanguage.CPP;
                        else switch (inputFileExtension.ToLower())
                        {
                            case ".laye": inputFileType = InputFileLanguage.Laye; break;
                            case ".c": inputFileType = InputFileLanguage.C; break;
                            case ".cpp": inputFileType = InputFileLanguage.CPP; break;
                            case ".ixx": inputFileType = InputFileLanguage.CPP; break;
                            case ".cc": inputFileType = InputFileLanguage.CPP; break;
                            case ".ccm": inputFileType = InputFileLanguage.CPP; break;
                        }
                    }

                    options.InputFiles.Add(new(inputFileType, inputFileInfo));
                }
            }
        }

        if (options.InputFiles.Count == 0)
            diag.Error("no input files");

        if (outputColoring == OutputColoring.Auto)
            outputColoring = Console.IsErrorRedirected ? OutputColoring.Never : OutputColoring.Always;
        options.OutputColoring = outputColoring == OutputColoring.Always;

        return options;
    }
}
