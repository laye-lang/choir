namespace Choir.Front.Laye.Syntax;

public sealed class SyntaxStmtExpr(SyntaxNode expr, SyntaxToken tokenSemiColon)
    : SyntaxNode(expr.Location)
{
    public SyntaxNode Expr { get; } = expr;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;
    public override IEnumerable<SyntaxNode> Children { get; } = [expr, tokenSemiColon];
}

public sealed class SyntaxStmtAssign(SyntaxNode lhs, SyntaxToken tokenAssignOp, SyntaxNode rhs, SyntaxToken tokenSemiColon)
    : SyntaxNode(tokenAssignOp.Location)
{
    public SyntaxNode Left { get; } = lhs;
    public SyntaxToken TokenAssignOp { get; } = tokenAssignOp;
    public SyntaxNode Right { get; } = rhs;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;

    public override IEnumerable<SyntaxNode> Children { get; } = [lhs, tokenAssignOp, rhs, tokenSemiColon];
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
    public SyntaxNode? Condition { get; } = condition;
    public SyntaxNode? Body { get; } = body;

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

public sealed class SyntaxStmtAssert(SyntaxToken tokenAssert, SyntaxNode condition, SyntaxToken? tokenComma, SyntaxToken? tokenMessage, SyntaxToken tokenSemiColon)
    : SyntaxNode(tokenAssert.Location)
{
    public SyntaxToken TokenAssert { get; } = tokenAssert;
    public SyntaxNode Condition { get; } = condition;
    public SyntaxToken? TokenComma { get; } = tokenComma;
    public SyntaxToken? TokenMessage { get; } = tokenMessage;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;

    public bool HasMessage => TokenMessage is not null;
    public string? MessageText => TokenMessage?.TextValue;

    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
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

public sealed class SyntaxStmtDoLoop(SyntaxToken tokenDo, SyntaxNode body, SyntaxToken tokenWhile, SyntaxNode condition)
    : SyntaxNode(tokenDo.Location)
{
    public SyntaxToken TokenDo { get; } = tokenDo;
    public SyntaxNode Body { get; } = body;
    public SyntaxToken TokenWhile { get; } = tokenWhile;
    public SyntaxNode Condition { get; } = condition;

    public override IEnumerable<SyntaxNode> Children { get; } = [tokenDo, body, tokenWhile, condition];
}
