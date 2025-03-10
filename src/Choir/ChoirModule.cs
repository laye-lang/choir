namespace Choir;

public sealed class ChoirModule
{
    public static ChoirModule Create(ChoirContext context, string moduleName)
    {
        return new ChoirModule(context, moduleName);
    }

    public ChoirContext Context { get; }
    public string Name { get; }

    private ChoirModule(ChoirContext context, string name)
    {
        Context = context;
        Name = name;
    }
}
