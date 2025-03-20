namespace Choir.Formatting;

public abstract class Markup
{
    public static implicit operator Markup(string literalText) => new MarkupLiteral(literalText);

    public abstract int Length { get; }

    public override string ToString() => RenderToString();

    public string RenderToString()
    {
        var renderer = new MarkupStringRenderer();
        return renderer.Render(this);
    }
}
