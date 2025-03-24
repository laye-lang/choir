using System.Numerics;

namespace Choir;

public enum EvaluatedConstantKind
{
    Bool,
    Integer,
    Float,
    String,
    Range,
}

public readonly record struct ConstRange(BigInteger Begin, BigInteger End);

public readonly struct EvaluatedConstant
{
    public readonly EvaluatedConstantKind Kind;

    public readonly bool BoolValue;
    public readonly BigInteger IntegerValue;
    public readonly string StringValue;
    public readonly ConstRange RangeValue;

    public EvaluatedConstant(bool boolValue)
    {
        Kind = EvaluatedConstantKind.Bool;
        BoolValue = boolValue;
        StringValue = "";
    }

    public EvaluatedConstant(BigInteger integerValue)
    {
        Kind = EvaluatedConstantKind.Integer;
        IntegerValue = integerValue;
        StringValue = "";
    }

    public EvaluatedConstant(string stringValue)
    {
        Kind = EvaluatedConstantKind.String;
        StringValue = stringValue;
    }

    public EvaluatedConstant(ConstRange rangeValue)
    {
        Kind = EvaluatedConstantKind.Range;
        RangeValue = rangeValue;
        StringValue = "";
    }
}
