using System.Runtime.InteropServices;

namespace Choir.Driver;

public sealed class ChoirDriver : ICompilerDriver
{
    [DllImport("LLVMCore", EntryPoint = "LLVMModuleCreateWithName", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ModuleCreateWithName([MarshalAs(UnmanagedType.LPStr)] string ModuleID);

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
        IntPtr module = ModuleCreateWithName("ChoirModule");
        Console.WriteLine(module);
        return 0;
    }
}
