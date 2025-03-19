using Choir.Driver;

namespace Choir;

public static class Program
{
    public static int Main(string[] args)
    {
        var driver = ChoirDriver.Create();
        return driver.Execute();
    }
}
