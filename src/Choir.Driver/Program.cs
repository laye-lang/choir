namespace Choir.Driver;

public static class Program
{
    public static int Main(string[] args)
    {
        var driver = ChoirDriver.Create();
        return driver.Execute();
    }
}
