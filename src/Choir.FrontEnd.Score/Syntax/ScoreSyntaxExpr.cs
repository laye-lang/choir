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
}

#region Statements

public sealed class ScoreSyntaxExprReturn(ScoreToken returnKeywordToken, ScoreSyntaxExpr? returnValue, ScoreToken semiColonToken)
    : ScoreSyntaxExpr(returnKeywordToken.Range)
{
    public ScoreToken ReturnKeywordToken { get; set; } = returnKeywordToken;
    public ScoreSyntaxExpr? ReturnValue { get; set; } = returnValue;
    public ScoreToken SemiColonToken { get; set; } = semiColonToken;
}

#endregion

#region Expressions

public sealed class ScoreSyntaxExprLiteral(ScoreToken literalToken)
    : ScoreSyntaxExpr(literalToken.Range)
{
    public ScoreToken LiteralToken { get; set; } = literalToken;
}

public sealed class ScoreSyntaxExprGrouped(ScoreToken openCurlyToken, List<ScoreSyntaxExpr> exprs, ScoreToken closeCurlyToken)
    : ScoreSyntaxExpr(openCurlyToken.Range)
{
    public ScoreToken OpenCurlyToken { get; set; } = openCurlyToken;
    public List<ScoreSyntaxExpr> Exprs { get; set; } = exprs;
    public ScoreToken CloseCurlyToken { get; set; } = closeCurlyToken;
    public override IEnumerable<ScoreSyntaxNode> Children
    {
        get
        {
            yield return OpenCurlyToken;
            foreach (var expr in Exprs)
                yield return expr;
            yield return CloseCurlyToken;
        }
    }
}

#endregion
