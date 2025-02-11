using System.Text;

using Choir;

namespace LLVMSharp.Interop;

public static class LLVMValueExtensions
{
    public static void AddAttributeAtIndex(this LLVMValueRef value, int attribIndex, LLVMAttributeRef attrib)
        => value.AddAttributeAtIndex((LLVMAttributeIndex)attribIndex, attrib);

    public static void AddAttributeAtIndex(this LLVMValueRef value, LLVMAttributeIndex attribIndex, LLVMAttributeRef attrib)
    {
        unsafe
        {
            LLVM.AddAttributeAtIndex(value, attribIndex, attrib);
        }
    }

    public static void AddNamedAttributeAtIndex(this LLVMValueRef value, LLVMContextRef context, string attribName, LLVMAttributeIndex attribIndex)
    {
        unsafe
        {
            byte[] bytes = Encoding.UTF8.GetBytes(attribName);
            fixed (byte* stringData = bytes)
            {
                uint kindId = LLVM.GetEnumAttributeKindForName((sbyte*)stringData, (uint)bytes.Length);
                var attrib = LLVM.CreateEnumAttribute(context, kindId, 0);
                value.AddAttributeAtIndex(attribIndex, attrib);
            }
        }
    }

    public static void AddNamedAttributeAtIndex(this LLVMValueRef value, LLVMContextRef context, string attribName, LLVMAttributeIndex attribIndex, LLVMTypeRef attribType)
    {
        unsafe
        {
            byte[] bytes = Encoding.UTF8.GetBytes(attribName);
            fixed (byte* stringData = bytes)
            {
                uint kindId = LLVM.GetEnumAttributeKindForName((sbyte*)stringData, (uint)bytes.Length);
                var attrib = LLVM.CreateTypeAttribute(context, kindId, attribType);
                value.AddAttributeAtIndex(attribIndex, attrib);
            }
        }
    }

    public static void AddSRetAttributeAtIndex(this LLVMValueRef value, LLVMContextRef context, LLVMAttributeIndex attribIndex, LLVMTypeRef returnType) =>
        value.AddNamedAttributeAtIndex(context, "sret", attribIndex, returnType);

    public static void AddNoAliasAttributeAtIndex(this LLVMValueRef value, LLVMContextRef context, LLVMAttributeIndex attribIndex) =>
        value.AddNamedAttributeAtIndex(context, "noalias", attribIndex);

    public static void AddWritableAttributeAtIndex(this LLVMValueRef value, LLVMContextRef context, LLVMAttributeIndex attribIndex) =>
        value.AddNamedAttributeAtIndex(context, "writable", attribIndex);

    public static void AddDeadOnUnwindAttributeAtIndex(this LLVMValueRef value, LLVMContextRef context, LLVMAttributeIndex attribIndex) =>
        value.AddNamedAttributeAtIndex(context, "dead_on_unwind", attribIndex);

    public static void AddNoUnwindAttributeAtIndex(this LLVMValueRef value, LLVMContextRef context, LLVMAttributeIndex attribIndex) =>
        value.AddNamedAttributeAtIndex(context, attribName: "nounwind", attribIndex);

    public static void AddReadNoneAttributeAtIndex(this LLVMValueRef value, LLVMContextRef context, LLVMAttributeIndex attribIndex) =>
        value.AddNamedAttributeAtIndex(context, attribName: "readnone", attribIndex);
}
