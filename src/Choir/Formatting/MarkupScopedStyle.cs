namespace Choir.Formatting;

public sealed class MarkupScopedStyle(MarkupStyle style, Markup contents)
    : Markup
{
    public MarkupStyle Style { get; } = style;
    public Markup Contents { get; } = contents;

    public override int Length => Contents.Length;
}
