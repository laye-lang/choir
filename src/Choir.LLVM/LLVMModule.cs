using Choir.LLVM.Interop;

namespace Choir.LLVM;

/// <summary>
/// Modules represent the top-level structure in an LLVM program.
/// An LLVM module is effectively a translation unit or a collection of translation units merged together.
/// </summary>
public sealed class LLVMModule
    : Disposable, ICloneable
{
    /// <summary>
    /// Creates a module with the given name/identifier in the global context.
    /// </summary>
    public static LLVMModule Create(string name)
    {
        var handle = LLVMModuleRef.CreateWithName(name);
        return new(handle);
    }

    /// <summary>
    /// The underlying interop handle to this module.
    /// </summary>
    public LLVMModuleRef Handle { get; private set; }

    /// <summary>
    /// The name/identifier of this module.
    /// </summary>
    public string Name
    {
        get => Handle.GetModuleIdentifier();
        set => Handle.SetModuleIdentifier(value);
    }

    internal LLVMModule(LLVMModuleRef handle)
    {
        Handle = handle;
    }

    protected override void DisposeUnmanaged()
    {
        Handle.Dispose();
        Handle = (LLVMModuleRef)IntPtr.Zero;
    }

    /// <summary>
    /// Returns an exact copy of this module.
    /// </summary>
    public LLVMModule Clone() => new(Handle.Clone());
    object ICloneable.Clone() => Clone();
}
