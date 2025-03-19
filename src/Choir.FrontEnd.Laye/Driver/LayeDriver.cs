using Choir.Driver;

namespace Choir.FrontEnd.Laye.Driver;

public sealed class LayeDriver
    : ICompilerDriver
{
    public static LayeDriver Create()
    {
        return new LayeDriver();
    }

    private LayeDriver()
    {
    }

    public int Execute()
    {
        return 0;
    }
}
