namespace LLVMSharp.Interop;

public static class LLVMModuleExtensions
{
    public static void EmbedBuffer(this LLVMModuleRef module, byte[] buffer, string sectionName)
    {
        LLVMValueRef moduleConstant;
        unsafe
        {
            fixed (byte* bufferPtr = buffer)
            {
                moduleConstant = LLVM.ConstStringInContext(module.Context, (sbyte*)bufferPtr, (uint)buffer.Length, 1);
            }

            var gv = module.AddGlobal(moduleConstant.TypeOf, "llvm.embedded.object");
            gv.Section = sectionName;
            gv.Alignment = 1;
            gv.Initializer = moduleConstant;
            gv.Linkage = LLVMLinkage.LLVMLinkerPrivateLinkage;
            LLVM.SetGlobalConstant(gv, 1);
        }
    }
}
