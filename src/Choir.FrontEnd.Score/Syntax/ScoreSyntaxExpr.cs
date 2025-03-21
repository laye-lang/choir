using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreSyntaxExpr
    : ScoreSyntaxNode
{
    public SourceRange Range { get; set; }
    public SourceLocation Location => Range.Begin;

    public ScoreTypeQual Type { get; set; }

    protected ScoreSyntaxExpr(SourceRange range, ScoreTypeQual type)
    {
        Range = range;
        Type = type;
    }
}
