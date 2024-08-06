using System.Diagnostics;

namespace Choir.Front.Laye.Sema;

public sealed class TranslationUnit(ChoirContext context)
{
    private readonly List<Module> _modules = [];

    public ChoirContext Context { get; } = context;
    public IEnumerable<Module> Modules => _modules;

    public void AddModule(Module module)
    {
        Debug.Assert(module.TranslationUnit is null);

        module.TranslationUnit = this;
        _modules.Add(module);
    }
}
