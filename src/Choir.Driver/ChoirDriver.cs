using Choir.LLVM;

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
        using var llvmContext = LLVMContext.Create();
        using var llvmModule = llvmContext.CreateModule("choir");
        Console.WriteLine(llvmContext.Handle.Handle);
        Console.WriteLine(llvmModule.Handle.Handle);
        return 0;
    }
}
