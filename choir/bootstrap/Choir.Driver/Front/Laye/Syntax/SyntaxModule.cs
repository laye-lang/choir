namespace Choir.Front.Laye.Syntax;

public sealed class SyntaxModule(SourceFile sourceFile)
{
    private readonly List<SyntaxNode> _topLevelNodes = [];

    public SourceFile SourceFile { get; } = sourceFile;
    public ChoirContext Context { get; } = sourceFile.Context;

    public void AddTopLevelNode(SyntaxNode node) => _topLevelNodes.Add(node);
}
