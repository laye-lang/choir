using Choir.Formatting;
using Choir.FrontEnd.Score.Syntax;
using Choir.Source;

namespace Choir.FrontEnd.Score.Types;

public enum ScoreBuiltinTypeKind
{
    Void,
    Noreturn,
    Bool,
    Int,
    IntSized,
    FloatSized,
}

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

    protected ScoreType()
    {
        Id = Interlocked.Increment(ref _counter);
    }

    public ScoreSyntaxTypeQual Qualified(SourceRange range, ScoreTypeQualifier qualifiers = ScoreTypeQualifier.None)
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

    public override abstract int GetHashCode();

    public override bool Equals(object? obj) => obj is ScoreType other && Equals(other);
    public bool Equals(ScoreType? other) => other is not null && TypeEquals(other, ScoreTypeComparison.WithIdenticalQualifiers);
    public abstract bool TypeEquals(ScoreType other, ScoreTypeComparison comp = ScoreTypeComparison.WithIdenticalQualifiers);

    void IMarkupFormattable.BuildMarkup(MarkupBuilder builder) => BuildSpelling(builder);
}

public sealed class ScoreTypeBuiltin(ScoreBuiltinTypeKind kind, Size size, Align align)
    : ScoreType
{
    public ScoreBuiltinTypeKind Kind { get; } = kind;

    public override Size Size { get; } = size;
    public override Align Align { get; } = align;

    public override bool IsVoid { get; } = kind is ScoreBuiltinTypeKind.Void;
    public override bool IsNoreturn { get; } = kind is ScoreBuiltinTypeKind.Noreturn;
    public override bool IsBool { get; } = kind is ScoreBuiltinTypeKind.Bool;
    public override bool IsInteger { get; } = kind is ScoreBuiltinTypeKind.Int or ScoreBuiltinTypeKind.IntSized;
    public override bool IsFloat { get; } = kind is ScoreBuiltinTypeKind.FloatSized;

    public bool IsExplicitlySized { get; } = kind is ScoreBuiltinTypeKind.IntSized or ScoreBuiltinTypeKind.FloatSized;

    public override bool TypeEquals(ScoreType other, ScoreTypeComparison comp = ScoreTypeComparison.WithIdenticalQualifiers)
    {
        if (Id == other.Id) return true;
        return other is ScoreTypeBuiltin builtin && Kind == builtin.Kind && Size == builtin.Size && Align == builtin.Align;
    }

    public override int GetHashCode() => HashCode.Combine(Kind, Size.Bits);

    public override void BuildSpelling(MarkupBuilder builder)
    {
        if (IsExplicitlySized)
            builder.Append(MarkupSemantic.KeywordType, $"{(IsInteger ? "int" : "float")}{Size.Bits}");
        else builder.Append(MarkupSemantic.KeywordType, Kind.ToString().ToLower());
    }
}
