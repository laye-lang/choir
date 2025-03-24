using Choir.Formatting;

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

public sealed class ScoreTypeBuiltin(ScoreBuiltinTypeKind kind, Size size, Align align)
    : ScoreType
{
    public static ScoreTypeBuiltin Void { get; } = new(ScoreBuiltinTypeKind.Void, Size.Zero, Align.ByteAligned);
    public static ScoreTypeBuiltin Noreturn { get; } = new(ScoreBuiltinTypeKind.Noreturn, Size.Zero, Align.ByteAligned);
    public static ScoreTypeBuiltin Bool { get; } = new(ScoreBuiltinTypeKind.Bool, Size.FromBytes(1), Align.ByteAligned);
    public static ScoreTypeBuiltin Float32 { get; } = new(ScoreBuiltinTypeKind.FloatSized, Size.FromBytes(4), Align.ForBytes(4));
    public static ScoreTypeBuiltin Float64 { get; } = new(ScoreBuiltinTypeKind.FloatSized, Size.FromBytes(8), Align.ForBytes(8));

    public ScoreBuiltinTypeKind Kind { get; } = kind;

    public override Size Size { get; } = size;
    public override Align Align { get; } = align;

    public override bool IsVoid { get; } = kind is ScoreBuiltinTypeKind.Void;
    public override bool IsNoreturn { get; } = kind is ScoreBuiltinTypeKind.Noreturn;
    public override bool IsBool { get; } = kind is ScoreBuiltinTypeKind.Bool;
    public override bool IsInteger { get; } = kind is ScoreBuiltinTypeKind.Int or ScoreBuiltinTypeKind.IntSized;
    public override bool IsFloat { get; } = kind is ScoreBuiltinTypeKind.FloatSized;

    public override bool IsExplicitlySized { get; } = kind is ScoreBuiltinTypeKind.IntSized or ScoreBuiltinTypeKind.FloatSized;

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
