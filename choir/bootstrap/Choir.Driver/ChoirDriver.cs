namespace Choir;

public enum ChoirDriverStage
{
    Lex,
    Parse,
    Sema,
    Compile,
}

public enum InputFileLanguage
{
    Default,
    Laye,
    C,
    CPP,
    Choir,
}

public readonly struct InputFileInfo(InputFileLanguage language, FileInfo fileInfo)
{
    public readonly InputFileLanguage Language = language;
    public readonly FileInfo FileInfo = fileInfo;
}

public sealed class ChoirDriver
{
    public ChoirDriverOptions Options { get; }
    public ChoirContext Context { get; }

    public ChoirDriver(ChoirDriverOptions options)
    {
        Options = options;
        Context = new(options.OutputColoring);
    }

    public int Execute()
    {
        return 0;
    }
}

public sealed class ChoirDriverOptions
{
    public List<InputFileInfo> InputFiles { get; set; } = [];
    public ChoirDriverStage DriverStage { get; set; } = ChoirDriverStage.Compile;
    public bool OutputColoring { get; set; }
}
