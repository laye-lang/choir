namespace Choir.Formatting;

public sealed class MarkupLiteral
    : Markup
{
    public string Literal { get; }

    public override int Length => Literal.Length;

    public MarkupLiteral(string literal)
    {
        Literal = literal;
    }
}
