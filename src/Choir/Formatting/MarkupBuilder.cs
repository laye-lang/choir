
namespace Choir.Formatting;

public sealed class MarkupBuilder
{
    private readonly List<Markup> _markupNodes = [];

    public Markup Markup => new MarkupSequence(_markupNodes);

    public void AppendLine() => Append("\n");
    public void Append(string s)
    {
        _markupNodes.Add(new MarkupLiteral(s));
    }

    public void Append(Markup markup)
    {
        _markupNodes.Add(markup);
    }

    public void Append(IMarkupFormattable formattable)
    {
        formattable.BuildMarkup(this);
    }

    public void Append(MarkupColor color, string s)
    {
        _markupNodes.Add(new MarkupScopedColor(color, new MarkupLiteral(s)));
    }

    public void Append(MarkupColor color, Markup markup)
    {
        _markupNodes.Add(new MarkupScopedColor(color, markup));
    }

    public void Append(MarkupColor color, IMarkupFormattable formattable)
    {
        var builder = new MarkupBuilder();
        formattable.BuildMarkup(builder);
        _markupNodes.Add(new MarkupScopedColor(color, builder.Markup));
    }

    public void Append(MarkupColor color, Action<MarkupBuilder> callback)
    {
        var builder = new MarkupBuilder();
        callback(builder);
        _markupNodes.Add(new MarkupScopedColor(color, builder.Markup));
    }

    public void Append(MarkupStyle style, string s)
    {
        _markupNodes.Add(new MarkupScopedStyle(style, new MarkupLiteral(s)));
    }

    public void Append(MarkupStyle style, Markup markup)
    {
        _markupNodes.Add(new MarkupScopedStyle(style, markup));
    }

    public void Append(MarkupStyle style, IMarkupFormattable formattable)
    {
        var builder = new MarkupBuilder();
        formattable.BuildMarkup(builder);
        _markupNodes.Add(new MarkupScopedStyle(style, builder.Markup));
    }

    public void Append(MarkupStyle style, Action<MarkupBuilder> callback)
    {
        var builder = new MarkupBuilder();
        callback(builder);
        _markupNodes.Add(new MarkupScopedStyle(style, builder.Markup));
    }

    public void Append(MarkupSemantic semantic, string s)
    {
        _markupNodes.Add(new MarkupScopedSemantic(semantic, new MarkupLiteral(s)));
    }

    public void Append(MarkupSemantic semantic, Markup markup)
    {
        _markupNodes.Add(new MarkupScopedSemantic(semantic, markup));
    }

    public void Append(MarkupSemantic semantic, IMarkupFormattable formattable)
    {
        var builder = new MarkupBuilder();
        formattable.BuildMarkup(builder);
        _markupNodes.Add(new MarkupScopedSemantic(semantic, builder.Markup));
    }

    public void Append(MarkupSemantic semantic, Action<MarkupBuilder> callback)
    {
        var builder = new MarkupBuilder();
        callback(builder);
        _markupNodes.Add(new MarkupScopedSemantic(semantic, builder.Markup));
    }
}
