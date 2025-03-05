using Choir.Source;

namespace Choir.FrontEnd.Laye.Semantics;

public abstract class LayeExpr
    : LayeSemaNode
{
    public SourceRange Range { get; }
    public SourceLocation Location => Range.Begin;

    public LayeTypeQual Type { get; }

    protected LayeExpr(SourceRange range, LayeTypeQual type)
    {
        Range = range;
        Type = type;
    }
}
