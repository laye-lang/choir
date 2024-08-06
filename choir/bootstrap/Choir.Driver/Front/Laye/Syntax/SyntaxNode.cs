namespace Choir.Front.Laye.Syntax;

public enum ImportKind
{
    Invalid,
    FilePath,
    Library,
}

public enum NamerefKind
{
    Default,
    Global,
    Implicit,
}

public abstract class SyntaxNode(Location location)
{
    public Location Location { get; } = location;
    public virtual IEnumerable<SyntaxNode> Children { get; } = [];

    public abstract class ImportQuery(Location location) : SyntaxNode(location);

    public sealed class ImportQueryWildcard(SyntaxToken token) : ImportQuery(token.Location)
    {
        public SyntaxToken Token { get; } = token;
        public override IEnumerable<SyntaxNode> Children { get { yield return Token; } }
    }

    public sealed class ImportQueryNamed(Nameref query) : ImportQuery(query.Location)
    {
        public Nameref Query { get; } = query;
        public override IEnumerable<SyntaxNode> Children { get { yield return Query; } }
    }

    public sealed class Import(SyntaxToken tokenImport) : SyntaxNode(tokenImport.Location)
    {
        public required ImportKind ImportKind { get; init; }
        public SyntaxToken TokenImport { get; } = tokenImport;
        public required IReadOnlyList<ImportQuery> Queries { get; init; }
        public SyntaxToken? TokenFrom { get; init; }
        public required SyntaxToken TokenModuleName { get; init; }
        public required SyntaxToken TokenSemiColon { get; init; }

        public string ModuleNameText => TokenModuleName.TextValue;

        public override IEnumerable<SyntaxNode> Children
        {
            get
            {
                yield return TokenImport;
                foreach (var query in Queries)
                    yield return query;
                if (TokenFrom is not null)
                    yield return TokenFrom;
                yield return TokenModuleName;
                yield return TokenSemiColon;
            }
        }
    }

    // foo
    // foo::bar
    // ::foo
    // global::foo
    public abstract class Nameref : SyntaxNode
    {
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

        public Nameref(SyntaxToken name)
            : base(name.Location)
        {
            NamerefKind = NamerefKind.Default;
            Names = [name];
        }
    }
}
