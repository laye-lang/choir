using System.Numerics;

using Choir.Front.Laye.Syntax;

namespace Choir.Front.Laye.Sema;

public enum CastKind
{
    Invalid,
    //Dependent,
    IntegralTruncate,
    IntegralZeroExtend,
    IntegralSignExtend,
    IntegerToFloat,
    FloatToInteger,
    BitCast,
    LValueBitCast,
    LValueToRValueBitCast,
    LValueToRValue,
    ReferenceToLValue,
    LValueToReference,
    PointerToLValue,
    //ReferenceToPointer,
    //PointerToReference,
    Implicit,
    NoOp,
}

[Flags]
public enum BinaryOperatorKind : long
{
    Undefined = 0,

    Add = 1L << 0,
    Sub = 1L << 1,
    Mul = 1L << 2,
    Div = 1L << 3,
    UDiv = 1L << 4,
    FloorDiv = 1L << 5,
    Rem = 1L << 6,
    URem = 1L << 7,

    Eq = 1L << 10,
    Neq = 1L << 11,
    Lt = 1L << 12,
    Le = 1L << 13,
    Gt = 1L << 14,
    Ge = 1L << 15,

    And = 1L << 20,
    Or = 1L << 21,
    Xor = 1L << 22,
    Shl = 1L << 23,
    Shr = 1L << 24,
    LShr = 1L << 25,

    LogAnd = 1L << 30,
    LogOr = 1L << 31,

    Bool = 1L << 50,
    Integer = 1L << 51,
    Pointer = 1L << 52,
    Buffer = 1L << 53,

    OperatorMask = (1L << 50) - 1,
    TypeMask = ~OperatorMask,
}

public static class BinaryOperatorKindExtensions
{
    public static bool IsComparisonOperator(this BinaryOperatorKind kind) => (kind & BinaryOperatorKind.OperatorMask) switch
    {
        BinaryOperatorKind.Eq or
        BinaryOperatorKind.Neq or
        BinaryOperatorKind.Lt or
        BinaryOperatorKind.Le or
        BinaryOperatorKind.Gt or
        BinaryOperatorKind.Ge
            => true,
        _ => false,
    };
}

public sealed class SemaExprLookup(Location location, SemaTypeQual type, SemaDeclNamed? entity)
    : SemaExpr(location, type)
{
    public SemaDeclNamed? ReferencedEntity { get; } = entity;
}

public sealed class SemaExprOverloadSet(Location location, SemaDeclNamed[] overloads)
    : SemaExpr(location, SemaTypeOverloadSet.Instance.Qualified(location))
{
    public IReadOnlyList<SemaDeclNamed> Overloads { get; } = overloads;
}

public abstract class SemaExprField(Location location, SemaExpr operand, string fieldName, SemaTypeQual type)
    : SemaExpr(location, type)
{
    public SemaExpr Operand { get; } = operand;
    public string FieldText { get; } = fieldName;
    public override IEnumerable<BaseSemaNode> Children { get; } = [operand];
}

public sealed class SemaExprFieldBadIndex(Location location, SemaExpr operand, string fieldName)
    : SemaExprField(location, operand, fieldName, SemaTypePoison.InstanceQualified)
{
}

public sealed class SemaExprFieldStructIndex(Location location, SemaExpr structOperand, SemaDeclField field, Size fieldOffset)
    : SemaExprField(location, structOperand, field.Name, field.FieldType)
{
    public Size FieldOffset { get; } = fieldOffset;
    public Align FieldAlign { get; } = field.FieldType.Align;
}

public abstract class SemaExprIndex(Location location, SemaTypeQual type)
    : SemaExpr(location, type)
{
}

public sealed class SemaExprIndexBuffer(SemaTypeQual type, SemaExpr operand, SemaExpr index)
    : SemaExprIndex(operand.Location, type)
{
    public SemaExpr Operand { get; } = operand;
    public SemaExpr Index { get; } = index;
    public override IEnumerable<BaseSemaNode> Children { get; } = [operand, index];
}

public sealed class SemaExprIndexArray(SemaTypeQual type, SemaExpr operand, IReadOnlyList<SemaExpr> indices)
    : SemaExprIndex(operand.Location, type)
{
    public SemaExpr Operand { get; } = operand;
    public IReadOnlyList<SemaExpr> Indices { get; } = indices;
    public override IEnumerable<BaseSemaNode> Children { get; } = [operand, ..indices];
}

public sealed class SemaExprIndexInvalid(SemaExpr operand, IReadOnlyList<SemaExpr> indices)
    : SemaExprIndex(operand.Location, SemaTypePoison.InstanceQualified)
{
    public SemaExpr Operand { get; } = operand;
    public IReadOnlyList<SemaExpr> Indices { get; } = indices;
    public override IEnumerable<BaseSemaNode> Children { get; } = [operand, ..indices];
}

public abstract class SemaExprUnary(SyntaxToken operatorToken, SemaTypeQual type, SemaExpr operand)
    : SemaExpr(operatorToken.Location, type)
{
    public SyntaxToken OperatorToken { get; } = operatorToken;
    public SemaExpr Operand { get; } = operand;
    public override IEnumerable<BaseSemaNode> Children { get; } = [operand];
}

public sealed class SemaExprUnaryUndefined(SyntaxToken operatorToken, SemaExpr operand)
    : SemaExprUnary(operatorToken, SemaTypePoison.InstanceQualified, operand)
{
}

public abstract class SemaExprBinary(SyntaxToken operatorToken, SemaTypeQual type, SemaExpr left, SemaExpr right)
    : SemaExpr(operatorToken.Location, type)
{
    public SyntaxToken OperatorToken { get; } = operatorToken;
    public SemaExpr Left { get; } = left;
    public SemaExpr Right { get; } = right;
    public override IEnumerable<BaseSemaNode> Children { get; } = [left, right];
}

public sealed class SemaExprBinaryBuiltIn(BinaryOperatorKind kind, SyntaxToken operatorToken, SemaTypeQual type, SemaExpr left, SemaExpr right)
    : SemaExprBinary(operatorToken, type, left, right)
{
    public BinaryOperatorKind Kind { get; } = kind;
}

public sealed class SemaExprEvaluatedConstant(SemaExpr sourceExpr, EvaluatedConstant value)
    : SemaExpr(sourceExpr.Location, sourceExpr.Type)
{
    public SemaExpr SourceExpr { get; } = sourceExpr;
    public EvaluatedConstant Value { get; } = value;
    public override IEnumerable<BaseSemaNode> Children { get; } = [sourceExpr];
}

public sealed class SemaExprLiteralInteger(Location location, BigInteger literalValue, SemaTypeQual type)
    : SemaExpr(location, type)
{
    public BigInteger LiteralValue { get; } = literalValue;
}

public sealed class SemaExprLiteralString(Location location, string literalValue, SemaTypeQual type)
    : SemaExpr(location, type)
{
    public string LiteralValue { get; } = literalValue;
}

public sealed class SemaExprLiteralBool(Location location, bool literalValue, SemaTypeQual type)
    : SemaExpr(location, type)
{
    public bool LiteralValue { get; } = literalValue;
}

public sealed class SemaExprLiteralNil(Location location, SemaTypeQual? type = null)
    : SemaExpr(location, type ?? SemaTypeNil.Instance.Qualified(location))
{
}

public sealed class SemaExprCast(Location location, CastKind castKind, SemaTypeQual type, SemaExpr operand)
    : SemaExpr(location, type)
{
    public CastKind CastKind { get; } = castKind;
    public SemaExpr Operand { get; } = operand;
    public override IEnumerable<BaseSemaNode> Children { get; } = [type, operand];
}

public sealed class SemaExprCall(Location location, SemaTypeQual type, SemaExpr callee, SemaExpr[] arguments)
    : SemaExpr(location, type)
{
    public SemaExpr Callee { get; } = callee;
    public IReadOnlyList<SemaExpr> Arguments { get; } = arguments;
    public override IEnumerable<BaseSemaNode> Children { get; } = [callee, ..arguments];
}

public sealed class SemaExprGrouped(Location location, SemaExpr inner)
    : SemaExpr(location, inner.Type)
{
    public SemaExpr Inner { get; } = inner;
    public override IEnumerable<BaseSemaNode> Children { get; } = [inner];
}

public sealed class SemaConstructorInitializer(Location location, SemaExpr value, Size offset)
    : SemaExpr(location, value.Type)
{
    public SemaExpr Value { get; } = value;
    public Size Offset { get; } = offset;
    public override IEnumerable<BaseSemaNode> Children { get; } = [value];
}

public sealed class SemaExprConstructor(Location location, SemaTypeQual type, IReadOnlyList<SemaConstructorInitializer> inits)
    : SemaExpr(location, type)
{
    public IReadOnlyList<SemaConstructorInitializer> Inits { get; } = inits;
    public override IEnumerable<BaseSemaNode> Children { get; } = [type, ..inits];
}

public sealed class SemaExprNew(Location location, SemaTypeQual type, IReadOnlyList<SemaExpr> @params, IReadOnlyList<SemaConstructorInitializer> inits)
    : SemaExpr(location, type)
{
    public IReadOnlyList<SemaExpr> Params { get; } = @params;
    public IReadOnlyList<SemaConstructorInitializer> Inits { get; } = inits;
    public override IEnumerable<BaseSemaNode> Children { get; } = [type, .. inits];
}

public sealed class SemaExprComplement(SemaExpr operand)
    : SemaExpr(operand.Location, operand.Type)
{
    public SemaExpr Operand { get; } = operand;
    public override IEnumerable<BaseSemaNode> Children { get; } = [operand];
}

public sealed class SemaExprNegate(SemaExpr operand)
    : SemaExpr(operand.Location, operand.Type)
{
    public SemaExpr Operand { get; } = operand;
    public override IEnumerable<BaseSemaNode> Children { get; } = [operand];
}

public sealed class SemaExprLogicalNot(SemaExpr operand)
    : SemaExpr(operand.Location, operand.Type)
{
    public SemaExpr Operand { get; } = operand;
    public override IEnumerable<BaseSemaNode> Children { get; } = [operand];
}
