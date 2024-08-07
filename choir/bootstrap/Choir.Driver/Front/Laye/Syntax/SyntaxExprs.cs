namespace Choir.Front.Laye.Syntax;

public enum NamerefKind
{
    Default,
    Global,
    Implicit,
}

public sealed class SyntaxTemplateArguments(IReadOnlyList<SyntaxNode> templateArguments)
    : SyntaxNode(templateArguments.Count == 0 ? Location.Nowhere : templateArguments[templateArguments.Count - 1].Location)
{
    public IReadOnlyList<SyntaxNode> TemplateArguments { get; init; } = templateArguments;

    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            foreach (var arg in TemplateArguments)
                yield return arg;
        }
    }
}

// foo
// foo::bar
// ::foo
// global::foo
public sealed class SyntaxNameref : SyntaxNode
{
    public static SyntaxNameref Create(SyntaxToken name) =>
        new(name.Location, NamerefKind.Default, [name], null);
        
    public static SyntaxNameref Create(SyntaxToken name, SyntaxTemplateArguments? templateArguments) =>
        new(name.Location, NamerefKind.Default, [name], templateArguments);
        
    public static SyntaxNameref Create(Location location, NamerefKind kind, IReadOnlyList<SyntaxToken> names) =>
        new(location, kind, names, null);
        
    public static SyntaxNameref Create(Location location, NamerefKind kind, IReadOnlyList<SyntaxToken> names, SyntaxTemplateArguments? templateArguments) =>
        new(location, kind, names, templateArguments);

    public NamerefKind NamerefKind { get; }
    public IReadOnlyList<SyntaxToken> Names { get; }
    public SyntaxTemplateArguments? TemplateArguments { get; }

    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            foreach (var name in Names)
                yield return name;
            if (TemplateArguments is not null)
                yield return TemplateArguments;
        }
    }

    private SyntaxNameref(Location location, NamerefKind kind, IReadOnlyList<SyntaxToken> names, SyntaxTemplateArguments? templateArguments)
        : base(location)
    {
        NamerefKind = kind;
        Names = names;
        TemplateArguments = templateArguments;
    }
}
