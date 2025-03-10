using Choir.LLVM.Interop;

namespace Choir.LLVM;

/// <summary>
/// Contexts are execution states for the core LLVM IR system.
/// Most types are tied to a context instance.Multiple contexts can exist simultaneously.
/// A single context is not thread safe.
/// However, different contexts can execute on different threads simultaneously.
/// </summary>
public sealed class LLVMContext
    : Disposable
{
    public static LLVMContext Create()
    {
        var handle = LLVMContextRef.Create();
        return new(handle);
    }

    public LLVMContextRef Handle { get; private set; }

    private LLVMContext(LLVMContextRef handle)
    {
        Handle = handle;
    }

    public LLVMModule CreateModule(string name)
    {
        return new(LLVMModuleRef.CreateWithNameInContext(name, Handle));
    }

    protected override void DisposeUnmanaged()
    {
        Handle.Dispose();
        Handle = (LLVMContextRef)IntPtr.Zero;
    }
}
