using System.Numerics;

using Choir.Front.Laye.Syntax;

namespace Choir.Front.Laye.Sema;

public enum CastKind
{
    Invalid,
    Dependent,
    BitCast,
    LValueBitCast,
    LValueToRValueBitCast,
    LValueToRValue,
    ReferenceToLValue,
    Implicit,
    NoOp,
}

[Flags]
public enum BinaryOperatorKind : long
{
    Undefined = 0,

    Add = 1 << 0,
    Sub = 1 << 1,
    Mul = 1 << 2,

    Integer = 1 << 50,

    OperatorMask = (1 << 50) - 1,
    TypeMask = ~OperatorMask,
}

public abstract class SemaExprLookup(Location location, SemaTypeQual type, SemaDeclNamed? entity)
    : SemaExpr(location, type)
{
    public SemaDeclNamed? ReferencedEntity { get; } = entity;
}

public sealed class SemaExprLookupSimple(Location location, string lookupName, SemaTypeQual type, SemaDeclNamed? entity)
    : SemaExprLookup(location, type, entity)
{
    public string Name { get; } = lookupName;
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

public sealed class SemaExprCast(Location location, CastKind castKind, SemaTypeQual type, SemaExpr operand)
    : SemaExpr(location, type)
{
    public CastKind CastKind { get; } = castKind;
    public SemaExpr Operand { get; } = operand;
    public override IEnumerable<BaseSemaNode> Children { get; } = [type, operand];
}

public sealed class SemaExprDereference : SemaExpr
{
    public static SemaExprDereference Create(ChoirContext context, Location location, SemaExpr expr)
    {
        context.Assert(expr.Type.Type is SemaTypePointer, location, "creating a dereference node requires the expression's type to be a pointer");
        return new(location, expr, ((SemaTypePointer)expr.Type.Type).ElementType);
    }

    public SemaExpr Expr { get; }

    private SemaExprDereference(Location location, SemaExpr expr, SemaTypeQual elementType)
        : base(location, elementType)
    {
        Expr = expr;
    }
}

public sealed class SemaExprCall(Location location, SemaTypeQual type, SemaExpr callee, SemaExpr[] arguments)
    : SemaExpr(location, type)
{
    public SemaExpr Callee { get; } = callee;
    public IReadOnlyList<SemaExpr> Arguments { get; } = arguments;
    public override IEnumerable<BaseSemaNode> Children { get; } = [callee, ..arguments];
}
