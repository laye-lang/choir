namespace Choir.Formatting;

public sealed class MarkupSequence
    : Markup
{
    public IReadOnlyList<Markup> Children { get; }

    public override int Length => Children.Sum(child => child.Length);

    public MarkupSequence(IReadOnlyList<Markup> children)
    {
        Children = children;
    }
}
