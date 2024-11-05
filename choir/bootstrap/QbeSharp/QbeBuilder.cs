namespace QbeSharp;

public sealed class QbeBuilder
{
    public QbeContext Context { get; }

    internal QbeBuilder(QbeContext context)
    {
        Context = context;
    }
}
