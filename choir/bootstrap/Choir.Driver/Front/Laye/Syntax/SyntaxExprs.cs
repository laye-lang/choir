namespace Choir.Front.Laye.Syntax;

public enum NamerefKind
{
    Default,
    Global,
    Implicit,
}

// foo
// foo::bar
// ::foo
// global::foo
public sealed class SyntaxNameref : SyntaxNode
{
    public static SyntaxNameref Create(SyntaxToken name) =>
        new(name.Location, NamerefKind.Default, [name]);
        
    public static SyntaxNameref Create(Location location, NamerefKind kind, IReadOnlyList<SyntaxToken> names) =>
        new(location, kind, names);

    public NamerefKind NamerefKind { get; }
    public IReadOnlyList<SyntaxToken> Names { get; }
    
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            foreach (var name in Names)
                yield return name;
        }
    }

    private SyntaxNameref(Location location, NamerefKind kind, IReadOnlyList<SyntaxToken> names)
        : base(location)
    {
        NamerefKind = kind;
        Names = names;
    }
}
