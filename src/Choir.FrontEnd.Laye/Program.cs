using Choir.FrontEnd.Laye.Driver;

namespace Choir.FrontEnd.Laye;

public static class Program
{
    public static int Main(string[] args)
    {
        var driver = LayeDriver.Create();
        return driver.Execute();
    }
}
