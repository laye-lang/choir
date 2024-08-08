namespace Choir.Front.Laye.Syntax;

public sealed class SyntaxStmtExpr(SyntaxNode expr, SyntaxToken tokenSemiColon)
    : SyntaxNode(expr.Location)
{
    public SyntaxNode Expr { get; } = expr;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;
    public override IEnumerable<SyntaxNode> Children { get; } = [expr, tokenSemiColon];
}

public sealed class SyntaxStmtCompound(Location location, IReadOnlyList<SyntaxNode> body)
    : SyntaxNode(location)
{
    public IReadOnlyList<SyntaxNode> Body { get; } = body;
    public override IEnumerable<SyntaxNode> Children { get; } = body;
}
