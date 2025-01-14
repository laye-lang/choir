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

    public static void AddSRetAttributeAtIndex(this LLVMValueRef value, LLVMContextRef context, LLVMAttributeIndex attribIndex, LLVMTypeRef returnType)
    {
        unsafe
        {
            fixed (byte* stringData = Encoding.UTF8.GetBytes("sret"))
            {
                uint kindId = LLVM.GetEnumAttributeKindForName((sbyte*)stringData, 4);
                var attrib = LLVM.CreateTypeAttribute(context, kindId, returnType);
                value.AddAttributeAtIndex(attribIndex, attrib);
            }
        }
    }

    public static void AddNoAliasAttributeAtIndex(this LLVMValueRef value, LLVMContextRef context, LLVMAttributeIndex attribIndex)
    {
        unsafe
        {
            fixed (byte* stringData = Encoding.UTF8.GetBytes("noalias"))
            {
                uint kindId = LLVM.GetEnumAttributeKindForName((sbyte*)stringData, 7);
                var attrib = LLVM.CreateEnumAttribute(context, kindId, 0);
                value.AddAttributeAtIndex(attribIndex, attrib);
            }
        }
    }

    public static void AddWritableAttributeAtIndex(this LLVMValueRef value, LLVMContextRef context, LLVMAttributeIndex attribIndex)
    {
        unsafe
        {
            fixed (byte* stringData = Encoding.UTF8.GetBytes("writable"))
            {
                uint kindId = LLVM.GetEnumAttributeKindForName((sbyte*)stringData, 8);
                var attrib = LLVM.CreateEnumAttribute(context, kindId, 0);
                value.AddAttributeAtIndex(attribIndex, attrib);
            }
        }
    }

    public static void AddDeadOnUnwindAttributeAtIndex(this LLVMValueRef value, LLVMContextRef context, LLVMAttributeIndex attribIndex)
    {
        unsafe
        {
            fixed (byte* stringData = Encoding.UTF8.GetBytes("dead_on_unwind"))
            {
                uint kindId = LLVM.GetEnumAttributeKindForName((sbyte*)stringData, 14);
                var attrib = LLVM.CreateEnumAttribute(context, kindId, 0);
                value.AddAttributeAtIndex(attribIndex, attrib);
            }
        }
    }
}
