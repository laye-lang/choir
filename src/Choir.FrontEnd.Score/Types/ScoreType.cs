using Choir.Formatting;

namespace Choir.FrontEnd.Score.Types;

public enum ScoreTypeComparison
{
    WithIdenticalQualifiers,
    WithQualifierConversions,
    TypeOnly,
}

public abstract class ScoreType
    : IEquatable<ScoreType>
    , IMarkupFormattable
{
    private static long _counter = 0;

    public long Id { get; }

    public virtual ScoreType Canonical => this;

    public abstract Size Size { get; }
    public virtual Align Align => Align.ForBytes(Size.Bytes);

    public virtual bool IsVoid { get; } = false;
    public virtual bool IsNoreturn { get; } = false;
    public virtual bool IsBool { get; } = false;
    public virtual bool IsInteger { get; } = false;
    public virtual bool IsFloat { get; } = false;

    public virtual bool IsExplicitlySized { get; } = false;

    protected ScoreType()
    {
        Id = Interlocked.Increment(ref _counter);
    }

    public ScoreTypeQual Qualified(ScoreTypeQualifier qualifiers = ScoreTypeQualifier.None)
    {
        return new(this, qualifiers);
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

    public override abstract int GetHashCode();

    public override bool Equals(object? obj) => obj is ScoreType other && Equals(other);
    public bool Equals(ScoreType? other) => other is not null && TypeEquals(other, ScoreTypeComparison.WithIdenticalQualifiers);
    public abstract bool TypeEquals(ScoreType other, ScoreTypeComparison comp = ScoreTypeComparison.WithIdenticalQualifiers);

    void IMarkupFormattable.BuildMarkup(MarkupBuilder builder) => BuildSpelling(builder);
}
