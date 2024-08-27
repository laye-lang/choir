namespace Choir.CommandLine;

public enum LongOptionFormat
{
    DoubleTick,
    SingleTick,
    SingleOrDoubleTick,
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class CliOptionAttribute(int shortOption, string? longOption, string? argument, string description)
    : Attribute
{
    public char ShortOptionName { get; } = (char)shortOption;
    public string? LongOptionName { get; } = longOption;
    public string? ArgumentName { get; } = argument;
    public string Description { get; } = description;

    public LongOptionFormat LongOptionFormat { get; init; } = LongOptionFormat.DoubleTick;
    public string[] ArgumentValues { get; init; } = [];
}
