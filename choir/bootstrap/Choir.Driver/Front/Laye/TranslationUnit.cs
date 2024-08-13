namespace Choir.Front.Laye;

public sealed class TranslationUnit(ChoirContext context)
{
    private readonly List<Module> _modules = [];

    public ChoirContext Context { get; } = context;
    public IEnumerable<Module> Modules => _modules;

    public void AddModule(Module module)
    {
        Context.Assert(module.TranslationUnit is null, "when adding a module to a translation unit, the module was already a part of another translation unit.");
        module.TranslationUnit = this;
        _modules.Add(module);
    }
}
