/// This file was generated from 'llvm-c/Core.h' in the LLVM C API.

using System.Runtime.InteropServices;

using LLVMBool = int;

namespace Choir.LibLLVM.Interop;

public static partial class LLVM
{
    private const string LLVMCore = "LLVMCore";

    [DllImport(LLVMCore, EntryPoint = "LLVMShutdown", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Shutdown();

    [DllImport(LLVMCore, EntryPoint = "LLVMGetVersion", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetVersion(IntPtr Major, IntPtr Minor, IntPtr Patch);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateMessage", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string CreateMessage([MarshalAs(UnmanagedType.LPStr)] string Message);

    [DllImport(LLVMCore, EntryPoint = "LLVMDisposeMessage", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisposeMessage(IntPtr Message);

    [DllImport(LLVMCore, EntryPoint = "LLVMContextCreate", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ContextCreate();

    [DllImport(LLVMCore, EntryPoint = "LLVMGetGlobalContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetGlobalContext();

    [DllImport(LLVMCore, EntryPoint = "LLVMContextSetDiagnosticHandler", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ContextSetDiagnosticHandler(IntPtr C, IntPtr Handler, IntPtr DiagnosticContext);

    [DllImport(LLVMCore, EntryPoint = "LLVMContextGetDiagnosticHandler", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ContextGetDiagnosticHandler(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMContextGetDiagnosticContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ContextGetDiagnosticContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMContextSetYieldCallback", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ContextSetYieldCallback(IntPtr C, IntPtr Callback, IntPtr OpaqueHandle);

    [DllImport(LLVMCore, EntryPoint = "LLVMContextShouldDiscardValueNames", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool ContextShouldDiscardValueNames(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMContextSetDiscardValueNames", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ContextSetDiscardValueNames(IntPtr C, LLVMBool Discard);

    [DllImport(LLVMCore, EntryPoint = "LLVMContextDispose", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ContextDispose(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetDiagInfoDescription", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetDiagInfoDescription(IntPtr DI);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetDiagInfoSeverity", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMDiagnosticSeverity GetDiagInfoSeverity(IntPtr DI);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetMDKindIDInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetMDKindIDInContext(IntPtr C, [MarshalAs(UnmanagedType.LPStr)] string Name, uint SLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetMDKindID", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetMDKindID([MarshalAs(UnmanagedType.LPStr)] string Name, uint SLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetSyncScopeID", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetSyncScopeID(IntPtr C, [MarshalAs(UnmanagedType.LPStr)] string Name, ulong SLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetEnumAttributeKindForName", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetEnumAttributeKindForName([MarshalAs(UnmanagedType.LPStr)] string Name, ulong SLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetLastEnumAttributeKind", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetLastEnumAttributeKind();

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateEnumAttribute", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateEnumAttribute(IntPtr C, uint KindID, ulong Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetEnumAttributeKind", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetEnumAttributeKind(IntPtr A);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetEnumAttributeValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong GetEnumAttributeValue(IntPtr A);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateTypeAttribute", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateTypeAttribute(IntPtr C, uint KindID, IntPtr type_ref);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetTypeAttributeValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetTypeAttributeValue(IntPtr A);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateConstantRangeAttribute", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateConstantRangeAttribute(IntPtr C, uint KindID, uint NumBits, IntPtr LowerWords, IntPtr UpperWords);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateStringAttribute", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateStringAttribute(IntPtr C, [MarshalAs(UnmanagedType.LPStr)] string K, uint KLength, [MarshalAs(UnmanagedType.LPStr)] string V, uint VLength);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetStringAttributeKind", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetStringAttributeKind(IntPtr A, IntPtr Length);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetStringAttributeValue", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetStringAttributeValue(IntPtr A, IntPtr Length);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsEnumAttribute", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsEnumAttribute(IntPtr A);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsStringAttribute", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsStringAttribute(IntPtr A);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsTypeAttribute", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsTypeAttribute(IntPtr A);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetTypeByName2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetTypeByName2(IntPtr C, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMModuleCreateWithName", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ModuleCreateWithName([MarshalAs(UnmanagedType.LPStr)] string ModuleID);

    [DllImport(LLVMCore, EntryPoint = "LLVMModuleCreateWithNameInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ModuleCreateWithNameInContext([MarshalAs(UnmanagedType.LPStr)] string ModuleID, IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMCloneModule", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CloneModule(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMDisposeModule", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisposeModule(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsNewDbgInfoFormat", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsNewDbgInfoFormat(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetIsNewDbgInfoFormat", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetIsNewDbgInfoFormat(IntPtr M, LLVMBool UseNewFormat);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetModuleIdentifier", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetModuleIdentifier(IntPtr M, IntPtr Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetModuleIdentifier", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetModuleIdentifier(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Ident, ulong Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetSourceFileName", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetSourceFileName(IntPtr M, IntPtr Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetSourceFileName", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetSourceFileName(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name, ulong Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetDataLayoutStr", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetDataLayoutStr(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetDataLayout", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetDataLayout(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetDataLayout", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetDataLayout(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string DataLayoutStr);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetTarget", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetTarget(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetTarget", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetTarget(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Triple);

    [DllImport(LLVMCore, EntryPoint = "LLVMCopyModuleFlagsMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CopyModuleFlagsMetadata(IntPtr M, IntPtr Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMDisposeModuleFlagsMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisposeModuleFlagsMetadata(IntPtr Entries);

    [DllImport(LLVMCore, EntryPoint = "LLVMModuleFlagEntriesGetFlagBehavior", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMModuleFlagBehavior ModuleFlagEntriesGetFlagBehavior(IntPtr Entries, uint Index);

    [DllImport(LLVMCore, EntryPoint = "LLVMModuleFlagEntriesGetKey", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string ModuleFlagEntriesGetKey(IntPtr Entries, uint Index, IntPtr Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMModuleFlagEntriesGetMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ModuleFlagEntriesGetMetadata(IntPtr Entries, uint Index);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetModuleFlag", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetModuleFlag(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Key, ulong KeyLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddModuleFlag", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddModuleFlag(IntPtr M, LLVMModuleFlagBehavior Behavior, [MarshalAs(UnmanagedType.LPStr)] string Key, ulong KeyLen, IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMDumpModule", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DumpModule(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMPrintModuleToFile", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool PrintModuleToFile(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Filename, IntPtr ErrorMessage);

    [DllImport(LLVMCore, EntryPoint = "LLVMPrintModuleToString", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string PrintModuleToString(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetModuleInlineAsm", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetModuleInlineAsm(IntPtr M, IntPtr Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetModuleInlineAsm2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetModuleInlineAsm2(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Asm, ulong Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMAppendModuleInlineAsm", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AppendModuleInlineAsm(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Asm, ulong Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInlineAsm", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetInlineAsm(IntPtr Ty, [MarshalAs(UnmanagedType.LPStr)] string AsmString, ulong AsmStringSize, [MarshalAs(UnmanagedType.LPStr)] string Constraints, ulong ConstraintsSize, LLVMBool HasSideEffects, LLVMBool IsAlignStack, LLVMInlineAsmDialect Dialect, LLVMBool CanThrow);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInlineAsmAsmString", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetInlineAsmAsmString(IntPtr InlineAsmVal, IntPtr Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInlineAsmConstraintString", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetInlineAsmConstraintString(IntPtr InlineAsmVal, IntPtr Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInlineAsmDialect", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMInlineAsmDialect GetInlineAsmDialect(IntPtr InlineAsmVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInlineAsmFunctionType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetInlineAsmFunctionType(IntPtr InlineAsmVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInlineAsmHasSideEffects", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool GetInlineAsmHasSideEffects(IntPtr InlineAsmVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInlineAsmNeedsAlignedStack", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool GetInlineAsmNeedsAlignedStack(IntPtr InlineAsmVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInlineAsmCanUnwind", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool GetInlineAsmCanUnwind(IntPtr InlineAsmVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetModuleContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetModuleContext(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetTypeByName", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetTypeByName(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFirstNamedMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetFirstNamedMetadata(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetLastNamedMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetLastNamedMetadata(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNextNamedMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNextNamedMetadata(IntPtr NamedMDNode);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPreviousNamedMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetPreviousNamedMetadata(IntPtr NamedMDNode);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNamedMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNamedMetadata(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name, ulong NameLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetOrInsertNamedMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetOrInsertNamedMetadata(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name, ulong NameLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNamedMetadataName", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetNamedMetadataName(IntPtr NamedMD, IntPtr NameLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNamedMetadataNumOperands", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetNamedMetadataNumOperands(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNamedMetadataOperands", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetNamedMetadataOperands(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name, IntPtr Dest);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddNamedMetadataOperand", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddNamedMetadataOperand(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name, IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetDebugLocDirectory", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetDebugLocDirectory(IntPtr Val, IntPtr Length);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetDebugLocFilename", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetDebugLocFilename(IntPtr Val, IntPtr Length);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetDebugLocLine", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetDebugLocLine(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetDebugLocColumn", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetDebugLocColumn(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddFunction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr AddFunction(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name, IntPtr FunctionTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNamedFunction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNamedFunction(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNamedFunctionWithLength", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNamedFunctionWithLength(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name, ulong Length);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFirstFunction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetFirstFunction(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetLastFunction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetLastFunction(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNextFunction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNextFunction(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPreviousFunction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetPreviousFunction(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetModuleInlineAsm", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetModuleInlineAsm(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Asm);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetTypeKind", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMTypeKind GetTypeKind(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMTypeIsSized", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool TypeIsSized(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetTypeContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetTypeContext(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMDumpType", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DumpType(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMPrintTypeToString", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string PrintTypeToString(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMInt1TypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Int1TypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMInt8TypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Int8TypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMInt16TypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Int16TypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMInt32TypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Int32TypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMInt64TypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Int64TypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMInt128TypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Int128TypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMIntTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IntTypeInContext(IntPtr C, uint NumBits);

    [DllImport(LLVMCore, EntryPoint = "LLVMInt1Type", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Int1Type();

    [DllImport(LLVMCore, EntryPoint = "LLVMInt8Type", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Int8Type();

    [DllImport(LLVMCore, EntryPoint = "LLVMInt16Type", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Int16Type();

    [DllImport(LLVMCore, EntryPoint = "LLVMInt32Type", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Int32Type();

    [DllImport(LLVMCore, EntryPoint = "LLVMInt64Type", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Int64Type();

    [DllImport(LLVMCore, EntryPoint = "LLVMInt128Type", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr Int128Type();

    [DllImport(LLVMCore, EntryPoint = "LLVMIntType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IntType(uint NumBits);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetIntTypeWidth", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetIntTypeWidth(IntPtr IntegerTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMHalfTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr HalfTypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMBFloatTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BFloatTypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMFloatTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FloatTypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMDoubleTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr DoubleTypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMX86FP80TypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr X86FP80TypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMFP128TypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FP128TypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMPPCFP128TypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr PPCFP128TypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMHalfType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr HalfType();

    [DllImport(LLVMCore, EntryPoint = "LLVMBFloatType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BFloatType();

    [DllImport(LLVMCore, EntryPoint = "LLVMFloatType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FloatType();

    [DllImport(LLVMCore, EntryPoint = "LLVMDoubleType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr DoubleType();

    [DllImport(LLVMCore, EntryPoint = "LLVMX86FP80Type", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr X86FP80Type();

    [DllImport(LLVMCore, EntryPoint = "LLVMFP128Type", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FP128Type();

    [DllImport(LLVMCore, EntryPoint = "LLVMPPCFP128Type", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr PPCFP128Type();

    [DllImport(LLVMCore, EntryPoint = "LLVMFunctionType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FunctionType(IntPtr ReturnType, IntPtr ParamTypes, uint ParamCount, LLVMBool IsVarArg);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsFunctionVarArg", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsFunctionVarArg(IntPtr FunctionTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetReturnType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetReturnType(IntPtr FunctionTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMCountParamTypes", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint CountParamTypes(IntPtr FunctionTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetParamTypes", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetParamTypes(IntPtr FunctionTy, IntPtr Dest);

    [DllImport(LLVMCore, EntryPoint = "LLVMStructTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr StructTypeInContext(IntPtr C, IntPtr ElementTypes, uint ElementCount, LLVMBool Packed);

    [DllImport(LLVMCore, EntryPoint = "LLVMStructType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr StructType(IntPtr ElementTypes, uint ElementCount, LLVMBool Packed);

    [DllImport(LLVMCore, EntryPoint = "LLVMStructCreateNamed", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr StructCreateNamed(IntPtr C, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetStructName", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetStructName(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMStructSetBody", CallingConvention = CallingConvention.Cdecl)]
    public static extern void StructSetBody(IntPtr StructTy, IntPtr ElementTypes, uint ElementCount, LLVMBool Packed);

    [DllImport(LLVMCore, EntryPoint = "LLVMCountStructElementTypes", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint CountStructElementTypes(IntPtr StructTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetStructElementTypes", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetStructElementTypes(IntPtr StructTy, IntPtr Dest);

    [DllImport(LLVMCore, EntryPoint = "LLVMStructGetTypeAtIndex", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr StructGetTypeAtIndex(IntPtr StructTy, uint i);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsPackedStruct", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsPackedStruct(IntPtr StructTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsOpaqueStruct", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsOpaqueStruct(IntPtr StructTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsLiteralStruct", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsLiteralStruct(IntPtr StructTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetElementType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetElementType(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetSubtypes", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetSubtypes(IntPtr Tp, IntPtr Arr);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNumContainedTypes", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetNumContainedTypes(IntPtr Tp);

    [DllImport(LLVMCore, EntryPoint = "LLVMArrayType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ArrayType(IntPtr ElementType, uint ElementCount);

    [DllImport(LLVMCore, EntryPoint = "LLVMArrayType2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ArrayType2(IntPtr ElementType, ulong ElementCount);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetArrayLength", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetArrayLength(IntPtr ArrayTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetArrayLength2", CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong GetArrayLength2(IntPtr ArrayTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMPointerType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr PointerType(IntPtr ElementType, uint AddressSpace);

    [DllImport(LLVMCore, EntryPoint = "LLVMPointerTypeIsOpaque", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool PointerTypeIsOpaque(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMPointerTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr PointerTypeInContext(IntPtr C, uint AddressSpace);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPointerAddressSpace", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetPointerAddressSpace(IntPtr PointerTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMVectorType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr VectorType(IntPtr ElementType, uint ElementCount);

    [DllImport(LLVMCore, EntryPoint = "LLVMScalableVectorType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ScalableVectorType(IntPtr ElementType, uint ElementCount);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetVectorSize", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetVectorSize(IntPtr VectorTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetConstantPtrAuthPointer", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetConstantPtrAuthPointer(IntPtr PtrAuth);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetConstantPtrAuthKey", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetConstantPtrAuthKey(IntPtr PtrAuth);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetConstantPtrAuthDiscriminator", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetConstantPtrAuthDiscriminator(IntPtr PtrAuth);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetConstantPtrAuthAddrDiscriminator", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetConstantPtrAuthAddrDiscriminator(IntPtr PtrAuth);

    [DllImport(LLVMCore, EntryPoint = "LLVMVoidTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr VoidTypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMLabelTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr LabelTypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMX86AMXTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr X86AMXTypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMTokenTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TokenTypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMMetadataTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr MetadataTypeInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMVoidType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr VoidType();

    [DllImport(LLVMCore, EntryPoint = "LLVMLabelType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr LabelType();

    [DllImport(LLVMCore, EntryPoint = "LLVMX86AMXType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr X86AMXType();

    [DllImport(LLVMCore, EntryPoint = "LLVMTargetExtTypeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TargetExtTypeInContext(IntPtr C, [MarshalAs(UnmanagedType.LPStr)] string Name, IntPtr TypeParams, uint TypeParamCount, IntPtr IntParams, uint IntParamCount);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetTargetExtTypeName", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetTargetExtTypeName(IntPtr TargetExtTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetTargetExtTypeNumTypeParams", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetTargetExtTypeNumTypeParams(IntPtr TargetExtTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetTargetExtTypeTypeParam", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetTargetExtTypeTypeParam(IntPtr TargetExtTy, uint Idx);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetTargetExtTypeNumIntParams", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetTargetExtTypeNumIntParams(IntPtr TargetExtTy);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetTargetExtTypeIntParam", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetTargetExtTypeIntParam(IntPtr TargetExtTy, uint Idx);

    [DllImport(LLVMCore, EntryPoint = "LLVMTypeOf", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr TypeOf(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetValueKind", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMValueKind GetValueKind(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetValueName2", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetValueName2(IntPtr Val, IntPtr Length);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetValueName2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetValueName2(IntPtr Val, [MarshalAs(UnmanagedType.LPStr)] string Name, ulong NameLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMDumpValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DumpValue(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMPrintValueToString", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string PrintValueToString(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetValueContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetValueContext(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMPrintDbgRecordToString", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string PrintDbgRecordToString(int Record);

    [DllImport(LLVMCore, EntryPoint = "LLVMReplaceAllUsesWith", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ReplaceAllUsesWith(IntPtr OldVal, IntPtr NewVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsConstant", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsConstant(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsUndef", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsUndef(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsPoison", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsPoison(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAArgument", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAArgument(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsABasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsABasicBlock(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAInlineAsm", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAInlineAsm(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAUser", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAUser(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstant", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstant(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsABlockAddress", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsABlockAddress(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantAggregateZero", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantAggregateZero(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantArray", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantArray(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantDataSequential", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantDataSequential(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantDataArray", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantDataArray(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantDataVector", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantDataVector(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantExpr", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantExpr(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantFP", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantFP(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantInt", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantInt(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantPointerNull", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantPointerNull(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantStruct", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantStruct(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantTokenNone", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantTokenNone(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantVector", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantVector(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAConstantPtrAuth", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAConstantPtrAuth(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAGlobalValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAGlobalValue(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAGlobalAlias", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAGlobalAlias(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAGlobalObject", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAGlobalObject(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAFunction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAFunction(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAGlobalVariable", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAGlobalVariable(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAGlobalIFunc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAGlobalIFunc(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAUndefValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAUndefValue(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAPoisonValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAPoisonValue(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAInstruction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAInstruction(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAUnaryOperator", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAUnaryOperator(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsABinaryOperator", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsABinaryOperator(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsACallInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsACallInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAIntrinsicInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAIntrinsicInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsADbgInfoIntrinsic", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsADbgInfoIntrinsic(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsADbgVariableIntrinsic", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsADbgVariableIntrinsic(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsADbgDeclareInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsADbgDeclareInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsADbgLabelInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsADbgLabelInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAMemIntrinsic", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAMemIntrinsic(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAMemCpyInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAMemCpyInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAMemMoveInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAMemMoveInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAMemSetInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAMemSetInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsACmpInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsACmpInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAFCmpInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAFCmpInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAICmpInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAICmpInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAExtractElementInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAExtractElementInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAGetElementPtrInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAGetElementPtrInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAInsertElementInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAInsertElementInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAInsertValueInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAInsertValueInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsALandingPadInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsALandingPadInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAPHINode", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAPHINode(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsASelectInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsASelectInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAShuffleVectorInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAShuffleVectorInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAStoreInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAStoreInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsABranchInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsABranchInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAIndirectBrInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAIndirectBrInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAInvokeInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAInvokeInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAReturnInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAReturnInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsASwitchInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsASwitchInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAUnreachableInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAUnreachableInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAResumeInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAResumeInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsACleanupReturnInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsACleanupReturnInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsACatchReturnInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsACatchReturnInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsACatchSwitchInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsACatchSwitchInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsACallBrInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsACallBrInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAFuncletPadInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAFuncletPadInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsACatchPadInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsACatchPadInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsACleanupPadInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsACleanupPadInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAUnaryInstruction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAUnaryInstruction(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAAllocaInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAAllocaInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsACastInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsACastInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAAddrSpaceCastInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAAddrSpaceCastInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsABitCastInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsABitCastInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAFPExtInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAFPExtInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAFPToSIInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAFPToSIInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAFPToUIInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAFPToUIInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAFPTruncInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAFPTruncInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAIntToPtrInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAIntToPtrInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAPtrToIntInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAPtrToIntInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsASExtInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsASExtInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsASIToFPInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsASIToFPInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsATruncInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsATruncInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAUIToFPInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAUIToFPInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAZExtInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAZExtInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAExtractValueInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAExtractValueInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsALoadInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsALoadInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAVAArgInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAVAArgInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAFreezeInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAFreezeInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAAtomicCmpXchgInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAAtomicCmpXchgInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAAtomicRMWInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAAtomicRMWInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAFenceInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAFenceInst(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAMDNode", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAMDNode(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAValueAsMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAValueAsMetadata(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAMDString", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsAMDString(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetValueName", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetValueName(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetValueName", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetValueName(IntPtr Val, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFirstUse", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetFirstUse(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNextUse", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNextUse(IntPtr U);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetUser", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetUser(IntPtr U);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetUsedValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetUsedValue(IntPtr U);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetOperand", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetOperand(IntPtr Val, uint Index);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetOperandUse", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetOperandUse(IntPtr Val, uint Index);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetOperand", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetOperand(IntPtr User, uint Index, IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNumOperands", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetNumOperands(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstNull", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstNull(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstAllOnes", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstAllOnes(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetUndef", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetUndef(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPoison", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetPoison(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsNull", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsNull(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstPointerNull", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstPointerNull(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstInt", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstInt(IntPtr IntTy, ulong N, LLVMBool SignExtend);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstIntOfArbitraryPrecision", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstIntOfArbitraryPrecision(IntPtr IntTy, uint NumWords, IntPtr Words);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstIntOfString", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstIntOfString(IntPtr IntTy, [MarshalAs(UnmanagedType.LPStr)] string Text, byte Radix);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstIntOfStringAndSize", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstIntOfStringAndSize(IntPtr IntTy, [MarshalAs(UnmanagedType.LPStr)] string Text, uint SLen, byte Radix);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstReal", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstReal(IntPtr RealTy, double N);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstRealOfString", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstRealOfString(IntPtr RealTy, [MarshalAs(UnmanagedType.LPStr)] string Text);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstRealOfStringAndSize", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstRealOfStringAndSize(IntPtr RealTy, [MarshalAs(UnmanagedType.LPStr)] string Text, uint SLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstIntGetZExtValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong ConstIntGetZExtValue(IntPtr ConstantVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstIntGetSExtValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern long ConstIntGetSExtValue(IntPtr ConstantVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstRealGetDouble", CallingConvention = CallingConvention.Cdecl)]
    public static extern double ConstRealGetDouble(IntPtr ConstantVal, IntPtr losesInfo);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstStringInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstStringInContext(IntPtr C, [MarshalAs(UnmanagedType.LPStr)] string Str, uint Length, LLVMBool DontNullTerminate);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstStringInContext2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstStringInContext2(IntPtr C, [MarshalAs(UnmanagedType.LPStr)] string Str, ulong Length, LLVMBool DontNullTerminate);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstString", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstString([MarshalAs(UnmanagedType.LPStr)] string Str, uint Length, LLVMBool DontNullTerminate);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsConstantString", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsConstantString(IntPtr c);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetAsString", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetAsString(IntPtr c, IntPtr Length);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstStructInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstStructInContext(IntPtr C, IntPtr ConstantVals, uint Count, LLVMBool Packed);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstStruct", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstStruct(IntPtr ConstantVals, uint Count, LLVMBool Packed);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstArray", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstArray(IntPtr ElementTy, IntPtr ConstantVals, uint Length);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstArray2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstArray2(IntPtr ElementTy, IntPtr ConstantVals, ulong Length);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstNamedStruct", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstNamedStruct(IntPtr StructTy, IntPtr ConstantVals, uint Count);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetAggregateElement", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetAggregateElement(IntPtr C, uint Idx);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetElementAsConstant", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetElementAsConstant(IntPtr C, uint idx);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstVector", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstVector(IntPtr ScalarConstantVals, uint Size);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstantPtrAuth", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstantPtrAuth(IntPtr Ptr, IntPtr Key, IntPtr Disc, IntPtr AddrDisc);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetConstOpcode", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMOpcode GetConstOpcode(IntPtr ConstantVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMAlignOf", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr AlignOf(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMSizeOf", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SizeOf(IntPtr Ty);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstNeg", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstNeg(IntPtr ConstantVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstNSWNeg", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstNSWNeg(IntPtr ConstantVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstNUWNeg", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstNUWNeg(IntPtr ConstantVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstNot", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstNot(IntPtr ConstantVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstAdd", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstAdd(IntPtr LHSConstant, IntPtr RHSConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstNSWAdd", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstNSWAdd(IntPtr LHSConstant, IntPtr RHSConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstNUWAdd", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstNUWAdd(IntPtr LHSConstant, IntPtr RHSConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstSub", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstSub(IntPtr LHSConstant, IntPtr RHSConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstNSWSub", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstNSWSub(IntPtr LHSConstant, IntPtr RHSConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstNUWSub", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstNUWSub(IntPtr LHSConstant, IntPtr RHSConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstMul", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstMul(IntPtr LHSConstant, IntPtr RHSConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstNSWMul", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstNSWMul(IntPtr LHSConstant, IntPtr RHSConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstNUWMul", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstNUWMul(IntPtr LHSConstant, IntPtr RHSConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstXor", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstXor(IntPtr LHSConstant, IntPtr RHSConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstGEP2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstGEP2(IntPtr Ty, IntPtr ConstantVal, IntPtr ConstantIndices, uint NumIndices);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstInBoundsGEP2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstInBoundsGEP2(IntPtr Ty, IntPtr ConstantVal, IntPtr ConstantIndices, uint NumIndices);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstGEPWithNoWrapFlags", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstGEPWithNoWrapFlags(IntPtr Ty, IntPtr ConstantVal, IntPtr ConstantIndices, uint NumIndices, uint NoWrapFlags);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstTrunc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstTrunc(IntPtr ConstantVal, IntPtr ToType);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstPtrToInt", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstPtrToInt(IntPtr ConstantVal, IntPtr ToType);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstIntToPtr", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstIntToPtr(IntPtr ConstantVal, IntPtr ToType);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstBitCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstBitCast(IntPtr ConstantVal, IntPtr ToType);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstAddrSpaceCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstAddrSpaceCast(IntPtr ConstantVal, IntPtr ToType);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstTruncOrBitCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstTruncOrBitCast(IntPtr ConstantVal, IntPtr ToType);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstPointerCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstPointerCast(IntPtr ConstantVal, IntPtr ToType);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstExtractElement", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstExtractElement(IntPtr VectorConstant, IntPtr IndexConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstInsertElement", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstInsertElement(IntPtr VectorConstant, IntPtr ElementValueConstant, IntPtr IndexConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstShuffleVector", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstShuffleVector(IntPtr VectorAConstant, IntPtr VectorBConstant, IntPtr MaskConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMBlockAddress", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BlockAddress(IntPtr F, IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetBlockAddressFunction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetBlockAddressFunction(IntPtr BlockAddr);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetBlockAddressBasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetBlockAddressBasicBlock(IntPtr BlockAddr);

    [DllImport(LLVMCore, EntryPoint = "LLVMConstInlineAsm", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ConstInlineAsm(IntPtr Ty, [MarshalAs(UnmanagedType.LPStr)] string AsmString, [MarshalAs(UnmanagedType.LPStr)] string Constraints, LLVMBool HasSideEffects, LLVMBool IsAlignStack);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetGlobalParent", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetGlobalParent(IntPtr Global);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsDeclaration", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsDeclaration(IntPtr Global);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetLinkage", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMLinkage GetLinkage(IntPtr Global);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetLinkage", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetLinkage(IntPtr Global, LLVMLinkage Linkage);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetSection", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetSection(IntPtr Global);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetSection", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetSection(IntPtr Global, [MarshalAs(UnmanagedType.LPStr)] string Section);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetVisibility", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMVisibility GetVisibility(IntPtr Global);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetVisibility", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetVisibility(IntPtr Global, LLVMVisibility Viz);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetDLLStorageClass", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMDLLStorageClass GetDLLStorageClass(IntPtr Global);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetDLLStorageClass", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetDLLStorageClass(IntPtr Global, LLVMDLLStorageClass Class);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetUnnamedAddress", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMUnnamedAddr GetUnnamedAddress(IntPtr Global);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetUnnamedAddress", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetUnnamedAddress(IntPtr Global, LLVMUnnamedAddr UnnamedAddr);

    [DllImport(LLVMCore, EntryPoint = "LLVMGlobalGetValueType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GlobalGetValueType(IntPtr Global);

    [DllImport(LLVMCore, EntryPoint = "LLVMHasUnnamedAddr", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool HasUnnamedAddr(IntPtr Global);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetUnnamedAddr", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetUnnamedAddr(IntPtr Global, LLVMBool HasUnnamedAddr);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetAlignment", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetAlignment(IntPtr V);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetAlignment", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetAlignment(IntPtr V, uint Bytes);

    [DllImport(LLVMCore, EntryPoint = "LLVMGlobalSetMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GlobalSetMetadata(IntPtr Global, uint Kind, IntPtr MD);

    [DllImport(LLVMCore, EntryPoint = "LLVMGlobalEraseMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GlobalEraseMetadata(IntPtr Global, uint Kind);

    [DllImport(LLVMCore, EntryPoint = "LLVMGlobalClearMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GlobalClearMetadata(IntPtr Global);

    [DllImport(LLVMCore, EntryPoint = "LLVMGlobalCopyAllMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GlobalCopyAllMetadata(IntPtr Value, IntPtr NumEntries);

    [DllImport(LLVMCore, EntryPoint = "LLVMDisposeValueMetadataEntries", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisposeValueMetadataEntries(IntPtr Entries);

    [DllImport(LLVMCore, EntryPoint = "LLVMValueMetadataEntriesGetKind", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint ValueMetadataEntriesGetKind(IntPtr Entries, uint Index);

    [DllImport(LLVMCore, EntryPoint = "LLVMValueMetadataEntriesGetMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ValueMetadataEntriesGetMetadata(IntPtr Entries, uint Index);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddGlobal", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr AddGlobal(IntPtr M, IntPtr Ty, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddGlobalInAddressSpace", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr AddGlobalInAddressSpace(IntPtr M, IntPtr Ty, [MarshalAs(UnmanagedType.LPStr)] string Name, uint AddressSpace);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNamedGlobal", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNamedGlobal(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNamedGlobalWithLength", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNamedGlobalWithLength(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name, ulong Length);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFirstGlobal", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetFirstGlobal(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetLastGlobal", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetLastGlobal(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNextGlobal", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNextGlobal(IntPtr GlobalVar);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPreviousGlobal", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetPreviousGlobal(IntPtr GlobalVar);

    [DllImport(LLVMCore, EntryPoint = "LLVMDeleteGlobal", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DeleteGlobal(IntPtr GlobalVar);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInitializer", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetInitializer(IntPtr GlobalVar);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetInitializer", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetInitializer(IntPtr GlobalVar, IntPtr ConstantVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsThreadLocal", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsThreadLocal(IntPtr GlobalVar);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetThreadLocal", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetThreadLocal(IntPtr GlobalVar, LLVMBool IsThreadLocal);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsGlobalConstant", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsGlobalConstant(IntPtr GlobalVar);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetGlobalConstant", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetGlobalConstant(IntPtr GlobalVar, LLVMBool IsConstant);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetThreadLocalMode", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMThreadLocalMode GetThreadLocalMode(IntPtr GlobalVar);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetThreadLocalMode", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetThreadLocalMode(IntPtr GlobalVar, LLVMThreadLocalMode Mode);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsExternallyInitialized", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsExternallyInitialized(IntPtr GlobalVar);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetExternallyInitialized", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetExternallyInitialized(IntPtr GlobalVar, LLVMBool IsExtInit);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddAlias2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr AddAlias2(IntPtr M, IntPtr ValueTy, uint AddrSpace, IntPtr Aliasee, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNamedGlobalAlias", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNamedGlobalAlias(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name, ulong NameLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFirstGlobalAlias", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetFirstGlobalAlias(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetLastGlobalAlias", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetLastGlobalAlias(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNextGlobalAlias", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNextGlobalAlias(IntPtr GA);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPreviousGlobalAlias", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetPreviousGlobalAlias(IntPtr GA);

    [DllImport(LLVMCore, EntryPoint = "LLVMAliasGetAliasee", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr AliasGetAliasee(IntPtr Alias);

    [DllImport(LLVMCore, EntryPoint = "LLVMAliasSetAliasee", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AliasSetAliasee(IntPtr Alias, IntPtr Aliasee);

    [DllImport(LLVMCore, EntryPoint = "LLVMDeleteFunction", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DeleteFunction(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMHasPersonalityFn", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool HasPersonalityFn(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPersonalityFn", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetPersonalityFn(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetPersonalityFn", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetPersonalityFn(IntPtr Fn, IntPtr PersonalityFn);

    [DllImport(LLVMCore, EntryPoint = "LLVMLookupIntrinsicID", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint LookupIntrinsicID([MarshalAs(UnmanagedType.LPStr)] string Name, ulong NameLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetIntrinsicID", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetIntrinsicID(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetIntrinsicDeclaration", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetIntrinsicDeclaration(IntPtr Mod, uint ID, IntPtr ParamTypes, ulong ParamCount);

    [DllImport(LLVMCore, EntryPoint = "LLVMIntrinsicGetType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IntrinsicGetType(IntPtr Ctx, uint ID, IntPtr ParamTypes, ulong ParamCount);

    [DllImport(LLVMCore, EntryPoint = "LLVMIntrinsicGetName", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string IntrinsicGetName(uint ID, IntPtr NameLength);

    [DllImport(LLVMCore, EntryPoint = "LLVMIntrinsicCopyOverloadedName", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string IntrinsicCopyOverloadedName(uint ID, IntPtr ParamTypes, ulong ParamCount, IntPtr NameLength);

    [DllImport(LLVMCore, EntryPoint = "LLVMIntrinsicCopyOverloadedName2", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string IntrinsicCopyOverloadedName2(IntPtr Mod, uint ID, IntPtr ParamTypes, ulong ParamCount, IntPtr NameLength);

    [DllImport(LLVMCore, EntryPoint = "LLVMIntrinsicIsOverloaded", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IntrinsicIsOverloaded(uint ID);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFunctionCallConv", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetFunctionCallConv(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetFunctionCallConv", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetFunctionCallConv(IntPtr Fn, uint CC);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetGC", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetGC(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetGC", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetGC(IntPtr Fn, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPrefixData", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetPrefixData(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMHasPrefixData", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool HasPrefixData(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetPrefixData", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetPrefixData(IntPtr Fn, IntPtr prefixData);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPrologueData", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetPrologueData(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMHasPrologueData", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool HasPrologueData(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetPrologueData", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetPrologueData(IntPtr Fn, IntPtr prologueData);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddAttributeAtIndex", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddAttributeAtIndex(IntPtr F, uint Idx, IntPtr A);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetAttributeCountAtIndex", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetAttributeCountAtIndex(IntPtr F, uint Idx);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetAttributesAtIndex", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetAttributesAtIndex(IntPtr F, uint Idx, IntPtr Attrs);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetEnumAttributeAtIndex", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetEnumAttributeAtIndex(IntPtr F, uint Idx, uint KindID);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetStringAttributeAtIndex", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetStringAttributeAtIndex(IntPtr F, uint Idx, [MarshalAs(UnmanagedType.LPStr)] string K, uint KLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMRemoveEnumAttributeAtIndex", CallingConvention = CallingConvention.Cdecl)]
    public static extern void RemoveEnumAttributeAtIndex(IntPtr F, uint Idx, uint KindID);

    [DllImport(LLVMCore, EntryPoint = "LLVMRemoveStringAttributeAtIndex", CallingConvention = CallingConvention.Cdecl)]
    public static extern void RemoveStringAttributeAtIndex(IntPtr F, uint Idx, [MarshalAs(UnmanagedType.LPStr)] string K, uint KLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddTargetDependentFunctionAttr", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddTargetDependentFunctionAttr(IntPtr Fn, [MarshalAs(UnmanagedType.LPStr)] string A, [MarshalAs(UnmanagedType.LPStr)] string V);

    [DllImport(LLVMCore, EntryPoint = "LLVMCountParams", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint CountParams(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetParams", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetParams(IntPtr Fn, IntPtr Params);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetParam", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetParam(IntPtr Fn, uint Index);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetParamParent", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetParamParent(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFirstParam", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetFirstParam(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetLastParam", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetLastParam(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNextParam", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNextParam(IntPtr Arg);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPreviousParam", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetPreviousParam(IntPtr Arg);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetParamAlignment", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetParamAlignment(IntPtr Arg, uint Align);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddGlobalIFunc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr AddGlobalIFunc(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name, ulong NameLen, IntPtr Ty, uint AddrSpace, IntPtr Resolver);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNamedGlobalIFunc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNamedGlobalIFunc(IntPtr M, [MarshalAs(UnmanagedType.LPStr)] string Name, ulong NameLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFirstGlobalIFunc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetFirstGlobalIFunc(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetLastGlobalIFunc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetLastGlobalIFunc(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNextGlobalIFunc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNextGlobalIFunc(IntPtr IFunc);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPreviousGlobalIFunc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetPreviousGlobalIFunc(IntPtr IFunc);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetGlobalIFuncResolver", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetGlobalIFuncResolver(IntPtr IFunc);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetGlobalIFuncResolver", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetGlobalIFuncResolver(IntPtr IFunc, IntPtr Resolver);

    [DllImport(LLVMCore, EntryPoint = "LLVMEraseGlobalIFunc", CallingConvention = CallingConvention.Cdecl)]
    public static extern void EraseGlobalIFunc(IntPtr IFunc);

    [DllImport(LLVMCore, EntryPoint = "LLVMRemoveGlobalIFunc", CallingConvention = CallingConvention.Cdecl)]
    public static extern void RemoveGlobalIFunc(IntPtr IFunc);

    [DllImport(LLVMCore, EntryPoint = "LLVMMDStringInContext2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr MDStringInContext2(IntPtr C, [MarshalAs(UnmanagedType.LPStr)] string Str, ulong SLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMMDNodeInContext2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr MDNodeInContext2(IntPtr C, IntPtr MDs, ulong Count);

    [DllImport(LLVMCore, EntryPoint = "LLVMMetadataAsValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr MetadataAsValue(IntPtr C, IntPtr MD);

    [DllImport(LLVMCore, EntryPoint = "LLVMValueAsMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ValueAsMetadata(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetMDString", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetMDString(IntPtr V, IntPtr Length);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetMDNodeNumOperands", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetMDNodeNumOperands(IntPtr V);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetMDNodeOperands", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetMDNodeOperands(IntPtr V, IntPtr Dest);

    [DllImport(LLVMCore, EntryPoint = "LLVMReplaceMDNodeOperandWith", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ReplaceMDNodeOperandWith(IntPtr V, uint Index, IntPtr Replacement);

    [DllImport(LLVMCore, EntryPoint = "LLVMMDStringInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr MDStringInContext(IntPtr C, [MarshalAs(UnmanagedType.LPStr)] string Str, uint SLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMMDString", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr MDString([MarshalAs(UnmanagedType.LPStr)] string Str, uint SLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMMDNodeInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr MDNodeInContext(IntPtr C, IntPtr Vals, uint Count);

    [DllImport(LLVMCore, EntryPoint = "LLVMMDNode", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr MDNode(IntPtr Vals, uint Count);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateOperandBundle", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateOperandBundle([MarshalAs(UnmanagedType.LPStr)] string Tag, ulong TagLen, IntPtr Args, uint NumArgs);

    [DllImport(LLVMCore, EntryPoint = "LLVMDisposeOperandBundle", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisposeOperandBundle(IntPtr Bundle);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetOperandBundleTag", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetOperandBundleTag(IntPtr Bundle, IntPtr Len);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNumOperandBundleArgs", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetNumOperandBundleArgs(IntPtr Bundle);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetOperandBundleArgAtIndex", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetOperandBundleArgAtIndex(IntPtr Bundle, uint Index);

    [DllImport(LLVMCore, EntryPoint = "LLVMBasicBlockAsValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BasicBlockAsValue(IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMValueIsBasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool ValueIsBasicBlock(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMValueAsBasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr ValueAsBasicBlock(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetBasicBlockName", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetBasicBlockName(IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetBasicBlockParent", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetBasicBlockParent(IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetBasicBlockTerminator", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetBasicBlockTerminator(IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMCountBasicBlocks", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint CountBasicBlocks(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetBasicBlocks", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetBasicBlocks(IntPtr Fn, IntPtr BasicBlocks);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFirstBasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetFirstBasicBlock(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetLastBasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetLastBasicBlock(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNextBasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNextBasicBlock(IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPreviousBasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetPreviousBasicBlock(IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetEntryBasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetEntryBasicBlock(IntPtr Fn);

    [DllImport(LLVMCore, EntryPoint = "LLVMInsertExistingBasicBlockAfterInsertBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern void InsertExistingBasicBlockAfterInsertBlock(IntPtr Builder, IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMAppendExistingBasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AppendExistingBasicBlock(IntPtr Fn, IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateBasicBlockInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateBasicBlockInContext(IntPtr C, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMAppendBasicBlockInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr AppendBasicBlockInContext(IntPtr C, IntPtr Fn, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMAppendBasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr AppendBasicBlock(IntPtr Fn, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMInsertBasicBlockInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr InsertBasicBlockInContext(IntPtr C, IntPtr BB, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMInsertBasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr InsertBasicBlock(IntPtr InsertBeforeBB, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMDeleteBasicBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DeleteBasicBlock(IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMRemoveBasicBlockFromParent", CallingConvention = CallingConvention.Cdecl)]
    public static extern void RemoveBasicBlockFromParent(IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMMoveBasicBlockBefore", CallingConvention = CallingConvention.Cdecl)]
    public static extern void MoveBasicBlockBefore(IntPtr BB, IntPtr MovePos);

    [DllImport(LLVMCore, EntryPoint = "LLVMMoveBasicBlockAfter", CallingConvention = CallingConvention.Cdecl)]
    public static extern void MoveBasicBlockAfter(IntPtr BB, IntPtr MovePos);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFirstInstruction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetFirstInstruction(IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetLastInstruction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetLastInstruction(IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMHasMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern int HasMetadata(IntPtr Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetMetadata(IntPtr Val, uint KindID);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetMetadata", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetMetadata(IntPtr Val, uint KindID, IntPtr Node);

    [DllImport(LLVMCore, EntryPoint = "LLVMInstructionGetAllMetadataOtherThanDebugLoc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr InstructionGetAllMetadataOtherThanDebugLoc(IntPtr Instr, IntPtr NumEntries);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInstructionParent", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetInstructionParent(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNextInstruction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNextInstruction(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPreviousInstruction", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetPreviousInstruction(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMInstructionRemoveFromParent", CallingConvention = CallingConvention.Cdecl)]
    public static extern void InstructionRemoveFromParent(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMInstructionEraseFromParent", CallingConvention = CallingConvention.Cdecl)]
    public static extern void InstructionEraseFromParent(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMDeleteInstruction", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DeleteInstruction(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInstructionOpcode", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMOpcode GetInstructionOpcode(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetICmpPredicate", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMIntPredicate GetICmpPredicate(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFCmpPredicate", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMRealPredicate GetFCmpPredicate(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMInstructionClone", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr InstructionClone(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsATerminatorInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr IsATerminatorInst(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFirstDbgRecord", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetFirstDbgRecord(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetLastDbgRecord", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetLastDbgRecord(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNextDbgRecord", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetNextDbgRecord(int DbgRecord);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetPreviousDbgRecord", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetPreviousDbgRecord(int DbgRecord);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNumArgOperands", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetNumArgOperands(IntPtr Instr);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetInstructionCallConv", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetInstructionCallConv(IntPtr Instr, uint CC);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInstructionCallConv", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetInstructionCallConv(IntPtr Instr);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetInstrParamAlignment", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetInstrParamAlignment(IntPtr Instr, uint Idx, uint Align);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddCallSiteAttribute", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddCallSiteAttribute(IntPtr C, uint Idx, IntPtr A);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCallSiteAttributeCount", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetCallSiteAttributeCount(IntPtr C, uint Idx);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCallSiteAttributes", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetCallSiteAttributes(IntPtr C, uint Idx, IntPtr Attrs);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCallSiteEnumAttribute", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetCallSiteEnumAttribute(IntPtr C, uint Idx, uint KindID);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCallSiteStringAttribute", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetCallSiteStringAttribute(IntPtr C, uint Idx, [MarshalAs(UnmanagedType.LPStr)] string K, uint KLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMRemoveCallSiteEnumAttribute", CallingConvention = CallingConvention.Cdecl)]
    public static extern void RemoveCallSiteEnumAttribute(IntPtr C, uint Idx, uint KindID);

    [DllImport(LLVMCore, EntryPoint = "LLVMRemoveCallSiteStringAttribute", CallingConvention = CallingConvention.Cdecl)]
    public static extern void RemoveCallSiteStringAttribute(IntPtr C, uint Idx, [MarshalAs(UnmanagedType.LPStr)] string K, uint KLen);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCalledFunctionType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetCalledFunctionType(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCalledValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetCalledValue(IntPtr Instr);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNumOperandBundles", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetNumOperandBundles(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetOperandBundleAtIndex", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetOperandBundleAtIndex(IntPtr C, uint Index);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsTailCall", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsTailCall(IntPtr CallInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetTailCall", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetTailCall(IntPtr CallInst, LLVMBool IsTailCall);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetTailCallKind", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMTailCallKind GetTailCallKind(IntPtr CallInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetTailCallKind", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetTailCallKind(IntPtr CallInst, LLVMTailCallKind kind);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNormalDest", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetNormalDest(IntPtr InvokeInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetUnwindDest", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetUnwindDest(IntPtr InvokeInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetNormalDest", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetNormalDest(IntPtr InvokeInst, IntPtr B);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetUnwindDest", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetUnwindDest(IntPtr InvokeInst, IntPtr B);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCallBrDefaultDest", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetCallBrDefaultDest(IntPtr CallBr);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCallBrNumIndirectDests", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetCallBrNumIndirectDests(IntPtr CallBr);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCallBrIndirectDest", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetCallBrIndirectDest(IntPtr CallBr, uint Idx);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNumSuccessors", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetNumSuccessors(IntPtr Term);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetSuccessor", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetSuccessor(IntPtr Term, uint i);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetSuccessor", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetSuccessor(IntPtr Term, uint i, IntPtr block);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsConditional", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsConditional(IntPtr Branch);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCondition", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetCondition(IntPtr Branch);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetCondition", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetCondition(IntPtr Branch, IntPtr Cond);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetSwitchDefaultDest", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetSwitchDefaultDest(IntPtr SwitchInstr);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetAllocatedType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetAllocatedType(IntPtr Alloca);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsInBounds", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsInBounds(IntPtr GEP);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetIsInBounds", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetIsInBounds(IntPtr GEP, LLVMBool InBounds);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetGEPSourceElementType", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetGEPSourceElementType(IntPtr GEP);

    [DllImport(LLVMCore, EntryPoint = "LLVMGEPGetNoWrapFlags", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GEPGetNoWrapFlags(IntPtr GEP);

    [DllImport(LLVMCore, EntryPoint = "LLVMGEPSetNoWrapFlags", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GEPSetNoWrapFlags(IntPtr GEP, uint NoWrapFlags);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddIncoming", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddIncoming(IntPtr PhiNode, IntPtr IncomingValues, IntPtr IncomingBlocks, uint Count);

    [DllImport(LLVMCore, EntryPoint = "LLVMCountIncoming", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint CountIncoming(IntPtr PhiNode);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetIncomingValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetIncomingValue(IntPtr PhiNode, uint Index);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetIncomingBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetIncomingBlock(IntPtr PhiNode, uint Index);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNumIndices", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetNumIndices(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetIndices", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetIndices(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateBuilderInContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateBuilderInContext(IntPtr C);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateBuilder", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateBuilder();

    [DllImport(LLVMCore, EntryPoint = "LLVMPositionBuilder", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PositionBuilder(IntPtr Builder, IntPtr Block, IntPtr Instr);

    [DllImport(LLVMCore, EntryPoint = "LLVMPositionBuilderBeforeDbgRecords", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PositionBuilderBeforeDbgRecords(IntPtr Builder, IntPtr Block, IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMPositionBuilderBefore", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PositionBuilderBefore(IntPtr Builder, IntPtr Instr);

    [DllImport(LLVMCore, EntryPoint = "LLVMPositionBuilderBeforeInstrAndDbgRecords", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PositionBuilderBeforeInstrAndDbgRecords(IntPtr Builder, IntPtr Instr);

    [DllImport(LLVMCore, EntryPoint = "LLVMPositionBuilderAtEnd", CallingConvention = CallingConvention.Cdecl)]
    public static extern void PositionBuilderAtEnd(IntPtr Builder, IntPtr Block);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetInsertBlock", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetInsertBlock(IntPtr Builder);

    [DllImport(LLVMCore, EntryPoint = "LLVMClearInsertionPosition", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ClearInsertionPosition(IntPtr Builder);

    [DllImport(LLVMCore, EntryPoint = "LLVMInsertIntoBuilder", CallingConvention = CallingConvention.Cdecl)]
    public static extern void InsertIntoBuilder(IntPtr Builder, IntPtr Instr);

    [DllImport(LLVMCore, EntryPoint = "LLVMInsertIntoBuilderWithName", CallingConvention = CallingConvention.Cdecl)]
    public static extern void InsertIntoBuilderWithName(IntPtr Builder, IntPtr Instr, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMDisposeBuilder", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisposeBuilder(IntPtr Builder);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCurrentDebugLocation2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetCurrentDebugLocation2(IntPtr Builder);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetCurrentDebugLocation2", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetCurrentDebugLocation2(IntPtr Builder, IntPtr Loc);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetInstDebugLocation", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetInstDebugLocation(IntPtr Builder, IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddMetadataToInst", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddMetadataToInst(IntPtr Builder, IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuilderGetDefaultFPMathTag", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuilderGetDefaultFPMathTag(IntPtr Builder);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuilderSetDefaultFPMathTag", CallingConvention = CallingConvention.Cdecl)]
    public static extern void BuilderSetDefaultFPMathTag(IntPtr Builder, IntPtr FPMathTag);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetBuilderContext", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetBuilderContext(IntPtr Builder);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetCurrentDebugLocation", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetCurrentDebugLocation(IntPtr Builder, IntPtr L);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCurrentDebugLocation", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetCurrentDebugLocation(IntPtr Builder);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildRetVoid", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildRetVoid(IntPtr Param0);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildRet", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildRet(IntPtr Param0, IntPtr V);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildAggregateRet", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildAggregateRet(IntPtr Param0, IntPtr RetVals, uint N);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildBr", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildBr(IntPtr Param0, IntPtr Dest);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildCondBr", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildCondBr(IntPtr Param0, IntPtr If, IntPtr Then, IntPtr Else);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildSwitch", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildSwitch(IntPtr Param0, IntPtr V, IntPtr Else, uint NumCases);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildIndirectBr", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildIndirectBr(IntPtr B, IntPtr Addr, uint NumDests);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildCallBr", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildCallBr(IntPtr B, IntPtr Ty, IntPtr Fn, IntPtr DefaultDest, IntPtr IndirectDests, uint NumIndirectDests, IntPtr Args, uint NumArgs, IntPtr Bundles, uint NumBundles, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildInvoke2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildInvoke2(IntPtr Param0, IntPtr Ty, IntPtr Fn, IntPtr Args, uint NumArgs, IntPtr Then, IntPtr Catch, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildInvokeWithOperandBundles", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildInvokeWithOperandBundles(IntPtr Param0, IntPtr Ty, IntPtr Fn, IntPtr Args, uint NumArgs, IntPtr Then, IntPtr Catch, IntPtr Bundles, uint NumBundles, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildUnreachable", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildUnreachable(IntPtr Param0);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildResume", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildResume(IntPtr B, IntPtr Exn);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildLandingPad", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildLandingPad(IntPtr B, IntPtr Ty, IntPtr PersFn, uint NumClauses, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildCleanupRet", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildCleanupRet(IntPtr B, IntPtr CatchPad, IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildCatchRet", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildCatchRet(IntPtr B, IntPtr CatchPad, IntPtr BB);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildCatchPad", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildCatchPad(IntPtr B, IntPtr ParentPad, IntPtr Args, uint NumArgs, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildCleanupPad", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildCleanupPad(IntPtr B, IntPtr ParentPad, IntPtr Args, uint NumArgs, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildCatchSwitch", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildCatchSwitch(IntPtr B, IntPtr ParentPad, IntPtr UnwindBB, uint NumHandlers, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddCase", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddCase(IntPtr Switch, IntPtr OnVal, IntPtr Dest);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddDestination", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddDestination(IntPtr IndirectBr, IntPtr Dest);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNumClauses", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetNumClauses(IntPtr LandingPad);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetClause", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetClause(IntPtr LandingPad, uint Idx);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddClause", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddClause(IntPtr LandingPad, IntPtr ClauseVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsCleanup", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsCleanup(IntPtr LandingPad);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetCleanup", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetCleanup(IntPtr LandingPad, LLVMBool Val);

    [DllImport(LLVMCore, EntryPoint = "LLVMAddHandler", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddHandler(IntPtr CatchSwitch, IntPtr Dest);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNumHandlers", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetNumHandlers(IntPtr CatchSwitch);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetHandlers", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GetHandlers(IntPtr CatchSwitch, IntPtr Handlers);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetArgOperand", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetArgOperand(IntPtr Funclet, uint i);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetArgOperand", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetArgOperand(IntPtr Funclet, uint i, IntPtr value);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetParentCatchSwitch", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetParentCatchSwitch(IntPtr CatchPad);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetParentCatchSwitch", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetParentCatchSwitch(IntPtr CatchPad, IntPtr CatchSwitch);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildAdd", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildAdd(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildNSWAdd", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildNSWAdd(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildNUWAdd", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildNUWAdd(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFAdd", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFAdd(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildSub", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildSub(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildNSWSub", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildNSWSub(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildNUWSub", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildNUWSub(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFSub", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFSub(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildMul", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildMul(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildNSWMul", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildNSWMul(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildNUWMul", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildNUWMul(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFMul", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFMul(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildUDiv", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildUDiv(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildExactUDiv", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildExactUDiv(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildSDiv", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildSDiv(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildExactSDiv", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildExactSDiv(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFDiv", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFDiv(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildURem", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildURem(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildSRem", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildSRem(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFRem", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFRem(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildShl", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildShl(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildLShr", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildLShr(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildAShr", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildAShr(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildAnd", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildAnd(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildOr", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildOr(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildXor", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildXor(IntPtr Param0, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildBinOp", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildBinOp(IntPtr B, LLVMOpcode Op, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildNeg", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildNeg(IntPtr Param0, IntPtr V, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildNSWNeg", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildNSWNeg(IntPtr B, IntPtr V, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildNUWNeg", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildNUWNeg(IntPtr B, IntPtr V, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFNeg", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFNeg(IntPtr Param0, IntPtr V, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildNot", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildNot(IntPtr Param0, IntPtr V, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNUW", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool GetNUW(IntPtr ArithInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetNUW", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetNUW(IntPtr ArithInst, LLVMBool HasNUW);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNSW", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool GetNSW(IntPtr ArithInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetNSW", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetNSW(IntPtr ArithInst, LLVMBool HasNSW);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetExact", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool GetExact(IntPtr DivOrShrInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetExact", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetExact(IntPtr DivOrShrInst, LLVMBool IsExact);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNNeg", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool GetNNeg(IntPtr NonNegInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetNNeg", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetNNeg(IntPtr NonNegInst, LLVMBool IsNonNeg);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetFastMathFlags", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetFastMathFlags(IntPtr FPMathInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetFastMathFlags", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetFastMathFlags(IntPtr FPMathInst, uint FMF);

    [DllImport(LLVMCore, EntryPoint = "LLVMCanValueUseFastMathFlags", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool CanValueUseFastMathFlags(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetIsDisjoint", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool GetIsDisjoint(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetIsDisjoint", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetIsDisjoint(IntPtr Inst, LLVMBool IsDisjoint);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildMalloc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildMalloc(IntPtr Param0, IntPtr Ty, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildArrayMalloc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildArrayMalloc(IntPtr Param0, IntPtr Ty, IntPtr Val, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildMemSet", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildMemSet(IntPtr B, IntPtr Ptr, IntPtr Val, IntPtr Len, uint Align);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildMemCpy", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildMemCpy(IntPtr B, IntPtr Dst, uint DstAlign, IntPtr Src, uint SrcAlign, IntPtr Size);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildMemMove", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildMemMove(IntPtr B, IntPtr Dst, uint DstAlign, IntPtr Src, uint SrcAlign, IntPtr Size);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildAlloca", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildAlloca(IntPtr Param0, IntPtr Ty, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildArrayAlloca", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildArrayAlloca(IntPtr Param0, IntPtr Ty, IntPtr Val, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFree", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFree(IntPtr Param0, IntPtr PointerVal);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildLoad2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildLoad2(IntPtr Param0, IntPtr Ty, IntPtr PointerVal, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildStore", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildStore(IntPtr Param0, IntPtr Val, IntPtr Ptr);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildGEP2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildGEP2(IntPtr B, IntPtr Ty, IntPtr Pointer, IntPtr Indices, uint NumIndices, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildInBoundsGEP2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildInBoundsGEP2(IntPtr B, IntPtr Ty, IntPtr Pointer, IntPtr Indices, uint NumIndices, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildGEPWithNoWrapFlags", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildGEPWithNoWrapFlags(IntPtr B, IntPtr Ty, IntPtr Pointer, IntPtr Indices, uint NumIndices, [MarshalAs(UnmanagedType.LPStr)] string Name, uint NoWrapFlags);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildStructGEP2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildStructGEP2(IntPtr B, IntPtr Ty, IntPtr Pointer, uint Idx, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildGlobalString", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildGlobalString(IntPtr B, [MarshalAs(UnmanagedType.LPStr)] string Str, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildGlobalStringPtr", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildGlobalStringPtr(IntPtr B, [MarshalAs(UnmanagedType.LPStr)] string Str, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetVolatile", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool GetVolatile(IntPtr MemoryAccessInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetVolatile", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetVolatile(IntPtr MemoryAccessInst, LLVMBool IsVolatile);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetWeak", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool GetWeak(IntPtr CmpXchgInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetWeak", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetWeak(IntPtr CmpXchgInst, LLVMBool IsWeak);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetOrdering", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMAtomicOrdering GetOrdering(IntPtr MemoryAccessInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetOrdering", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetOrdering(IntPtr MemoryAccessInst, LLVMAtomicOrdering Ordering);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetAtomicRMWBinOp", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMAtomicRMWBinOp GetAtomicRMWBinOp(IntPtr AtomicRMWInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetAtomicRMWBinOp", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetAtomicRMWBinOp(IntPtr AtomicRMWInst, LLVMAtomicRMWBinOp BinOp);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildTrunc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildTrunc(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildZExt", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildZExt(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildSExt", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildSExt(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFPToUI", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFPToUI(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFPToSI", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFPToSI(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildUIToFP", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildUIToFP(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildSIToFP", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildSIToFP(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFPTrunc", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFPTrunc(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFPExt", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFPExt(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildPtrToInt", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildPtrToInt(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildIntToPtr", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildIntToPtr(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildBitCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildBitCast(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildAddrSpaceCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildAddrSpaceCast(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildZExtOrBitCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildZExtOrBitCast(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildSExtOrBitCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildSExtOrBitCast(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildTruncOrBitCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildTruncOrBitCast(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildCast(IntPtr B, LLVMOpcode Op, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildPointerCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildPointerCast(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildIntCast2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildIntCast2(IntPtr Param0, IntPtr Val, IntPtr DestTy, LLVMBool IsSigned, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFPCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFPCast(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildIntCast", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildIntCast(IntPtr Param0, IntPtr Val, IntPtr DestTy, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCastOpcode", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMOpcode GetCastOpcode(IntPtr Src, LLVMBool SrcIsSigned, IntPtr DestTy, LLVMBool DestIsSigned);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildICmp", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildICmp(IntPtr Param0, LLVMIntPredicate Op, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFCmp", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFCmp(IntPtr Param0, LLVMRealPredicate Op, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildPhi", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildPhi(IntPtr Param0, IntPtr Ty, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildCall2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildCall2(IntPtr Param0, IntPtr Param1, IntPtr Fn, IntPtr Args, uint NumArgs, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildCallWithOperandBundles", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildCallWithOperandBundles(IntPtr Param0, IntPtr Param1, IntPtr Fn, IntPtr Args, uint NumArgs, IntPtr Bundles, uint NumBundles, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildSelect", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildSelect(IntPtr Param0, IntPtr If, IntPtr Then, IntPtr Else, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildVAArg", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildVAArg(IntPtr Param0, IntPtr List, IntPtr Ty, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildExtractElement", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildExtractElement(IntPtr Param0, IntPtr VecVal, IntPtr Index, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildInsertElement", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildInsertElement(IntPtr Param0, IntPtr VecVal, IntPtr EltVal, IntPtr Index, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildShuffleVector", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildShuffleVector(IntPtr Param0, IntPtr V1, IntPtr V2, IntPtr Mask, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildExtractValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildExtractValue(IntPtr Param0, IntPtr AggVal, uint Index, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildInsertValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildInsertValue(IntPtr Param0, IntPtr AggVal, IntPtr EltVal, uint Index, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFreeze", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFreeze(IntPtr Param0, IntPtr Val, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildIsNull", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildIsNull(IntPtr Param0, IntPtr Val, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildIsNotNull", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildIsNotNull(IntPtr Param0, IntPtr Val, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildPtrDiff2", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildPtrDiff2(IntPtr Param0, IntPtr ElemTy, IntPtr LHS, IntPtr RHS, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFence", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFence(IntPtr B, LLVMAtomicOrdering ordering, LLVMBool singleThread, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildFenceSyncScope", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildFenceSyncScope(IntPtr B, LLVMAtomicOrdering ordering, uint SSID, [MarshalAs(UnmanagedType.LPStr)] string Name);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildAtomicRMW", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildAtomicRMW(IntPtr B, LLVMAtomicRMWBinOp op, IntPtr PTR, IntPtr Val, LLVMAtomicOrdering ordering, LLVMBool singleThread);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildAtomicRMWSyncScope", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildAtomicRMWSyncScope(IntPtr B, LLVMAtomicRMWBinOp op, IntPtr PTR, IntPtr Val, LLVMAtomicOrdering ordering, uint SSID);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildAtomicCmpXchg", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildAtomicCmpXchg(IntPtr B, IntPtr Ptr, IntPtr Cmp, IntPtr New, LLVMAtomicOrdering SuccessOrdering, LLVMAtomicOrdering FailureOrdering, LLVMBool SingleThread);

    [DllImport(LLVMCore, EntryPoint = "LLVMBuildAtomicCmpXchgSyncScope", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr BuildAtomicCmpXchgSyncScope(IntPtr B, IntPtr Ptr, IntPtr Cmp, IntPtr New, LLVMAtomicOrdering SuccessOrdering, LLVMAtomicOrdering FailureOrdering, uint SSID);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetNumMaskElements", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetNumMaskElements(IntPtr ShuffleVectorInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetUndefMaskElem", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetUndefMaskElem();

    [DllImport(LLVMCore, EntryPoint = "LLVMGetMaskValue", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetMaskValue(IntPtr ShuffleVectorInst, uint Elt);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAtomicSingleThread", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsAtomicSingleThread(IntPtr AtomicInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetAtomicSingleThread", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetAtomicSingleThread(IntPtr AtomicInst, LLVMBool SingleThread);

    [DllImport(LLVMCore, EntryPoint = "LLVMIsAtomic", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsAtomic(IntPtr Inst);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetAtomicSyncScopeID", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint GetAtomicSyncScopeID(IntPtr AtomicInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetAtomicSyncScopeID", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetAtomicSyncScopeID(IntPtr AtomicInst, uint SSID);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCmpXchgSuccessOrdering", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMAtomicOrdering GetCmpXchgSuccessOrdering(IntPtr CmpXchgInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetCmpXchgSuccessOrdering", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetCmpXchgSuccessOrdering(IntPtr CmpXchgInst, LLVMAtomicOrdering Ordering);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetCmpXchgFailureOrdering", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMAtomicOrdering GetCmpXchgFailureOrdering(IntPtr CmpXchgInst);

    [DllImport(LLVMCore, EntryPoint = "LLVMSetCmpXchgFailureOrdering", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetCmpXchgFailureOrdering(IntPtr CmpXchgInst, LLVMAtomicOrdering Ordering);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateModuleProviderForExistingModule", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateModuleProviderForExistingModule(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMDisposeModuleProvider", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisposeModuleProvider(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateMemoryBufferWithContentsOfFile", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool CreateMemoryBufferWithContentsOfFile([MarshalAs(UnmanagedType.LPStr)] string Path, IntPtr OutMemBuf, IntPtr OutMessage);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateMemoryBufferWithSTDIN", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool CreateMemoryBufferWithSTDIN(IntPtr OutMemBuf, IntPtr OutMessage);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateMemoryBufferWithMemoryRange", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateMemoryBufferWithMemoryRange([MarshalAs(UnmanagedType.LPStr)] string InputData, ulong InputDataLength, [MarshalAs(UnmanagedType.LPStr)] string BufferName, LLVMBool RequiresNullTerminator);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateMemoryBufferWithMemoryRangeCopy", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateMemoryBufferWithMemoryRangeCopy([MarshalAs(UnmanagedType.LPStr)] string InputData, ulong InputDataLength, [MarshalAs(UnmanagedType.LPStr)] string BufferName);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetBufferStart", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public static extern string GetBufferStart(IntPtr MemBuf);

    [DllImport(LLVMCore, EntryPoint = "LLVMGetBufferSize", CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong GetBufferSize(IntPtr MemBuf);

    [DllImport(LLVMCore, EntryPoint = "LLVMDisposeMemoryBuffer", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisposeMemoryBuffer(IntPtr MemBuf);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreatePassManager", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreatePassManager();

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateFunctionPassManagerForModule", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateFunctionPassManagerForModule(IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMCreateFunctionPassManager", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr CreateFunctionPassManager(IntPtr MP);

    [DllImport(LLVMCore, EntryPoint = "LLVMRunPassManager", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool RunPassManager(IntPtr PM, IntPtr M);

    [DllImport(LLVMCore, EntryPoint = "LLVMInitializeFunctionPassManager", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool InitializeFunctionPassManager(IntPtr FPM);

    [DllImport(LLVMCore, EntryPoint = "LLVMRunFunctionPassManager", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool RunFunctionPassManager(IntPtr FPM, IntPtr F);

    [DllImport(LLVMCore, EntryPoint = "LLVMFinalizeFunctionPassManager", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool FinalizeFunctionPassManager(IntPtr FPM);

    [DllImport(LLVMCore, EntryPoint = "LLVMDisposePassManager", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DisposePassManager(IntPtr PM);

    [DllImport(LLVMCore, EntryPoint = "LLVMStartMultithreaded", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool StartMultithreaded();

    [DllImport(LLVMCore, EntryPoint = "LLVMStopMultithreaded", CallingConvention = CallingConvention.Cdecl)]
    public static extern void StopMultithreaded();

    [DllImport(LLVMCore, EntryPoint = "LLVMIsMultithreaded", CallingConvention = CallingConvention.Cdecl)]
    public static extern LLVMBool IsMultithreaded();
}
