namespace Choir.LLVM.Interop;

public readonly struct LLVMModuleRef(IntPtr handle)
    : IDisposable
{
    public static explicit operator LLVMModuleRef(IntPtr handle) => new(handle);
    public static implicit operator IntPtr(LLVMModuleRef module) => module.Handle;

    public static LLVMModuleRef CreateWithName(string moduleName)
    {
        IntPtr handle = LLVM.ModuleCreateWithName(moduleName);
        return new(handle);
    }

    public static LLVMModuleRef CreateWithNameInContext(string moduleName, LLVMContextRef context)
    {
        IntPtr handle = LLVM.ModuleCreateWithNameInContext(moduleName, context);
        return new(handle);
    }

    public readonly IntPtr Handle = handle;

    public void Dispose()
    {
        LLVM.DisposeModule(Handle);
    }

    public LLVMModuleRef Clone()
    {
        return new(LLVM.CloneModule(Handle));
    }

    public string GetModuleIdentifier()
    {
        return LLVM.GetModuleIdentifier(Handle, IntPtr.Zero);
    }

    public void SetModuleIdentifier(string identifier)
    {
        LLVM.SetModuleIdentifier(Handle, identifier, (ulong)identifier.Length);
    }
}
