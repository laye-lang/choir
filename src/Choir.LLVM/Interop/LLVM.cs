using System.Runtime.InteropServices;

namespace Choir.LLVM.Interop;

public static class LLVM
{
    private const string LLVMCore = "LLVMCore";

    #region Contexts

    [DllImport(LLVMCore, EntryPoint = "LLVMContextCreate", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ContextCreate();

    [DllImport(LLVMCore, EntryPoint = "LLVMContextDispose", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ContextDispose(IntPtr C);

    #endregion

    #region Modules

    [DllImport(LLVMCore, EntryPoint = "LLVMModuleCreateWithName", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ModuleCreateWithName([MarshalAs(UnmanagedType.LPStr)] string ModuleID);

    [DllImport(LLVMCore, EntryPoint = "LLVMModuleCreateWithName", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ModuleCreateWithNameInContext([MarshalAs(UnmanagedType.LPStr)] string ModuleID, IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMCloneModule", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CloneModule(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMDisposeModule", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisposeModule(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetModuleIdentifier", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern unsafe string GetModuleIdentifier(IntPtr M, IntPtr Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetModuleIdentifier", CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void SetModuleIdentifier(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Ident, ulong Len);

    #endregion
}
