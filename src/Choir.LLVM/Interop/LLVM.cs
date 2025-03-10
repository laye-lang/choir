using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Choir.LLVM.Interop;

public static partial class LLVM
{
    private const string LLVMCore = "LLVMCore";

    //[LibraryImport("LLVMCore", EntryPoint = "LLVMModuleCreateWithName", StringMarshalling = StringMarshalling.Utf8)]
    //[UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    //public static partial IntPtr ModuleCreateWithName(string ModuleID);

}
