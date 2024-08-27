using Choir.Front.Laye.Sema;
using Choir.Front.Laye.Syntax;

namespace Choir.Front.Laye;

public sealed record class ModuleImport(SyntaxDeclImport ImportDecl, Module ReferencedModule);

public sealed class Module(SourceFile sourceFile)
{
    private readonly List<SyntaxToken> _tokens = [];
    private readonly List<SyntaxNode> _topLevelSyntax = [];
    private readonly List<ModuleImport> _imports = [];
    private readonly List<SemaDecl> _decls = [];

    public SourceFile SourceFile { get; } = sourceFile;
    public ChoirContext Context { get; } = sourceFile.Context;
    public TranslationUnit? TranslationUnit { get; internal set; }

    public Scope FileScope { get; } = new();
    public Scope ExportScope { get; } = new();

    public bool HasTokens => _tokens.Count != 0;
    public IEnumerable<SyntaxToken> Tokens => _tokens;

    public bool HasSyntax => _topLevelSyntax.Count != 0;
    public IEnumerable<SyntaxNode> TopLevelSyntax => _topLevelSyntax;

    public bool HasSemaDecls => _decls.Count != 0;
    public IEnumerable<SemaDecl> SemaDecls => _decls;

    public void AddToken(SyntaxToken token) => _tokens.Add(token);
    public void AddTopLevelSyntax(SyntaxNode node) => _topLevelSyntax.Add(node);
    public void AddImportReference(SyntaxDeclImport importDecl, Module referencedModule) => _imports.Add(new(importDecl, referencedModule));
    public void AddDecl(SemaDecl decl) => _decls.Add(decl);
}
