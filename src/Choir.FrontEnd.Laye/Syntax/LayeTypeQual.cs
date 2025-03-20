using Choir.Formatting;
using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public sealed class LayeTypeQual
    : LayeExpr, IMarkupFormattable
{
    public LayeType Unqualified { get; set;  }
    public LayeTypeQual Canonical => Unqualified.Canonical.Qualified(Range, Qualifiers);

    public LayeTypeQualifier Qualifiers { get; set; }

    public LayeTypeQual(SourceRange range, LayeType type, LayeTypeQualifier qualifiers)
        : base(range, LayeTypeTypeInfo.Instance.Qualified(range))
    {
        Unqualified = type;
        Qualifiers = qualifiers;
    }

    public void BuildSpelling(MarkupBuilder builder)
    {
        Unqualified.BuildSpelling(builder);

        if (Qualifiers.HasFlag(LayeTypeQualifier.Mutable))
        {
            builder.Append(" ");
            builder.Append(MarkupColor.Blue, "mut");
        }
    }

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
