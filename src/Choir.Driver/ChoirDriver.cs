namespace Choir.Driver;

public sealed class ChoirDriver : ICompilerDriver
{
    public static ChoirDriver Create()
    {
        return new ChoirDriver();
    }

    private ChoirDriver()
    {
    }

    public int Execute()
    {
        Console.WriteLine("Hello, Choir!");
        return 0;
    }
}
