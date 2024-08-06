namespace Choir.Front.Laye.Syntax;

public abstract class SyntaxNode(Location location)
{
    public Location Location { get; } = location;
    public virtual IEnumerable<SyntaxNode> Children { get; } = [];
}
