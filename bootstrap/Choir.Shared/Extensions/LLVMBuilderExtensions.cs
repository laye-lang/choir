using Choir;

namespace LLVMSharp.Interop;

public static class LLVMBuilderExtensions
{
    public static LLVMValueRef BuildPtrAdd(this LLVMBuilderRef builder, LLVMValueRef pointer, Size offset, string name = "")
    {
        return builder.BuildPtrAdd(pointer, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)offset.Bytes, false), name);
    }

    public static LLVMValueRef BuildPtrAdd(this LLVMBuilderRef builder, LLVMValueRef pointer, LLVMValueRef offset, string name = "")
    {
        return builder.BuildGEP2(LLVMTypeRef.Int8, pointer, new ReadOnlySpan<LLVMValueRef>(ref offset), name);
    }

    public static LLVMValueRef BuildPtrAdd(this LLVMBuilderRef builder, LLVMValueRef pointer, LLVMValueRef offset, Size stride, string name = "")
    {
        var offsetMul = builder.BuildMul(offset, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)stride.Bytes, false), $"{name}.mulstride");
        return builder.BuildPtrAdd(pointer, offsetMul, name);
    }

    public static LLVMValueRef BuildMemSet(this LLVMBuilderRef builder, LLVMValueRef storage, LLVMValueRef value, Size size, Align align)
    {
        unsafe
        {
            return LLVM.BuildMemSet(builder, storage,
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, 0),
                LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)size.Bytes, false),
                (uint)align.Bytes
            );
        }
    }
}
