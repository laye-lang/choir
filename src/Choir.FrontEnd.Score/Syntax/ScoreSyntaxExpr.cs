using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreSyntaxExpr
    : ScoreSyntaxNode
{
    public SourceRange Range { get; set; }
    public SourceLocation Location => Range.Begin;

    public ScoreSyntaxTypeQual Type { get; set; }

    protected ScoreSyntaxExpr(SourceRange range, ScoreSyntaxTypeQual type)
    {
        Range = range;
        Type = type;
    }
}
