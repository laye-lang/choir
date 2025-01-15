using System.Diagnostics;
using System.Numerics;

namespace Choir.Front.Laye.Sema;

public enum EvaluatedConstantKind
{
    Bool,
    Integer,
    Float,
    String,
}

public readonly struct EvaluatedConstant
{
    public readonly EvaluatedConstantKind Kind;
    public readonly bool BoolValue;
    public readonly BigInteger IntegerValue;
    public readonly string StringValue;

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
}

public sealed class ConstantEvaluator
{
    public bool TryEvaluate(SemaExpr expr, out EvaluatedConstant value)
    {
        value = default;
        switch (expr)
        {
            default: return false;

            case SemaExprEvaluatedConstant eval:
            {
                value = eval.Value;
                return true;
            }

            case SemaExprLiteralInteger integerLiteral:
            {
                value = new EvaluatedConstant(integerLiteral.LiteralValue);
                return true;
            }

            case SemaExprLiteralString stringLiteral:
            {
                value = new EvaluatedConstant(stringLiteral.LiteralValue);
                return true;
            }

            case SemaExprBinaryBuiltIn binaryBuiltIn:
            {
                if (!TryEvaluate(binaryBuiltIn.Left, out var leftConst))
                    return false;

                if (!TryEvaluate(binaryBuiltIn.Right, out var rightConst))
                    return false;

                switch (binaryBuiltIn.Kind)
                {
                    default: return false;

                    case BinaryOperatorKind.Eq | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue == rightConst.IntegerValue); break;
                    case BinaryOperatorKind.Add | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue + rightConst.IntegerValue); break;
                    case BinaryOperatorKind.Sub | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue - rightConst.IntegerValue); break;
                }

                return true;
            }

            case SemaExprCast cast:
            {
                if (TryEvaluate(cast.Operand, out var operandConst))
                {
                    value = operandConst;
                    return true;
                }

                return false;
            }
        }
    }
}
