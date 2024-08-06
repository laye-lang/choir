namespace Choir.Front.Laye.Sema;

public sealed class SemaStmtExpr(SemaExpr expr) : SemaStmt(expr.Location)
{
    public SemaExpr Expr { get; } = expr;
    public override IEnumerable<BaseSemaNode> Children { get; } = [expr];
}
