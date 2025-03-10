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
        IntPtr module = LLVM.Interop.LLVM.ModuleCreateWithName("ChoirModule");
        Console.WriteLine(module);
        return 0;
    }
}
