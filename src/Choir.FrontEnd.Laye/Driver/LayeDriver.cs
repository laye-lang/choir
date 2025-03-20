using Choir.Driver;

namespace Choir.FrontEnd.Score.Driver;

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
