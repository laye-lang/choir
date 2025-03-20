using Choir.Diagnostics;
using Choir.Driver;

namespace Choir.FrontEnd.Score.Driver;

public sealed class ScoreDriverOptions
    : BaseCompilerDriverOptions<ScoreDriverOptions, ScoreContext, BaseCompilerDriverParseState>
{
    public bool Run { get; set; } = false;

    public List<(string Name, FileInfo File)> InputFiles { get; set; } = [];

    protected override void HandleValue(string value, DiagnosticEngine diag, CliArgumentIterator args, BaseCompilerDriverParseState state)
    {
        var inputFile = new FileInfo(value);
        if (!inputFile.Exists)
            diag.Emit(DiagnosticLevel.Error, $"No such file or directory '{value}'.");

        InputFiles.Add((value, inputFile));
    }

    protected override void HandleArgument(string arg, DiagnosticEngine diag, CliArgumentIterator args, BaseCompilerDriverParseState state)
    {
        switch (arg)
        {
            default: base.HandleArgument(arg, diag, args, state); break;

            case "--run": Run = true; break;
        }
    }
}
