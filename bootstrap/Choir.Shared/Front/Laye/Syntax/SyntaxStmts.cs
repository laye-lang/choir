namespace Choir.Front.Laye.Syntax;

public sealed class SyntaxStmtExpr(SyntaxNode expr, SyntaxToken? tokenSemiColon)
    : SyntaxNode(expr.Location)
{
    public SyntaxNode Expr { get; } = expr;
    public SyntaxToken? TokenSemiColon { get; } = tokenSemiColon;
    public override IEnumerable<SyntaxNode> Children { get; } = tokenSemiColon is not null ? [expr, tokenSemiColon] : [expr];
}

public sealed class SyntaxStmtAssign(SyntaxNode lhs, SyntaxToken tokenAssignOp, SyntaxNode rhs, SyntaxToken? tokenSemiColon)
    : SyntaxNode(tokenAssignOp.Location)
{
    public SyntaxNode Left { get; } = lhs;
    public SyntaxToken TokenAssignOp { get; } = tokenAssignOp;
    public SyntaxNode Right { get; } = rhs;
    public SyntaxToken? TokenSemiColon { get; } = tokenSemiColon;

    public override IEnumerable<SyntaxNode> Children { get; } = tokenSemiColon is not null ? [lhs, tokenAssignOp, rhs, tokenSemiColon] : [lhs, tokenAssignOp, rhs];
}

public sealed class SyntaxCompound(Location location, IReadOnlyList<SyntaxNode> body)
    : SyntaxNode(location)
{
    public IReadOnlyList<SyntaxNode> Body { get; } = body;
    public override IEnumerable<SyntaxNode> Children { get; } = body;
}

public sealed class SyntaxIfPrimary(SyntaxToken tokenIf, SyntaxNode condition, SyntaxNode body)
    : SyntaxNode(tokenIf.Location)
{
    public SyntaxToken TokenIf { get; } = tokenIf;
    public SyntaxNode Condition { get; } = condition;
    public SyntaxNode Body { get; } = body;

    public override IEnumerable<SyntaxNode> Children { get; } = [tokenIf, condition, body];
}

public sealed class SyntaxIf(IReadOnlyList<SyntaxIfPrimary> conditions, SyntaxToken? tokenElse, SyntaxNode? elseBody)
    : SyntaxNode(conditions.Count == 0 ? Location.Nowhere : conditions[0].Location)
{
    public IReadOnlyList<SyntaxIfPrimary> Conditions { get; } = conditions;
    public SyntaxToken? TokenElse { get; } = tokenElse;
    public SyntaxNode? ElseBody { get; } = elseBody;

    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            foreach (var condition in Conditions)
                yield return condition;
            if (TokenElse is not null)
                yield return TokenElse;
            if (ElseBody is not null)
                yield return ElseBody;
        }
    }
}

public sealed class SyntaxStaticIf(SyntaxToken tokenStatic, IReadOnlyList<SyntaxIfPrimary> conditions, SyntaxToken? tokenElse, SyntaxNode? elseBody)
    : SyntaxNode(tokenStatic.Location)
{
    public SyntaxToken TokenStatic { get; } = tokenStatic;
    public IReadOnlyList<SyntaxIfPrimary> Conditions { get; } = conditions;
    public SyntaxToken? TokenElse { get; } = tokenElse;
    public SyntaxNode? ElseBody { get; } = elseBody;

    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return TokenStatic;
            foreach (var condition in Conditions)
                yield return condition;
            if (TokenElse is not null)
                yield return TokenElse;
            if (ElseBody is not null)
                yield return ElseBody;
        }
    }
}

public sealed class SyntaxSwitchCase(SyntaxToken tokenCase, SyntaxNode casePattern, SyntaxToken? tokenIf, SyntaxNode? guardClause, SyntaxToken tokenColon, SyntaxNode? caseBody)
    : SyntaxNode(tokenCase.Location)
{
    public SyntaxToken TokenCase { get; } = tokenCase;
    public SyntaxNode CasePattern { get; } = casePattern;
    public SyntaxToken? TokenIf { get; } = tokenIf;
    public SyntaxNode? GuardClause { get; } = guardClause;
    public SyntaxToken TokenColon { get; } = tokenColon;
    public SyntaxNode? CaseBody { get; } = caseBody;

    public bool IsImplicitFallthrough { get; } = caseBody is null;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return TokenCase;
            yield return CasePattern;
            if (TokenIf is not null)
                yield return TokenIf;
            if (GuardClause is not null)
                yield return GuardClause;
            yield return TokenColon;
            if (CaseBody is not null)
                yield return CaseBody;
        }
    }
}

public sealed class SyntaxStmtSwitch(SyntaxToken tokenSwitch)
    : SyntaxNode(tokenSwitch.Location)
{
    public SyntaxToken TokenSwitch { get; } = tokenSwitch;

    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return TokenSwitch;
        }
    }
}

public sealed class SyntaxStmtReturn(SyntaxToken tokenReturn, SyntaxNode? value, SyntaxToken tokenSemiColon)
    : SyntaxNode(tokenReturn.Location)
{
    public SyntaxToken TokenReturn { get; } = tokenReturn;
    public SyntaxNode? Value { get; } = value;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;

    public override IEnumerable<SyntaxNode> Children { get; } = value is null ? [tokenReturn, tokenSemiColon] : [tokenReturn, value, tokenSemiColon];
}

public sealed class SyntaxStmtYield(SyntaxToken tokenYield, SyntaxNode value, SyntaxToken tokenSemiColon)
    : SyntaxNode(tokenYield.Location)
{
    public SyntaxToken TokenYield { get; } = tokenYield;
    public SyntaxNode Value { get; } = value;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;

    public override IEnumerable<SyntaxNode> Children { get; } = [tokenYield, value, tokenSemiColon];
}

public sealed class SyntaxStmtBreak(SyntaxToken tokenBreak, SyntaxToken? target, SyntaxToken tokenSemiColon)
    : SyntaxNode(tokenBreak.Location)
{
    public SyntaxToken TokenReturn { get; } = tokenBreak;
    public SyntaxToken? Target { get; } = target;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;

    public override IEnumerable<SyntaxNode> Children { get; } = target is null ? [tokenBreak, tokenSemiColon] : [tokenBreak, target, tokenSemiColon];
}

public sealed class SyntaxStmtContinue(SyntaxToken tokenContinue, SyntaxToken? target, SyntaxToken tokenSemiColon)
    : SyntaxNode(tokenContinue.Location)
{
    public SyntaxToken TokenReturn { get; } = tokenContinue;
    public SyntaxToken? Target { get; } = target;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;

    public override IEnumerable<SyntaxNode> Children { get; } = target is null ? [tokenContinue, tokenSemiColon] : [tokenContinue, target, tokenSemiColon];
}

public sealed class SyntaxStmtGoto(SyntaxToken tokenGoto, SyntaxToken target, SyntaxToken tokenSemiColon)
    : SyntaxNode(tokenGoto.Location)
{
    public SyntaxToken TokenReturn { get; } = tokenGoto;
    public SyntaxToken Target { get; } = target;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;

    public override IEnumerable<SyntaxNode> Children { get; } = [tokenGoto, target, tokenSemiColon];
}

public sealed class SyntaxStmtLabel(SyntaxToken tokenLabel, SyntaxToken tokenColon)
    : SyntaxNode(tokenLabel.Location)
{
    public SyntaxToken TokenLabel { get; } = tokenLabel;
    public SyntaxToken TokenColon { get; } = tokenColon;

    public override IEnumerable<SyntaxNode> Children { get; } = [tokenLabel, tokenColon];
}

public sealed class SyntaxStmtXyzzy(SyntaxToken tokenXyzzy, SyntaxToken tokenSemiColon)
    : SyntaxNode(tokenXyzzy.Location)
{
    public SyntaxToken TokenXyzzy { get; } = tokenXyzzy;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;

    public override IEnumerable<SyntaxNode> Children { get; } = [tokenXyzzy, tokenSemiColon];
}

public sealed class SyntaxStmtUnreachable(SyntaxToken tokenUnreachable, SyntaxToken tokenSemiColon)
    : SyntaxNode(tokenUnreachable.Location)
{
    public SyntaxToken TokenUnreachable { get; } = tokenUnreachable;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;

    public override IEnumerable<SyntaxNode> Children { get; } = [tokenUnreachable, tokenSemiColon];
}

public sealed class SyntaxStmtAssert(SyntaxToken? tokenStatic, SyntaxToken tokenAssert, SyntaxNode condition, SyntaxToken? tokenComma, SyntaxToken? tokenMessage, SyntaxToken tokenSemiColon)
    : SyntaxNode(tokenAssert.Location)
{
    public SyntaxToken? TokenStatic { get; } = tokenStatic;
    public SyntaxToken TokenAssert { get; } = tokenAssert;
    public SyntaxNode Condition { get; } = condition;
    public SyntaxToken? TokenComma { get; } = tokenComma;
    public SyntaxToken? TokenMessage { get; } = tokenMessage;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;

    public bool IsStaticAssert { get; } = tokenStatic is not null;
    public bool HasMessage => TokenMessage is not null;
    public string? MessageText => TokenMessage?.TextValue;

    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            if (TokenStatic is not null)
                yield return TokenStatic;
            yield return TokenAssert;
            yield return Condition;
            if (TokenComma is not null)
                yield return TokenComma;
            if (TokenMessage is not null)
                yield return TokenMessage;
            yield return TokenSemiColon;
        }
    }
}

public sealed class SyntaxStmtDefer(SyntaxToken tokenDefer, SyntaxNode stmt)
    : SyntaxNode(tokenDefer.Location)
{
    public SyntaxToken TokenDefer { get; } = tokenDefer;
    public SyntaxNode Stmt { get; } = stmt;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenDefer, stmt];
}

public sealed class SyntaxStmtDelete(SyntaxToken tokenDelete, SyntaxNode expr)
    : SyntaxNode(tokenDelete.Location)
{
    public SyntaxToken TokenDelete { get; } = tokenDelete;
    public SyntaxNode Expr { get; } = expr;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenDelete, expr];
}

public sealed class SyntaxStmtDiscard(SyntaxToken tokenDiscard, SyntaxNode expr)
    : SyntaxNode(tokenDiscard.Location)
{
    public SyntaxToken TokenDiscard { get; } = tokenDiscard;
    public SyntaxNode Expr { get; } = expr;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenDiscard, expr];
}

public sealed class SyntaxStmtDoLoop(SyntaxToken tokenDo, SyntaxNode body, SyntaxToken tokenWhile, SyntaxNode condition)
    : SyntaxNode(tokenDo.Location)
{
    public SyntaxToken TokenDo { get; } = tokenDo;
    public SyntaxNode Body { get; } = body;
    public SyntaxToken TokenWhile { get; } = tokenWhile;
    public SyntaxNode Condition { get; } = condition;

    public override IEnumerable<SyntaxNode> Children { get; } = [tokenDo, body, tokenWhile, condition];
}

public sealed class SyntaxStmtWhileLoop(SyntaxToken tokenWhile, SyntaxNode condition, SyntaxNode body, SyntaxNode? elseBody)
    : SyntaxNode(tokenWhile.Location)
{
    public SyntaxToken TokenWhile { get; } = tokenWhile;
    public SyntaxNode Condition { get; } = condition;
    public SyntaxNode Body { get; } = body;
    public SyntaxNode? ElseBody { get; } = elseBody;

    public override IEnumerable<SyntaxNode> Children { get; } = elseBody is not null ? [tokenWhile, condition, body, elseBody] : [tokenWhile, condition, body];
}

public sealed class SyntaxStmtForLoop(SyntaxToken tokenFor, SyntaxNode? initializer, SyntaxNode? condition, SyntaxNode? increment, SyntaxNode body)
    : SyntaxNode(tokenFor.Location)
{
    public SyntaxToken TokenFor { get; } = tokenFor;
    public SyntaxNode? Initializer { get; } = initializer;
    public SyntaxNode? Condition { get; } = condition;
    public SyntaxNode? Increment { get; } = increment;
    public SyntaxNode Body { get; } = body;

    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return TokenFor;
            if (Initializer is not null)
                yield return Initializer;
            if (Condition is not null)
                yield return Condition;
            if (Increment is not null)
                yield return Increment;
            yield return Body;
        }
    }
}
