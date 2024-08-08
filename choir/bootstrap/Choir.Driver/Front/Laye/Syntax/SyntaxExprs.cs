using Choir.Front.Laye.Sema;

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

    public override bool CanBeType { get; } = true;
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

public sealed class SyntaxGrouped(SyntaxNode inner)
    : SyntaxNode(inner.Location)
{
    public SyntaxNode Inner { get; } = inner;
    public override bool CanBeType => Inner.CanBeType;
    public override IEnumerable<SyntaxNode> Children { get; } = [inner];
}

public sealed class SyntaxExprCall(SyntaxNode callee, IReadOnlyList<SyntaxNode> args)
    : SyntaxNode(callee.Location)
{
    public SyntaxNode Callee { get; } = callee;
    public IReadOnlyList<SyntaxNode> Args { get; } = args;
    public override IEnumerable<SyntaxNode> Children { get; } = [callee, ..args];
}

public sealed class SyntaxExprBinary(SyntaxNode lhs, SyntaxNode rhs, SyntaxToken tokenOperator)
    : SyntaxNode(tokenOperator.Location)
{
    public SyntaxNode Left { get; } = lhs;
    public SyntaxNode Right { get; } = rhs;
    public SyntaxToken TokenOperator { get; } = tokenOperator;

    public override IEnumerable<SyntaxNode> Children { get; } = [lhs, tokenOperator, rhs];
}

public sealed class SyntaxQualMut(SyntaxNode inner, SyntaxToken tokenMut)
    : SyntaxNode(inner.Location)
{
    public SyntaxNode Inner { get; } = inner;
    public SyntaxToken TokenMut { get; } = tokenMut;

    public override bool CanBeType { get; } = true;
    public override IEnumerable<SyntaxNode> Children { get; } = [inner, tokenMut];
}

public sealed class SyntaxTypeBuiltIn(Location location, SemaTypeBuiltIn type)
    : SyntaxNode(location)
{
    public SemaTypeBuiltIn Type { get; } = type;

    public override bool CanBeType { get; } = true;
}

public sealed class SyntaxTypePointer(SyntaxNode inner)
    : SyntaxNode(inner.Location)
{
    public SyntaxNode Inner { get; } = inner;
    public override IEnumerable<SyntaxNode> Children { get; } = [inner];

    public override bool CanBeType { get; } = true;
}

public sealed class SyntaxTypeReference(SyntaxNode inner)
    : SyntaxNode(inner.Location)
{
    public SyntaxNode Inner { get; } = inner;
    public override IEnumerable<SyntaxNode> Children { get; } = [inner];

    public override bool CanBeType { get; } = true;
}

public sealed class SyntaxTypeBuffer(SyntaxNode inner)
    : SyntaxNode(inner.Location)
{
    public SyntaxNode Inner { get; } = inner;
    public override IEnumerable<SyntaxNode> Children { get; } = [inner];

    public override bool CanBeType { get; } = true;
}
