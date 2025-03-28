using Choir.Front.Laye.Syntax;

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

public sealed record class SemaDeferStackNode
{
    public required SemaDeferStackNode? Previous { get; init; }
    public required SemaStmtDefer Defer { get; init; }
}

public sealed class SemaStmtCompound(Location location, IReadOnlyList<SemaStmt> statements)
    : SemaStmt(location)
{
    public IReadOnlyList<SemaStmt> Statements { get; } = statements;

    public SemaDeferStackNode? StartDefer { get; set; }
    public SemaDeferStackNode? EndDefer { get; set; }

    public override StmtControlFlow ControlFlow
    {
        get
        {
            foreach (var child in Statements)
            {
                if (child.ControlFlow != StmtControlFlow.Fallthrough)
                    return child.ControlFlow;
            }

            return StmtControlFlow.Fallthrough;
        }
    }

    public override IEnumerable<BaseSemaNode> Children { get; } = statements;
}

public sealed class SemaStmtReturnVoid(Location location)
    : SemaStmt(location)
{
    public SemaDeferStackNode? Defer { get; set; }
    public override StmtControlFlow ControlFlow { get; } = StmtControlFlow.Return;
}

public sealed class SemaStmtReturnValue(Location location, SemaExpr value)
    : SemaStmt(location)
{
    public SemaExpr Value { get; } = value;
    public SemaDeferStackNode? Defer { get; set; }
    public override StmtControlFlow ControlFlow { get; } = StmtControlFlow.Return;
    public override IEnumerable<BaseSemaNode> Children { get; } = [value];
}

public sealed class SemaStmtBreak(Location location, SemaStmt? breakTarget)
    : SemaStmt(location)
{
    public SemaStmt? BreakTarget { get; } = breakTarget;

    // The 'start defer' at the opening of the scope associated with the break target
    public SemaDeferStackNode? StartDefer { get; set; }
    // The most recent defer as of this break statement
    public SemaDeferStackNode? EndDefer { get; set; }

    public override StmtControlFlow ControlFlow { get; } = StmtControlFlow.Jump;
}

public sealed class SemaStmtContinue(Location location, SemaStmt? continueTarget)
    : SemaStmt(location)
{
    public SemaStmt? ContinueTarget { get; } = continueTarget;

    // The 'start defer' at the opening of the scope associated with the continue target
    public SemaDeferStackNode? StartDefer { get; set; }
    // The most recent defer as of this continue statement
    public SemaDeferStackNode? EndDefer { get; set; }

    public override StmtControlFlow ControlFlow { get; } = StmtControlFlow.Jump;
}

public sealed class SemaStmtUnreachable(Location location)
    : SemaStmt(location)
{
    public override StmtControlFlow ControlFlow { get; } = StmtControlFlow.Return;
}

public sealed class SemaStmtDefer(Location location, SemaStmt deferred)
    : SemaStmt(location)
{
    public SemaStmt DeferredStatement { get; } = deferred;
    public override StmtControlFlow ControlFlow { get; } = deferred.ControlFlow;
    public override IEnumerable<BaseSemaNode> Children { get; } = [deferred];
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
    public override StmtControlFlow ControlFlow { get; } = elseBody is null ? StmtControlFlow.Fallthrough :
        conditions.Concat([elseBody]).Where(s => s is not null).Select(s => s!.ControlFlow)
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

public sealed class SemaStmtWhileLoop(Location location)
    : SemaStmt(location)
{
    public SemaExpr? Condition { get; set; }
    public SemaStmt? Body { get; set; }
    public SemaStmt? ElseBody { get; set; }

    public override StmtControlFlow ControlFlow => Body is null || ElseBody is null ? StmtControlFlow.Fallthrough :
        (StmtControlFlow)Math.Min((int)Body.ControlFlow, (int)ElseBody.ControlFlow);

    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            if (Condition is not null)
                yield return Condition;
            if (Body is not null)
                yield return Body;
            if (ElseBody is not null)
                yield return ElseBody;
        }
    }
}

public sealed class SemaStmtForLoop(Location location)
    : SemaStmt(location)
{
    // remember: SemaDecl inherits SemaStmt, so this covers both cases
    public SemaStmt? Initializer { get; set; }
    public SemaExpr? Condition { get; set; }
    public SemaStmt? Increment { get; set; }
    public SemaStmt? Body { get; set; }

    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            if (Initializer is not null)
                yield return Initializer;
            if (Condition is not null)
                yield return Condition;
            if (Increment is not null)
                yield return Increment;
            if (Body is not null)
                yield return Body;
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

public sealed class SemaStmtIncrement(SemaExpr operand)
    : SemaStmt(operand.Location)
{
    public SemaExpr Operand { get; } = operand;
    public override IEnumerable<BaseSemaNode> Children { get; } = [operand];
}

public sealed class SemaStmtDecrement(SemaExpr operand)
    : SemaStmt(operand.Location)
{
    public SemaExpr Operand { get; } = operand;
    public override IEnumerable<BaseSemaNode> Children { get; } = [operand];
}
