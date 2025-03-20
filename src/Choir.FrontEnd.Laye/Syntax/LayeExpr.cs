using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class LayeExpr
    : LayeSyntaxNode
{
    public SourceRange Range { get; set; }
    public SourceLocation Location => Range.Begin;

    public LayeTypeQual Type { get; set; }

    protected LayeExpr(SourceRange range, LayeTypeQual type)
    {
        Range = range;
        Type = type;
    }
}
