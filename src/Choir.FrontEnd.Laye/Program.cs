using Choir.FrontEnd.Score.Driver;

namespace Choir.FrontEnd.Score;

public static class Program
{
    public static int Main(string[] args)
    {
        var driver = LayeDriver.Create();
        return driver.Execute();
    }
}
