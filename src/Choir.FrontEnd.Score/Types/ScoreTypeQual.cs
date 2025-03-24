using Choir.Formatting;

namespace Choir.FrontEnd.Score.Types;

[Flags]
public enum ScoreTypeQualifier
    : byte
{
    None = 0,
    Readonly = 1 << 0,
    Writeonly = 1 << 1,
}

public sealed class ScoreTypeQual(ScoreType unqualifiedType, ScoreTypeQualifier qualifiers)
    : IEquatable<ScoreTypeQual?>
    , IMarkupFormattable
{
    public ScoreType Unqualified { get; } = unqualifiedType;
    public ScoreTypeQual Canonical { get; } = unqualifiedType.Canonical.Qualified(qualifiers);

    public ScoreTypeQualifier Qualifiers { get; set; }

    public void BuildSpelling(MarkupBuilder builder)
    {
        Unqualified.BuildSpelling(builder);

        if (Qualifiers != ScoreTypeQualifier.None)
        {
            throw new NotImplementedException("Need to implement spelling for qualified types in Score.");
        }
    }

    public override int GetHashCode() => HashCode.Combine(32378677, Unqualified);

    public override bool Equals(object? obj) => obj is ScoreTypeQual other && Equals(other);
    public bool Equals(ScoreTypeQual? other) => other is not null && TypeEquals(other, ScoreTypeComparison.WithIdenticalQualifiers);
    public bool TypeEquals(ScoreTypeQual other, ScoreTypeComparison comp = ScoreTypeComparison.WithIdenticalQualifiers)
    {
        if (comp == ScoreTypeComparison.WithIdenticalQualifiers && Qualifiers != other.Qualifiers)
            return false;
        System.Diagnostics.Debug.Assert(comp != ScoreTypeComparison.WithQualifierConversions, "Need to implement qualifier conversion equality");
        return Unqualified.TypeEquals(other.Unqualified, comp);
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
