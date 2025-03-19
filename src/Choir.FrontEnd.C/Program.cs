using Choir.FrontEnd.C.Driver;

namespace Choir.FrontEnd.C;

public static class Program
{
    public static int Main(string[] args)
    {
        var driver = CDriver.Create();
        return driver.Execute();
    }
}
