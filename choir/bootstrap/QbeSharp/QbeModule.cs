namespace QbeSharp;

public sealed class QbeModule
{
    public QbeContext Context { get; }
    public string Name { get; }

    private readonly List<QbeFunction> _functions = [];
    private readonly List<QbeGlobal> _globals = [];

    internal QbeModule(QbeContext context, string name)
    {
        Context = context;
        Name = name;
    }

    public QbeValue AddFunction(string name, QbeType returnType, params QbeType[] parameterTypes)
    {
        var function = new QbeFunction(Context, this, name, returnType, parameterTypes);
        // TODO(local): how to error on duplicate name? validation error?
        _functions.Add(function);
        return function;
    }

    public QbeValue AddFunction(string name, QbeType returnType, IEnumerable<QbeType> parameterTypes)
    {
        return AddFunction(name, returnType, [..parameterTypes]);
    }

    public QbeValue AddGlobal(string name, QbeType type)
    {
        var global = new QbeGlobal(Context, this, name, type);
        // TODO(local): how to error on duplicate name? validation error?
        _globals.Add(global);
        return global;
    }
}
