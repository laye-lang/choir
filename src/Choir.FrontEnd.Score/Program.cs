using Choir.FrontEnd.Score.Driver;

namespace Choir.FrontEnd.Score;

public static class Program
{
    public static int Main(string[] args)
    {
        var driver = ScoreDriver.Create();
        return driver.Execute();
    }
}
