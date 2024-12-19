namespace QbeSharp;

public abstract class QbeType
{
    public QbeContext Context { get; }

    internal QbeType(QbeContext context)
    {
        Context = context;
    }
}
