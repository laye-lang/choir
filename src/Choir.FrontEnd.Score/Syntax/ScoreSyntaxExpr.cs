using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreSyntaxExpr(SourceRange range)
    : ScoreSyntaxNode(range)
{
}

/// <summary>
/// For expressions which don't come with a semicolon in a statement context require one.
/// </summary>
public sealed class ScoreSyntaxExprStmt(ScoreSyntaxExpr expr, ScoreToken semiColonToken)
    : ScoreSyntaxExpr(expr.Range)
{
    public ScoreSyntaxExpr Expr { get; } = expr;
    public ScoreToken SemiColonToken { get; } = semiColonToken;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [expr, semiColonToken];
}

#region Statements

public sealed class ScoreSyntaxExprReturn(ScoreToken returnKeywordToken, ScoreSyntaxExpr? returnValue, ScoreToken semiColonToken)
    : ScoreSyntaxExpr(returnKeywordToken.Range)
{
    public ScoreToken ReturnKeywordToken { get; } = returnKeywordToken;
    public ScoreSyntaxExpr? ReturnValue { get; } = returnValue;
    public ScoreToken SemiColonToken { get; } = semiColonToken;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = returnValue is null ? [returnKeywordToken, semiColonToken] : [returnKeywordToken, returnValue, semiColonToken];
}

#endregion

#region Expressions

public sealed class ScoreSyntaxExprLiteral(ScoreToken literalToken)
    : ScoreSyntaxExpr(literalToken.Range)
{
    public ScoreToken LiteralToken { get; } = literalToken;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [literalToken];
}

public sealed class ScoreSyntaxExprCompound(ScoreToken openCurlyToken, IReadOnlyList<ScoreSyntaxNode> childNodes, ScoreToken closeCurlyToken)
    : ScoreSyntaxExpr(openCurlyToken.Range)
{
    public ScoreToken OpenCurlyToken { get; } = openCurlyToken;
    public IReadOnlyList<ScoreSyntaxNode> ChildNodes { get; } = childNodes;
    public ScoreToken CloseCurlyToken { get; } = closeCurlyToken;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [openCurlyToken, .. childNodes, closeCurlyToken];
}

#endregion
