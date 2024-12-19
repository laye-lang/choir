namespace Choir.CommandLine;

public sealed class CliOptionsParser
{
    public static TOptions Parse<TOptions>(string[] args)
        where TOptions : new()
    {
        return CliOptionsParser<TOptions>.Parse(args);
    }
}

public sealed class CliOptionsParser<TOptions>
    where TOptions : new()
{
    public static TOptions Parse(string[] args)
    {
        throw new NotImplementedException();
    }

    private sealed class OptionInfo(int shortOption, string? longOption, string? argument, string description)
    {
        public char ShortOptionName { get; } = (char)shortOption;
        public string? LongOptionName { get; } = longOption;
        public string? ArgumentName { get; } = argument;
        public string Description { get; } = description;

        public LongOptionFormat LongOptionFormat { get; init; } = LongOptionFormat.DoubleTick;

        public bool IsFilePath { get; init; }
    }

    private readonly TOptions _options = new();
    // for options like `-llibrary` where multiple options can be specified, each with a value.
    private readonly Dictionary<OptionInfo, List<string>> _multiOptionValues = [];
}
