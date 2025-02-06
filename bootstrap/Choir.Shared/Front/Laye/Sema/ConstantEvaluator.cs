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

            case SemaExprLiteralBool boolLiteral:
            {
                value = new EvaluatedConstant(boolLiteral.LiteralValue);
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

            case SemaExprNegate negate:
            {
                if (!TryEvaluate(negate.Operand, out var operandConst))
                    return false;

                if (operandConst.Kind == EvaluatedConstantKind.Integer)
                {
                    value = new EvaluatedConstant(-operandConst.IntegerValue);
                    return true;
                }

                return false;
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
                    case BinaryOperatorKind.Neq | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue != rightConst.IntegerValue); break;
                    case BinaryOperatorKind.Lt | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue < rightConst.IntegerValue); break;
                    case BinaryOperatorKind.Le | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue <= rightConst.IntegerValue); break;
                    case BinaryOperatorKind.Gt | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue > rightConst.IntegerValue); break;
                    case BinaryOperatorKind.Ge | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue >= rightConst.IntegerValue); break;

                    case BinaryOperatorKind.Add | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue + rightConst.IntegerValue); break;
                    case BinaryOperatorKind.Sub | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue - rightConst.IntegerValue); break;
                    case BinaryOperatorKind.Mul | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue * rightConst.IntegerValue); break;
                    case BinaryOperatorKind.Div | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue / rightConst.IntegerValue); break;
                    case BinaryOperatorKind.UDiv | BinaryOperatorKind.Integer:
                    {
                        var lu = new BigInteger(leftConst.IntegerValue.ToByteArray(), isUnsigned: true);
                        var ru = new BigInteger(leftConst.IntegerValue.ToByteArray(), isUnsigned: true);
                        value = new EvaluatedConstant(lu / ru);
                    } break;
                    case BinaryOperatorKind.Rem | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue % rightConst.IntegerValue); break;
                    case BinaryOperatorKind.URem | BinaryOperatorKind.Integer:
                    {
                        var lu = new BigInteger(leftConst.IntegerValue.ToByteArray(), isUnsigned: true);
                        var ru = new BigInteger(leftConst.IntegerValue.ToByteArray(), isUnsigned: true);
                        value = new EvaluatedConstant(lu % ru);
                    } break;

                    case BinaryOperatorKind.And | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue & rightConst.IntegerValue); break;
                    case BinaryOperatorKind.Or | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue | rightConst.IntegerValue); break;
                    case BinaryOperatorKind.Xor | BinaryOperatorKind.Integer: value = new EvaluatedConstant(leftConst.IntegerValue ^ rightConst.IntegerValue); break;
                    case BinaryOperatorKind.Shl | BinaryOperatorKind.Integer:
                    {
                        if (rightConst.IntegerValue.GetBitLength() > 32)
                            return false;
                        value = new EvaluatedConstant(leftConst.IntegerValue << (int)rightConst.IntegerValue);
                    } break;
                    case BinaryOperatorKind.Shr | BinaryOperatorKind.Integer:
                    {
                        if (rightConst.IntegerValue.GetBitLength() > 32)
                            return false;
                        value = new EvaluatedConstant(leftConst.IntegerValue >> (int)rightConst.IntegerValue);
                    } break;
                    case BinaryOperatorKind.LShr | BinaryOperatorKind.Integer:
                    {
                        if (rightConst.IntegerValue.GetBitLength() > 32)
                            return false;
                        value = new EvaluatedConstant(leftConst.IntegerValue >>> (int)rightConst.IntegerValue);
                    } break;
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

            case SemaExprSizeof @sizeof:
            {
                value = new EvaluatedConstant(@sizeof.Size.Bytes);
                return true;
            }

            case SemaExprCountof @countof:
            {
                SemaType type;
                if (@countof.Operand is SemaTypeQual typeQual)
                    type = typeQual.CanonicalType.Type;
                else if (@countof.Operand is SemaExprType exprType)
                    type = exprType.TypeExpr.CanonicalType.Type;
                else if (@countof.Operand is SemaExpr justExpr)
                    type = justExpr.Type.CanonicalType.Type;
                else return false;

                if (type is SemaTypeArray typeArray && typeArray.Arity == 1)
                {
                    value = new EvaluatedConstant(typeArray.Lengths[0]);
                    return true;
                }

                return false;
            }
        }
    }
}
