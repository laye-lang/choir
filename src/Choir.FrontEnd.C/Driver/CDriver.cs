using Choir.Driver;

namespace Choir.FrontEnd.C.Driver;

public sealed class CDriver
    : ICompilerDriver
{
    public static CDriver Create()
    {
        return new CDriver();
    }

    private CDriver()
    {
    }

    public int Execute()
    {
        return 0;
    }
}
