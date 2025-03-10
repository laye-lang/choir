namespace Choir.Formatting;

public sealed class MarkupScopedSemantic(MarkupSemantic semantic, Markup contents)
    : Markup
{
    public MarkupSemantic Semantic { get; } = semantic;
    public Markup Contents { get; } = contents;

    public override int Length => Contents.Length;
}
