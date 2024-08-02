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

                var driver = new ChoirDriver(options);
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

    private static ChoirDriverOptions ParseChoirDriverOptions(DiagnosticWriter diag, CliArgumentIterator args)
    {
        var options = new ChoirDriverOptions();

        var currentFileType = InputFileLanguage.Default;

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
            else
            {
                var inputFileInfo = new FileInfo(arg);
                if (!inputFileInfo.Exists)
                    diag.Error($"no such file or directory: '{arg}'");
                else options.InputFiles.Add(new(currentFileType, inputFileInfo));
            }
        }

        if (options.InputFiles.Count == 0)
            diag.Error("no input files");

        return options;
    }
}

#if false

// https://github.com/dotnet/ClangSharp/blob/main/tests/ClangSharp.UnitTests/CXTranslationUnitTest.cs
using ClangSharp;
using ClangSharp.Interop;
using static ClangSharp.Interop.CXTranslationUnit_Flags;

var name = "basic";
var dir = Path.GetRandomFileName();
_ = Directory.CreateDirectory(dir);

try
{
    // Create a file with the right name
    var file = new FileInfo(Path.Combine(dir, name + ".c"));
    File.WriteAllText(file.FullName, "int main() { return 0; }");

    using var index = CXIndex.Create();
    var translationUnit = CXTranslationUnit.Parse(
        index, file.FullName, Array.Empty<string>(),
        Array.Empty<CXUnsavedFile>(), CXTranslationUnit_None);
    var clangFile = translationUnit.GetFile(file.FullName);

    using var tu = TranslationUnit.GetOrCreate(translationUnit);
    foreach (var decl in tu.TranslationUnitDecl.Decls)
    {
        if (decl is FunctionDecl funcDecl) {
            Console.WriteLine($"function: {funcDecl.Name}");
        } else {
            Console.WriteLine(decl.Spelling);
        }
    }
}
finally
{
    Directory.Delete(dir, true);
}

#endif
