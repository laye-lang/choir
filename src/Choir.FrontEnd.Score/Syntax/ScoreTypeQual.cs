using Choir.Formatting;
using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public sealed class ScoreTypeQual
    : ScoreExpr, IMarkupFormattable
{
    public ScoreType Unqualified { get; set;  }
    public ScoreTypeQual Canonical => Unqualified.Canonical.Qualified(Range, Qualifiers);

    public ScoreTypeQualifier Qualifiers { get; set; }

    public ScoreTypeQual(SourceRange range, ScoreType type, ScoreTypeQualifier qualifiers)
        : base(range, ScoreTypeTypeInfo.Instance.Qualified(range))
    {
        Unqualified = type;
        Qualifiers = qualifiers;
    }

    public void BuildSpelling(MarkupBuilder builder)
    {
        Unqualified.BuildSpelling(builder);

        if (Qualifiers != ScoreTypeQualifier.None)
        {
            throw new NotImplementedException("Need to implement spelling for qualified types in Score.");
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
