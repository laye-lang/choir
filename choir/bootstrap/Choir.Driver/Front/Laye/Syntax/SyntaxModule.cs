namespace Choir.Front.Laye.Syntax;

public sealed class SyntaxModule(SourceFile sourceFile)
{
    private readonly List<SyntaxToken> _tokens = [];
    private readonly List<SyntaxNode> _topLevelNodes = [];

    public SourceFile SourceFile { get; } = sourceFile;
    public ChoirContext Context { get; } = sourceFile.Context;

    public IEnumerable<SyntaxToken> Tokens => _tokens;
    public IEnumerable<SyntaxNode> TopLevelNodes => _topLevelNodes;

    public void AddToken(SyntaxToken token) => _tokens.Add(token);
    public void AddTopLevelNode(SyntaxNode node) => _topLevelNodes.Add(node);
}
