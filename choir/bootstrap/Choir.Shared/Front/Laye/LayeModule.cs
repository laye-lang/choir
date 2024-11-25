using Choir.Front.Laye.Sema;

namespace Choir.Front.Laye;

public sealed class LayeModule(ChoirContext context, IEnumerable<SourceFile> sourceFiles)
{
    private readonly List<SemaDecl> _declarations = [];

    public ChoirContext Context { get; } = context;
    public IReadOnlyList<SourceFile> SourceFiles = [.. sourceFiles];

    public string? ModuleName { get; set; }

    public Scope ModuleScope { get; } = new();
    public Scope ExportScope { get; } = new();

    public IEnumerable<BaseSemaNode> Declarations => _declarations;

    public void AddDecl(SemaDecl decl)
    {
        _declarations.Add(decl);
    }
}
