namespace Choir.Front.Laye.Sema;

public sealed class SemaStmtXyzzy(Location location) : SemaStmt(location);

public sealed class SemaStmtExpr(SemaExpr expr) : SemaStmt(expr.Location)
{
    public SemaExpr Expr { get; } = expr;
    public override StmtControlFlow ControlFlow { get; } = expr.Type.IsNoReturn ? StmtControlFlow.Return : StmtControlFlow.Fallthrough;
    public override IEnumerable<BaseSemaNode> Children { get; } = [expr];
}

public sealed class SemaStmtAssign(SemaExpr target, SemaExpr value)
    : SemaStmt(target.Location)
{
    public SemaExpr Target { get; } = target;
    public SemaExpr Value { get; } = value;
    public override IEnumerable<BaseSemaNode> Children { get; } = [target, value];
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
    public override StmtControlFlow ControlFlow { get; } = StmtControlFlow.Return;
}

public sealed class SemaStmtReturnValue(Location location, SemaExpr value)
    : SemaStmt(location)
{
    public SemaExpr Value { get; } = value;
    public override StmtControlFlow ControlFlow { get; } = StmtControlFlow.Return;
    public override IEnumerable<BaseSemaNode> Children { get; } = [value];
}

public sealed class SemaStmtIfPrimary(Location location, SemaExpr condition, SemaStmt body)
    : SemaStmt(location)
{
    public SemaExpr Condition { get; } = condition;
    public SemaStmt Body { get; } = body;
    public override StmtControlFlow ControlFlow { get; } = body.ControlFlow;

    public override IEnumerable<BaseSemaNode> Children { get; } = [condition, body];
}

public sealed class SemaStmtIf(IReadOnlyList<SemaStmtIfPrimary> conditions, SemaStmt? elseBody)
    : SemaStmt(conditions[0].Location)
{
    public IReadOnlyList<SemaStmtIfPrimary> Conditions { get; } = conditions;
    public SemaStmt? ElseBody { get; } = elseBody;
    public override StmtControlFlow ControlFlow { get; } = conditions.Concat([elseBody]).Where(s => s is not null).Select(s => s!.ControlFlow)
        .Aggregate(StmtControlFlow.Return, (a, b) => (StmtControlFlow)Math.Min((int)a, (int)b));

    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            foreach (var condition in Conditions)
                yield return condition;
            if (ElseBody is not null)
                yield return ElseBody;
        }
    }
}

public sealed class SemaStmtDiscard(Location location, SemaExpr expr)
    : SemaStmt(location)
{
    public SemaExpr Expr { get; } = expr;
    public override IEnumerable<BaseSemaNode> Children { get; } = [expr];
}

public sealed class SemaStmtAssert(Location location, SemaExpr condition, string failureMessage)
    : SemaStmt(location)
{
    public SemaExpr Condition { get; } = condition;
    public string FailureMessage { get; } = failureMessage;
    public override IEnumerable<BaseSemaNode> Children { get; } = [condition];
}
