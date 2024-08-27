using System.Numerics;

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

public sealed class SemaExprLiteralInteger(Location location, BigInteger literalValue, SemaTypeQual type)
    : SemaExpr(location, type)
{
    public BigInteger LiteralValue { get; } = literalValue;
}

public sealed class SemaExprCast(Location location, CastKind castKind, SemaTypeQual type, SemaExpr operand)
    : SemaExpr(location, type)
{
    public CastKind CastKind { get; } = castKind;
    public SemaExpr Operand { get; } = operand;
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
