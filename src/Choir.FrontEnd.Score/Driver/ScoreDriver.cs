using Choir.Diagnostics;
using Choir.Driver;
using Choir.FrontEnd.Score.Syntax;
using Choir.Source;

namespace Choir.FrontEnd.Score.Driver;

public delegate IDiagnosticConsumer DiagnosticConsumerProvider(bool useColor);

public sealed class ScoreDriver
    : ICompilerDriver
{
    public static int RunWithArgs(DiagnosticConsumerProvider diagProvider, string[] args, string programName = "score")
    {
        ScoreDriverOptions options;
        using (var parserDiag = diagProvider(true))
        using (var diag = new DiagnosticEngine(parserDiag))
        {
            options = ScoreDriverOptions.Parse(diag, new(args));
            if (diag.HasEmittedErrors)
                return 1;
        }

        if (options.ShowVersion)
        {
            Console.WriteLine("TODO: Show Score compiler version text");
            return 0;
        }

        if (options.ShowHelp)
        {
            Console.WriteLine("TODO: Show Score compiler help text");
            return 0;
        }

        using var driverDiag = diagProvider(options.OutputColoring);
        var driver = Create(driverDiag, options, programName);
        return driver.Execute();
    }

    public static ScoreDriver Create(IDiagnosticConsumer diagConsumer, ScoreDriverOptions options, string programName = "score")
    {
        return new ScoreDriver(programName, diagConsumer, options);
    }

    public string ProgramName { get; set; }
    public ScoreContext Context { get; set; }
    public ScoreDriverOptions Options { get; set; }

    private ScoreDriver(string programName, IDiagnosticConsumer diagConsumer, ScoreDriverOptions options)
    {
        ProgramName = programName;
        Context = new ScoreContext(diagConsumer, Target.X86_64);
        Options = options;
    }

    public int Execute()
    {
        foreach (var (fileName, file) in Options.InputFiles)
        {
            var source = new SourceText(fileName, File.ReadAllText(file.FullName));
            var printer = new ScoreSyntaxPrinter(source, Options.OutputColoring);

            //var tokens = ScoreLexer.ReadTokens(Context, source);
            //printer.PrintTokens(tokens);

            var syntaxUnit = ScoreParser.ParseSyntaxUnit(Context, source);
            printer.PrintSyntaxUnit(syntaxUnit);
        }

        return 0;
    }
}
