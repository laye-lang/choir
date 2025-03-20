using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreExpr
    : ScoreSyntaxNode
{
    public SourceRange Range { get; set; }
    public SourceLocation Location => Range.Begin;

    public ScoreTypeQual Type { get; set; }

    protected ScoreExpr(SourceRange range, ScoreTypeQual type)
    {
        Range = range;
        Type = type;
    }
}
