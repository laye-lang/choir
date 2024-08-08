namespace Choir.Front.Laye.Syntax;

public sealed class SyntaxStmtCompound(Location location, IReadOnlyList<SyntaxNode> body)
    : SyntaxNode(location)
{
    public IReadOnlyList<SyntaxNode> Body { get; } = body;
    public override IEnumerable<SyntaxNode> Children { get; } = body;
}
