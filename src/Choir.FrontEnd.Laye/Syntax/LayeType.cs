using Choir.Formatting;
using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class LayeType
    : LayeSyntaxNode, IMarkupFormattable
{
    public virtual LayeType Canonical => this;

    protected LayeType()
    {
    }

    public LayeTypeQual Qualified(SourceRange range, LayeTypeQualifier qualifiers = LayeTypeQualifier.None)
    {
        return new(range, this, qualifiers);
    }

    public abstract void BuildSpelling(MarkupBuilder builder);
    public Markup GetSpellingMarkup()
    {
        var builder = new MarkupBuilder();
        BuildSpelling(builder);
        return builder.Markup;
    }

    public string GetSpelling() => GetSpellingMarkup().RenderToString();
    public override string ToString() => GetSpelling();

    void IMarkupFormattable.BuildMarkup(MarkupBuilder builder) => BuildSpelling(builder);
}
