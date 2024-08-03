namespace Choir.Front.Laye.Syntax;

public sealed class SyntaxTranslationUnit(ChoirContext context)
{
    private readonly List<SyntaxModule> _modules = [];

    public ChoirContext Context { get; } = context;
    public IEnumerable<SyntaxModule> Modules => _modules;
}
