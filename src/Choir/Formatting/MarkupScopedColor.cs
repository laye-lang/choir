namespace Choir.Formatting;

public sealed class MarkupScopedColor(MarkupColor color, Markup contents)
    : Markup
{
    public MarkupColor Color { get; } = color;
    public Markup Contents { get; } = contents;

    public override int Length => Contents.Length;
}
