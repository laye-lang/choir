namespace Choir.Front.Laye.Syntax;

public enum SyntaxImportKind
{
    FilePath,
    Library,
}

public abstract class SyntaxNode(SyntaxKind kind, Location location)
{
    public virtual IEnumerable<SyntaxNode> Children { get { yield break; } }

    public SyntaxKind Kind { get; } = kind;
    public Location Location { get; } = location;

    public sealed class Import(SyntaxKind kind, Location location) : SyntaxNode(kind, location)
    {
        public required SyntaxImportKind ImportKind { get; init; }
        public required string ImportText { get; init; }
        public string? NamespaceAlias { get; set; }
    }
}
