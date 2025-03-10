namespace Choir.LLVM.Interop;

public readonly struct LLVMContextRef(IntPtr handle)
    : IDisposable
{
    public static explicit operator LLVMContextRef(IntPtr handle) => new(handle);
    public static implicit operator IntPtr(LLVMContextRef module) => module.Handle;

    public static LLVMContextRef Create()
    {
        IntPtr handle = LLVM.ContextCreate();
        return new(handle);
    }

    public readonly IntPtr Handle = handle;

    public void Dispose()
    {
        LLVM.ContextDispose(Handle);
    }
}
