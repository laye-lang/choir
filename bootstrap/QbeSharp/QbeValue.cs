namespace QbeSharp;

public abstract class QbeValue
{
    private static long _counter = 0;

    public long Id { get; }
    public QbeContext Context { get; }

    internal QbeValue(QbeContext context)
    {
        Id = Interlocked.Increment(ref _counter);
        Context = context;
    }
}

public sealed class QbeFunction : QbeValue
{
    public QbeModule Module { get; }
    public string Name { get; }
    public QbeType ReturnType { get; }
    public IReadOnlyList<QbeType> ParameterTypes { get; }

    internal QbeFunction(QbeContext context, QbeModule module, string name, QbeType returnType, IReadOnlyList<QbeType> parameterTypes)
        : base(context)
    {
        Module = module;
        Name = name;
        ReturnType = returnType;
        ParameterTypes = parameterTypes;
    }
}

public sealed class QbeGlobal : QbeValue
{
    public QbeModule Module { get; }
    public string Name { get; }
    public QbeType Type { get; }

    internal QbeGlobal(QbeContext context, QbeModule module, string name, QbeType type)
        : base(context)
    {
        Module = module;
        Name = name;
        Type = type;
    }
}
