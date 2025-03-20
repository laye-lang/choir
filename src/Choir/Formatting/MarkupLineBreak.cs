namespace Choir.Formatting;

public sealed class MarkupLineBreak
    : Markup
{
    public static readonly MarkupLineBreak Instance = new();
    public override int Length { get; } = 0;
}
