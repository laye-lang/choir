namespace QbeSharp;

public sealed class QbeContext
{
    public QbeContext()
    {
    }

    public QbeModule CreateModule(string name)
    {
        var module = new QbeModule(this, name);
        return module;
    }

    public QbeBuilder CreateBuilder()
    {
        var builder = new QbeBuilder(this);
        return builder;
    }
}
