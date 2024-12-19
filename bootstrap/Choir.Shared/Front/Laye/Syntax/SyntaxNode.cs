namespace Choir.Front.Laye.Syntax;

public abstract class SyntaxNode(Location location)
{
    public Location Location { get; } = location;
    public virtual IEnumerable<SyntaxNode> Children { get; } = [];

    public bool IsCompilerGenerated { get; init; } = false;
    public virtual bool IsDecl { get; } = false;
    public virtual bool CanBeType { get; } = false;
}
