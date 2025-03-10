using System.Runtime.InteropServices;

namespace Choir.LLVM.Interop;

public static partial class LLVM
{
    private const string LLVMCore = "LLVMCore";

    [DllImport(LLVMCore, EntryPoint = "LLVMModuleCreateWithName", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ModuleCreateWithName([MarshalAs(UnmanagedType.LPStr)] string ModuleID);
}
