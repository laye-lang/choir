namespace Choir.Front.Laye.Sema;

public sealed class SemaStmtExpr(SemaExpr expr) : SemaStmt(expr.Location)
{
    public SemaExpr Expr { get; } = expr;
    public override IEnumerable<BaseSemaNode> Children { get; } = [expr];
}

public sealed class SemaStmtCompound(Location location, IReadOnlyList<SemaStmt> statements)
    : SemaStmt(location)
{
    public IReadOnlyList<SemaStmt> Statements { get; } = statements;
    public override IEnumerable<BaseSemaNode> Children { get; } = statements;
}

public sealed class SemaStmtReturnVoid(Location location)
    : SemaStmt(location)
{
}

public sealed class SemaStmtReturnValue(Location location, SemaExpr value)
    : SemaStmt(location)
{
    public SemaExpr Value { get; } = value;
    public override IEnumerable<BaseSemaNode> Children { get; } = [value];
}
