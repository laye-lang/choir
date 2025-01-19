﻿using System.Diagnostics;
using System.Text;

using Choir.CommandLine;
using Choir.Front.Laye.Sema;

using LLVMSharp;
using LLVMSharp.Interop;

namespace Choir.Front.Laye.Codegen;

internal enum ValueClass
{
    NoClass,
    Integer,
    Sse,
    SseUp,
    X87,
    X87Up,
    ComplexX87,
    // NOTE(local): might need to add a separate memory class depending on how the code
    // generator is able to handle Laye's const ref semantics
    Memory,
}

internal enum ParameterSemantics : byte
{
    Value,
    Reference,
    ConstReference,
}

public sealed class LayeCodegen(LayeModule module, LLVMModuleRef llvmModule)
{
    private record class ParameterInfo
    {
        public LLVMValueRef Value { get; set; }
        public (ValueClass Lo, ValueClass Hi) Class { get; set; }
        public ParameterSemantics Semantics { get; set; }
        public int BeginIndex { get; set; }
    }

    private record class FunctionInfo
    {
        public ParameterInfo? SretParameter { get; set; }
        public bool IsSret => SretParameter is not null;

        public ParameterInfo[] ParameterInfos { get; set; } = [];

        public FunctionInfo()
        {
        }
    }

    public static LLVMModuleRef GenerateIR(LayeModule module)
    {
        var llvmContext = LLVMContextRef.Create();
        var llvmModule = llvmContext.CreateModuleWithName(module.ModuleName);

        var cg = new LayeCodegen(module, llvmModule);

        void GenerateDeclarations(IEnumerable<SemaDeclNamed> decls)
        {
            //foreach (var decl in decls)
            //{
            //    if (decl is SemaDeclStruct @struct)
            //        cg.GenerateStructDeclaration(@struct);
            //}

            foreach (var decl in decls)
            {
                if (decl is SemaDeclFunction function)
                    cg.GenerateFunctionDeclaration(function);
            }
        }

        void GenerateDefinitions(IEnumerable<SemaDeclNamed> decls)
        {
            //foreach (var decl in module.Declarations)
            //{
            //    if (decl is SemaDeclStruct @struct)
            //        cg.GenerateStructDefinition(@struct);
            //}

            foreach (var decl in module.Declarations)
            {
                if (decl is SemaDeclFunction function)
                {
                    if (function.Body is not null)
                        cg.GenerateFunctionDefinition(function);
                }
            }
        }

        foreach (var dependency in module.Dependencies)
            GenerateDeclarations(dependency.ExportedDeclarations);

        GenerateDeclarations(module.Declarations);
        GenerateDefinitions(module.Declarations);

        byte[] moduleData = module.Serialize();
        string sectionName = LayeConstants.GetModuleDescriptionSectionName(module.ModuleName);
        llvmModule.EmbedBuffer(moduleData, sectionName);

        return llvmModule;
    }

    public ChoirContext Context { get; } = module.Context;
    public LayeModule Module { get; } = module;
    public LLVMContextRef LlvmContext { get; } = llvmModule.Context;
    public LLVMModuleRef LlvmModule { get; } = llvmModule;
    public LayeNameMangler Mangler { get; } = new(module.Context, module);
    public Colors Colors { get; } = new(module.Context.UseColor);

    private readonly Dictionary<SemaDeclNamed, LLVMValueRef> _declaredValues = [];
    private readonly Dictionary<SemaDeclNamed, LLVMTypeRef> _declaredTypes = [];
    private readonly Dictionary<SemaDeclFunction, FunctionInfo> _functionInfos = [];

    private LLVMValueRef _assertHandlerFunction = default;

    private SemaDeclFunction? CurrentFunction { get; set; }
    private LLVMValueRef CurrentFunctionValue => CurrentFunction is null ? default : _declaredValues[CurrentFunction];

    private int _nameCounter = 0;
    private string NextName(string name = "") => $"{name}{_nameCounter++}";

    private ValueClass Merge(ValueClass accum, ValueClass field)
    {
        if (accum == field || field == ValueClass.NoClass)
            return accum;
        else if (field == ValueClass.Memory)
            return ValueClass.Memory;
        else if (accum == ValueClass.NoClass)
            return field;
        else if (accum == ValueClass.Integer || field == ValueClass.Integer)
            return ValueClass.Integer;
        // NOTE(local): not handling X87 et al
        else return ValueClass.Sse;
    }

    private void PostMerge(int bitWidth, ref ValueClass lo, ref ValueClass hi)
    {
        Context.Assert(bitWidth >= 0, "bit width cannot be less than 0");
        
        if (hi == ValueClass.Memory)
            lo = ValueClass.Memory;

        if (hi == ValueClass.X87Up && lo != ValueClass.X87)
            lo = ValueClass.Memory;

        if (bitWidth > 128 && (lo != ValueClass.Sse && hi != ValueClass.SseUp))
            lo = ValueClass.Memory;

        if (hi == ValueClass.SseUp && lo != ValueClass.Sse)
            lo = ValueClass.Sse;
    }

    private (ValueClass Lo, ValueClass Hi) Classify(SemaTypeQual typeQual, int offsetBase = 0) => Classify(typeQual.Type, offsetBase);
    private (ValueClass Lo, ValueClass Hi) Classify(SemaType type, int offsetBase = 0)
    {
        ValueClass lo = ValueClass.NoClass, hi = ValueClass.NoClass;
        Classify(type, offsetBase, ref lo, ref hi);
        return (lo, hi);
    }

    private void Classify(SemaTypeQual typeQual, int offsetBase, ref ValueClass lo, ref ValueClass hi) => Classify(typeQual.Type, offsetBase, ref lo, ref hi);
    private void Classify(SemaType type, int offsetBase, ref ValueClass lo, ref ValueClass hi)
    {
        type = type.CanonicalType;

        int bitWidth = type.Size.Bits;
        lo = ValueClass.NoClass;
        hi = ValueClass.NoClass;

        if (offsetBase < 64)
            ClassifyInternal(ref lo, ref hi, ref lo);
        else ClassifyInternal(ref lo, ref hi, ref hi);

        void ClassifyInternal(ref ValueClass lo, ref ValueClass hi, ref ValueClass current)
        {
            if (type.IsVoid || type.IsNoReturn)
                current = ValueClass.NoClass;
            else if (type.IsInteger || type.IsBool)
            {
                if (bitWidth <= 64)
                    current = ValueClass.Integer;
                else
                {
                    lo = ValueClass.Integer;
                    hi = ValueClass.Integer;
                }
                // else pass in memory
            }
            else if (type.IsFloat)
            {
                if (bitWidth is 16 or 32 or 64)
                    current = ValueClass.Sse;
                else if (bitWidth == 128)
                {
                    lo = ValueClass.Sse;
                    hi = ValueClass.SseUp;
                }
                else
                {
                    Context.Assert(bitWidth == 80, "unsupported float bit width");
                    lo = ValueClass.X87;
                    hi = ValueClass.X87Up;
                }
            }
            else if (type is SemaTypePointer or SemaTypeBuffer)
                current = ValueClass.Integer;
            else if (type is SemaTypeSlice)
            {
                lo = ValueClass.Integer;
                hi = ValueClass.Integer;
            }
            else if (type is SemaTypeStruct typeStruct)
            {
                // a struct that is more than eight eightbytes just goes in MEMORY
                if (bitWidth >= 8 * 64)
                    return;

                // We need to break up structs into "eightbytes".
                current = ValueClass.NoClass;

                var fieldDecls = typeStruct.DeclStruct.FieldDecls;
                for (int i = 0, currentOffset = offsetBase; i < fieldDecls.Count; i++)
                {
                    var field = fieldDecls[i];
                    var fieldType = field.FieldType;

                    int fieldOffset = Align.AlignTo(currentOffset, fieldType.Align.Bytes);
                    currentOffset = fieldOffset + fieldType.Size.Bits;

                    // TODO(local): any place here where bitfields can go? not sure, might need special C handling for stuff like that.
                    if (0 != (fieldOffset % fieldType.Align.Bits))
                    {
                        lo = ValueClass.Memory;
                        PostMerge(bitWidth, ref lo, ref hi);
                        return;
                    }

                    var (fieldLo, fieldHi) = Classify(fieldType, fieldOffset);

                    lo = Merge(lo, fieldLo);
                    hi = Merge(hi, fieldHi);

                    if (lo == ValueClass.Memory || hi == ValueClass.Memory)
                        break;
                }

                PostMerge(bitWidth, ref lo, ref hi);
            }
            else
            {
                Context.Todo($"Classify {type.ToDebugString(Colors)} ({type.GetType().FullName})");
            }
        }
    }

    private LLVMTypeRef GenerateType(SemaTypeQual typeQual) => GenerateType(typeQual.Type);
    private LLVMTypeRef GenerateType(SemaType type)
    {
        type = type.CanonicalType;
        switch (type)
        {
            default:
            {
                Context.Diag.ICE($"Unimplemented Laye type in Choir codegen: {type.GetType().FullName}");
                throw new UnreachableException();
            }

            case SemaTypeNil:
            {
                Context.Diag.ICE("The 'nil' type should not make it to code generation, it should always be converted to a proper type.");
                throw new UnreachableException();
            }

            case SemaTypeBuiltIn builtIn:
            {
                switch (builtIn.Kind)
                {
                    default:
                    {
                        Context.Diag.ICE($"Unimplemented Laye built in type kind in Choir codegen: {builtIn.Kind}");
                        throw new UnreachableException();
                    }

                    case BuiltinTypeKind.NoReturn:
                    case BuiltinTypeKind.Void: return LLVMTypeRef.Void;
                    case BuiltinTypeKind.Bool: return LLVMTypeRef.Int8;
                    case BuiltinTypeKind.BoolSized: return LLVMTypeRef.CreateInt((uint)builtIn.Size.Bits);
                    case BuiltinTypeKind.Int: return LLVMTypeRef.CreateInt((uint)Context.Target.SizeOfSizeType.Bits);
                    case BuiltinTypeKind.IntSized: return LLVMTypeRef.CreateInt((uint)builtIn.Size.Bits);
                    case BuiltinTypeKind.FloatSized:
                    {
                        switch (builtIn.Size.Bits)
                        {
                            default:
                            {
                                Context.Diag.ICE($"Unimplemented Laye float size in Choir codegen: {builtIn.ToDebugString(Colors)}");
                                throw new UnreachableException();
                            }

                            case 32: return LLVMTypeRef.Float;
                            case 64: return LLVMTypeRef.Double;
                            case 80: return LLVMTypeRef.X86FP80;
                            case 128: return LLVMTypeRef.FP128;
                        }
                    }

                    case BuiltinTypeKind.FFIInt: return LLVMTypeRef.CreateInt((uint)builtIn.Size.Bits);
                }
            }

            //case SemaTypeAlias typeAlias: return GenerateType(typeAlias.CanonicalType);
            case SemaTypeStruct typeStruct:
            {
                return LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)type.Size.Bytes);
            }

            case SemaTypeArray typeArray:
            {
                var elementType = GenerateType(typeArray.ElementType);
                long flatLength = typeArray.FlatLength;
                Context.Assert(flatLength <= uint.MaxValue, $"The length of an array exceeded LLVM's max array length. ({flatLength} > {uint.MaxValue})");
                return LLVMTypeRef.CreateArray(elementType, (uint)flatLength);
            }

            case SemaTypePointer: return LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
            case SemaTypeBuffer: return LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
        }
    }

    private void GenerateFunctionDeclaration(SemaDeclFunction function)
    {
        var cc = function.CallingConvention;
        Context.Assert(cc is CallingConvention.CDecl or CallingConvention.Laye, $"Unsupported calling convention {cc} in codegen.");

        string functionName = Mangler.GetMangledName(function);

        var existingFunction = LlvmModule.GetNamedFunction(functionName);
        if (existingFunction.Handle != IntPtr.Zero)
        {
            // TODO(local): assert some invariants to make sure sema did the right thing
            _declaredValues[function] = existingFunction;
            return;
        }

        var returnClass = Classify(function.ReturnType.Type);
        var functionInfo = _functionInfos[function] = new();

        var parameterTypes = new List<LLVMTypeRef>(function.ParameterDecls.Count * 2 + 1);
        if (returnClass.Lo == ValueClass.Memory)
        {
            functionInfo.SretParameter = new()
            {
                Class = returnClass,
                Semantics = ParameterSemantics.Reference,
            };

            var sretType = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
            parameterTypes.Add(sretType);
        }

        functionInfo.ParameterInfos = new ParameterInfo[function.ParameterDecls.Count];
        for (int i = 0; i < function.ParameterDecls.Count; i++)
        {
            var paramDecl = function.ParameterDecls[i];
            var paramInfo = functionInfo.ParameterInfos[i] = new()
            {
                Class = Classify(paramDecl.ParamType),
            };

            if (paramInfo.Class.Lo == ValueClass.Memory)
            {
                if (cc == CallingConvention.Laye && !paramDecl.ParamType.IsMutable)
                    paramInfo.Semantics = ParameterSemantics.ConstReference;
                else if (Context.Abi.PassMemoryValuesByAddress)
                    paramInfo.Semantics = ParameterSemantics.Reference;
            }

            LLVMTypeRef paramType;
            if (paramInfo.Semantics is ParameterSemantics.Reference or ParameterSemantics.ConstReference)
                paramType = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
            else paramType = GenerateType(paramDecl.ParamType);
            parameterTypes.Add(paramType);
        }

        var returnType = functionInfo.IsSret ? LLVMTypeRef.Void : GenerateType(function.ReturnType);
        var functionType = LLVMTypeRef.CreateFunction(returnType, [.. parameterTypes]);

        var f = LlvmModule.AddFunction(functionName, functionType);
        _declaredValues[function] = f;

        if (functionInfo.IsSret)
        {
            f.AddSRetAttributeAtIndex(LlvmContext, (LLVMAttributeIndex)1, GenerateType(function.ReturnType));
            f.AddNoAliasAttributeAtIndex(LlvmContext, (LLVMAttributeIndex)1);
            f.AddWritableAttributeAtIndex(LlvmContext, (LLVMAttributeIndex)1);
            f.AddDeadOnUnwindAttributeAtIndex(LlvmContext, (LLVMAttributeIndex)1);

            var sretParam = f.GetParam(0);
            functionInfo.SretParameter!.Value = sretParam;
            sretParam.Name = "sret";
        }

        int paramStartIndex = functionInfo.IsSret ? 1 : 0;
        for (int i = 0; i < function.ParameterDecls.Count; i++)
        {
            var paramDecl = function.ParameterDecls[i];
            var paramValue = f.GetParam((uint)(i + paramStartIndex));
            functionInfo.ParameterInfos[i].Value = paramValue;
            paramValue.Name = paramDecl.Name;
        }
    }

    private void GenerateFunctionDefinition(SemaDeclFunction function)
    {
        Context.Assert(function.Body is not null, function.Location, "Attempt to generate code for a function definition when only a declaration is present.");
        Context.Assert(_declaredValues.ContainsKey(function), function.Location, "No LLVM function declaration was generated for this function.");

        CurrentFunction = function;

        var f = _declaredValues[function];
        var fInfo = _functionInfos[function];
        var startBlock = f.AppendBasicBlock("start");

        var builder = LlvmContext.CreateBuilder();
        EnterBlock(builder, startBlock);

        uint paramOffset = fInfo.IsSret ? 1u : 0;
        for (int i = 0; i < function.ParameterDecls.Count; i++)
        {
            var paramType = f.Params[i].TypeOf;
            var paramLocal = builder.BuildAlloca(paramType, "param");
            builder.BuildStore(f.GetParam((uint)i + paramOffset), paramLocal);
            _declaredValues[function.ParameterDecls[i]] = paramLocal;
        }

        BuildStmt(builder, function.Body);

        if (builder.InsertBlock.Terminator.Handle == IntPtr.Zero)
        {
            //Context.Assert(function.ReturnType.IsVoid, function.Location, "There was no explicit return in the final block of this function. Sema should have validated it could only return void.");
            if (function.ReturnType.IsVoid)
                builder.BuildRetVoid();
            else builder.BuildUnreachable();
        }
    }

    private LLVMBasicBlockRef EnterBlock(LLVMBuilderRef builder, LLVMBasicBlockRef bb)
    {
        Context.Assert(CurrentFunction is not null, "Not currently within a function definition.");
        Context.Assert(builder != default, CurrentFunction.Location, "Invalid builder.");
        Context.Assert(bb != default, CurrentFunction.Location, "Invalid basic block.");
        Context.Assert(bb.Parent != default, CurrentFunction!.Location, "Block is not inside a function.");
        Context.Assert(bb.Parent == CurrentFunctionValue, CurrentFunction!.Location, "Block is not inside this function.");

        if (builder.InsertBlock.Handle != IntPtr.Zero && builder.InsertBlock.Terminator.Handle == IntPtr.Zero && bb != CurrentFunctionValue.EntryBasicBlock)
        {
            builder.BuildBr(bb);
        }

        builder.PositionAtEnd(bb);
        return bb;
    }

    private LLVMValueRef GetAssertHandlerFunction()
    {
        if (_assertHandlerFunction.Handle != IntPtr.Zero)
            return _assertHandlerFunction;

        var ptrType = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
        var assertHandlerType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, [ptrType, ptrType, LLVMTypeRef.Int64], false);
        _assertHandlerFunction = LlvmModule.AddFunction("__laye_assert_handler", assertHandlerType);

        return _assertHandlerFunction;
    }

    private void GenerateDefers(LLVMBuilderRef builder, SemaDeferStackNode? current, SemaDeferStackNode? last)
    {
        while (current != last)
        {
            Context.Assert(current is not null, "Well, somehow current != last but current == null, so something did a fuckey.");
            BuildStmt(builder, current!.Defer.DeferredStatement);
            current = current.Previous;
        }
    }

    private void BuildStmt(LLVMBuilderRef builder, SemaStmt stmt)
    {
        switch (stmt)
        {
            default:
            {
                Context.Diag.ICE(stmt.Location, $"Unimplemented Laye node in Choir builder/codegen: {stmt.GetType().FullName}");
                throw new UnreachableException();
            }

            case SemaDeclBinding binding:
            {
                var type = GenerateType(binding.BindingType);
                var storage = builder.BuildAlloca(type, $"{binding.Name}.alloca");
                storage.SetAlignment((uint)binding.BindingType.Align.Bytes);
                _declaredValues[binding] = storage;

                builder.BuildMemSet(storage, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, 0), binding.BindingType.Size, binding.BindingType.Align);
                if (binding.InitialValue is not null)
                    BuildExprIntoMemory(builder, binding.InitialValue, storage);
            } break;

            case SemaStmtXyzzy:
            {
                // Nothing happens.
            } break;

            case SemaStmtDefer:
            {
                // Skip it, and let the defer generation handle it at the appropriate time
            } break;

            case SemaStmtCompound compound:
            {
                foreach (var child in compound.Statements)
                {
                    BuildStmt(builder, child);
                    if (child.ControlFlow != StmtControlFlow.Fallthrough)
                        break;
                }

                if (compound.ControlFlow == StmtControlFlow.Fallthrough)
                    GenerateDefers(builder, compound.EndDefer, compound.StartDefer);
            } break;

            case SemaStmtReturnVoid @return:
            {
                GenerateDefers(builder, @return.Defer, null);
                builder.BuildRetVoid();
            } break;

            case SemaStmtReturnValue @return:
            {
                var functionInfo = _functionInfos[CurrentFunction!];
                if (functionInfo.IsSret)
                {
                    var sretStorage = CurrentFunctionValue.GetParam(0);
                    Context.Assert(sretStorage.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind, "The first parameter of an SRet function must be a pointer, the address to store the return value in.");
                    BuildExprIntoMemory(builder, @return.Value, sretStorage);

                    GenerateDefers(builder, @return.Defer, null);
                    builder.BuildRetVoid();
                }
                else
                {
                    var value = BuildExpr(builder, @return.Value);

                    GenerateDefers(builder, @return.Defer, null);
                    builder.BuildRet(value);
                }
            } break;

            case SemaStmtUnreachable:
            {
                builder.BuildUnreachable();
            } break;

            case SemaStmtIf @if:
            {
                Context.Assert(CurrentFunction is not null, @if.Location, "`if` should be within a function.");
                Context.Assert(@if.Conditions.Count > 0, @if.Location, "`if` should have at least 1 condition.");

                var f = CurrentFunctionValue;
                string ifName = NextName("if");

                var conditionBlocks = new LLVMBasicBlockRef[@if.Conditions.Count];
                var bodyBlocks = new LLVMBasicBlockRef[@if.Conditions.Count + (@if.ElseBody is null ? 0 : 1)];

                for (int i = 0; i < @if.Conditions.Count; i++)
                {
                    conditionBlocks[i] = f.AppendBasicBlock($".{ifName}.cond{i}");
                    bodyBlocks[i] = f.AppendBasicBlock($".{ifName}.pass{i}");
                }

                if (@if.ElseBody is not null)
                    bodyBlocks[@if.Conditions.Count] = f.AppendBasicBlock($".{ifName}.fail");

                var joinBlock = @if.ControlFlow == StmtControlFlow.Fallthrough ? f.AppendBasicBlock($".{ifName}.join") : default;

                builder.BuildBr(conditionBlocks[0]);
                for (int i = 0; i < @if.Conditions.Count; i++)
                {
                    var conditionBlock = conditionBlocks[i];
                    var passBlock = bodyBlocks[i];
                    var failBlock = i < @if.Conditions.Count - 1 ? conditionBlocks[i + 1] : (@if.ElseBody is not null ? bodyBlocks[@if.Conditions.Count] : joinBlock);
                    Context.Assert(failBlock != default, @if.Location, "Invalid control flow semantics for if statement made it to code generation.");

                    EnterBlock(builder, conditionBlock);
                    var condition = BuildExpr(builder, @if.Conditions[i].Condition);

                    // ensure condition is *always* i1
                    condition = builder.BuildTrunc(condition, LLVMTypeRef.Int1, "conv2i1");
                    builder.BuildCondBr(condition, passBlock, failBlock);

                    EnterBlock(builder, passBlock);
                    BuildStmt(builder, @if.Conditions[i].Body);

                    if (builder.InsertBlock.Terminator.Handle != IntPtr.Zero && @if.Conditions[i].ControlFlow == StmtControlFlow.Fallthrough)
                    {
                        Context.Assert(joinBlock != default, @if.Conditions[i].Body.Location, "Invalid control flow semantics for if statement made it to code generation.");
                        builder.BuildBr(joinBlock);
                    }
                }

                if (@if.ElseBody is { } elseBody)
                {
                    EnterBlock(builder, bodyBlocks[@if.Conditions.Count]);
                    BuildStmt(builder, elseBody);

                    if (builder.InsertBlock.Terminator.Handle != IntPtr.Zero && elseBody.ControlFlow == StmtControlFlow.Fallthrough)
                    {
                        Context.Assert(joinBlock != default, elseBody.Location, "Invalid control flow semantics for if statement made it to code generation.");
                        builder.BuildBr(joinBlock);
                    }
                }

                if (joinBlock != default)
                    EnterBlock(builder, joinBlock);
            } break;

            case SemaStmtAssign assign:
            {
                if (assign.Target is SemaExprLookup { ValueCategory: ValueCategory.Register } register)
                {
                    Context.Assert(register.ReferencedEntity is SemaDeclRegister, register.Location, "Should refrence a register decl");
                    var value = BuildExpr(builder, assign.Value);
                    BuildStoreToRegister(builder, register.Location, (SemaDeclRegister)register.ReferencedEntity, value);
                    break;
                }

                var target = BuildExpr(builder, assign.Target);
                Context.Assert(target.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind, "Can't assign to non-pointer target.");
                BuildExprIntoMemory(builder, assign.Value, target);
            } break;

            case SemaStmtDiscard discard:
            {
                BuildExpr(builder, discard.Expr);
            } break;

            case SemaStmtAssert assert:
            {
                var f = CurrentFunctionValue;

                var failBlock = f.AppendBasicBlock("assert.fail");
                var continueBlock = f.AppendBasicBlock("assert.continue");

                var condition = BuildExpr(builder, assert.Condition);
                condition = builder.BuildTrunc(condition, LLVMTypeRef.Int1, "conv2i1");
                builder.BuildCondBr(condition, continueBlock, failBlock);

                EnterBlock(builder, failBlock);

                var source = Context.GetSourceFileById(assert.Location.FileId);
                var locInfo = assert.Location.SeekLineColumn(Context);

                var message = builder.BuildGlobalStringPtr(assert.FailureMessage, "assert.message");
                var fileName = builder.BuildGlobalStringPtr(source?.FilePath ?? "<no-file>", "assert.file_name");
                var lineNumber = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)(locInfo?.Line ?? 0));
                var assertHandler = GetAssertHandlerFunction();

                unsafe
                {
                    builder.BuildCall2(LLVM.GlobalGetValueType(assertHandler), assertHandler, [message, fileName, lineNumber]);
                }

                builder.BuildUnreachable();

                EnterBlock(builder, continueBlock);
            } break;

            case SemaStmtExpr expr:
            {
                BuildExpr(builder, expr.Expr);
            } break;
        }
    }

    private void BuildExprIntoMemory(LLVMBuilderRef builder, SemaExpr expr, LLVMValueRef tempAddr)
    {
        unsafe
        {
            switch (expr)
            {
                case SemaExprGrouped grouped: BuildExprIntoMemory(builder, grouped.Inner, tempAddr); break;

                case SemaExprConstructor ctor:
                {
                    foreach (var init in ctor.Inits)
                    {
                        var storage = builder.BuildPtrAdd(tempAddr, init.Offset, "ctor.fieldinit");
                        BuildExprIntoMemory(builder, init.Value, storage);
                    }
                } break;

                case SemaExprCall call:
                {
                    Context.Assert(call.Callee.Type.CanonicalType.Type is SemaTypeFunction, "The call expression only works on functions");

                    switch (call.Callee)
                    {
                        default:
                        {
                            Context.Todo($"Handle calling (into memory) a callee of kind {call.Callee.GetType().FullName}");
                            throw new UnreachableException();
                        }

                        case SemaExprLookup { ReferencedEntity: SemaDeclFunction staticFunction }:
                        {
                            Context.Assert(staticFunction.VarargsKind != VarargsKind.Laye, call.Location, "Generating code for varargs Laye functions is not supported yet.");

                            var functionInfo = _functionInfos[staticFunction];
                            if (!functionInfo.IsSret)
                            {
                                LLVMValueRef returnValue = BuildExpr(builder, expr);
                                var store = builder.BuildStore(returnValue, tempAddr);
                                store.SetAlignment((uint)staticFunction.ReturnType.Align.Bytes);
                            }
                            else
                            {
#pragma warning disable IDE0028 // Simplify collection initialization
                                var arguments = new List<LLVMValueRef>(1 + (call.Arguments.Count * 2));
                                arguments.Add(tempAddr); // the sret address;
#pragma warning restore IDE0028 // Simplify collection initialization

                                for (int i = 0; i < staticFunction.ParameterDecls.Count; i++)
                                    BuildArgumentForFunctionCall(builder, arguments, staticFunction.ParameterDecls[i], functionInfo.ParameterInfos[i], call.Arguments[i]);

                                if (staticFunction.VarargsKind == VarargsKind.C)
                                {
                                    for (int i = staticFunction.ParameterDecls.Count; i < call.Arguments.Count; i++)
                                        BuildArgumentForFunctionCallAsCVarargs(builder, arguments, call.Arguments[i]);
                                }

                                // no need to BuildExpr when I can look it up manually right now
                                var callee = _declaredValues[staticFunction];
                                builder.BuildCall2(LLVM.GlobalGetValueType(callee), callee, arguments.ToArray(), "");
                            }
                        } break;
                    }
                } break;

                default:
                {
                    LLVMValueRef exprValue = BuildExpr(builder, expr);
                    var store = builder.BuildStore(exprValue, tempAddr);
                    store.SetAlignment((uint)expr.Type.Align.Bytes);
                } break;
            }
        }
    }

    private LLVMValueRef BuildExpr(LLVMBuilderRef builder, SemaExpr expr)
    {
        var exprType = expr.Type.Type.CanonicalType;

        unsafe
        {
            switch (expr)
            {
                default:
                {
                    Context.Diag.ICE(expr.Location, $"Unimplemented Laye node in Choir builder/codegen: {expr.GetType().FullName}");
                    throw new UnreachableException();
                }

                case SemaExprGrouped grouped: return BuildExpr(builder, grouped.Inner);

                case SemaExprConstructor ctor:
                {
                    Context.Diag.ICE(ctor.Location, $"Should not encounter a constructor here; the code generator needs to instantiate a temporary and call {nameof(BuildExprIntoMemory)} instead.");
                    throw new UnreachableException();
                }

                case SemaExprLiteralBool literalBool:
                {
                    var type = GenerateType(expr.Type);
                    return LLVMValueRef.CreateConstInt(type, literalBool.LiteralValue ? 1ul : 0ul, false);
                }

                case SemaExprLiteralNil literalNil when exprType is SemaTypePointer or SemaTypeBuffer:
                    return LLVMValueRef.CreateConstPointerNull(LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));

                case SemaExprLiteralNil literalNil:
                {
                    Context.Diag.ICE(literalNil.Location, $"Unimplemented type for literal 'nil' value: ${expr.Type.ToDebugString(Colors)} (canonical: {exprType.ToDebugString(Colors)})");
                    throw new UnreachableException();
                }

                case SemaExprEvaluatedConstant constant:
                {
                    var type = GenerateType(constant.Type);
                    switch (constant.Value.Kind)
                    {
                        default:
                        {
                            Context.Diag.ICE(expr.Location, $"Unimplemented Laye constant kind in Choir builder/codegen: {constant.Value.Kind}");
                            throw new UnreachableException();
                        }

                        case EvaluatedConstantKind.Integer: return LLVMValueRef.CreateConstInt(type, (ulong)(long)constant.Value.IntegerValue, true);
                        case EvaluatedConstantKind.String:
                        {
                            if (expr.Type.Type == Context.Types.LayeTypeI8Buffer)
                                return builder.BuildGlobalStringPtr(constant.Value.StringValue, "str");
                            else
                            {
                                Context.Assert(false, $"Unhandled type for string literal {expr.Type.ToDebugString(Colors)} in LLVM code generator.");
                                throw new UnreachableException();
                            }
                        }
                    }
                }

                case SemaExprFieldStructIndex structField:
                {
                    var lvalue = BuildExpr(builder, structField.Operand);
                    Context.Assert(lvalue.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind, "Struct field lookup requires an lvalue, which will be of type `ptr`.");

                    var ptradd = builder.BuildPtrAdd(lvalue, structField.FieldOffset, "field.ptradd");
                    ptradd.Alignment = (uint)structField.Type.Align.Bytes;
                    return ptradd;
                }

                case SemaExprIndexArray arrayIndex:
                {
                    var lvalue = BuildExpr(builder, arrayIndex.Operand);
                    Context.Assert(lvalue.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind, "Array index lookup requires an lvalue, which will be of type `ptr`.");

                    /*

                    int[2, 3, 4] foo;
                    foo[1, 1, 1] = 69;

                    can be either A:

                    foo_flat[1 + (1 * 3) + (1 * 3 * 4)] = 69;

                    or B:

                    foo_flat[(1 * 2) + (1 * 3) + 1] = 69;

                    I think...

                    */

                    var typeInt = GenerateType(Context.Types.LayeTypeInt);
                    Context.Assert(arrayIndex.Operand.Type.CanonicalType.Type is SemaTypeArray, $"An array index expression should have an array type operator, but got {arrayIndex.Operand.Type.ToDebugString(Colors)}.");
                    var typeArray = (SemaTypeArray)arrayIndex.Operand.Type.CanonicalType.Type;
                    var strides = new LLVMValueRef[typeArray.Arity];

                    for (int i = 0; i < strides.Length; i++)
                    {
                        if (i == 0)
                            strides[i] = LLVMValueRef.CreateConstInt(typeInt, 1);
                        else
                        {
                            long strideAgg = typeArray.Lengths.Take(i).Aggregate(1L, (agg, l) => agg * l);
                            strides[i] = LLVMValueRef.CreateConstInt(typeInt, (ulong)strideAgg);
                        }
                    }

                    var totalOffset = BuildExpr(builder, arrayIndex.Indices[0]);
                    for (int i = 1; i < typeArray.Arity; i++)
                    {
                        var indexOffset = BuildExpr(builder, arrayIndex.Indices[i]);
                        Context.Assert(indexOffset.TypeOf == typeInt, $"Index was built with an unexpected integer type ({indexOffset.TypeOf}).");
                        var timesStride = builder.BuildMul(strides[i], indexOffset);
                        totalOffset = builder.BuildAdd(totalOffset, timesStride);
                    }

                    var elementSize = typeArray.ElementType.Size;
                    totalOffset = builder.BuildMul(LLVMValueRef.CreateConstInt(typeInt, (ulong)elementSize.Bytes), totalOffset);

                    var ptradd = builder.BuildPtrAdd(lvalue, totalOffset, "arrayidx.ptradd");
                    ptradd.Alignment = (uint)arrayIndex.Type.Align.Bytes;
                    return ptradd;
                }

                case SemaExprBinaryBuiltIn binaryBuiltIn:
                {
                    var type = GenerateType(binaryBuiltIn.Type);
                    var left = BuildExpr(builder, binaryBuiltIn.Left);
                    var right = BuildExpr(builder, binaryBuiltIn.Right);

                    switch (binaryBuiltIn.Kind)
                    {
                        default:
                        {
                            Context.Diag.ICE(expr.Location, $"Unimplemented Laye built-in binary operator kind in Choir builder/codegen: {binaryBuiltIn.Kind}");
                            throw new UnreachableException();
                        }
                        
                        case BinaryOperatorKind.Eq | BinaryOperatorKind.Pointer: return builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, left, right, "peq");
                        case BinaryOperatorKind.Neq | BinaryOperatorKind.Pointer: return builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, left, right, "pne");

                        case BinaryOperatorKind.Eq | BinaryOperatorKind.Integer: return builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, left, right, "ieq");
                        case BinaryOperatorKind.Neq | BinaryOperatorKind.Integer: return builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, left, right, "ine");
                        case BinaryOperatorKind.Add | BinaryOperatorKind.Integer: return builder.BuildAdd(left, right, "iadd");
                        case BinaryOperatorKind.Sub | BinaryOperatorKind.Integer: return builder.BuildSub(left, right, "isub");
                    }
                }

                case SemaExprCast cast:
                {
                    switch (cast.CastKind)
                    {
                        default:
                        {
                            Context.Diag.ICE(expr.Location, $"Unimplemented Laye cast kind in Choir builder/codegen: {cast.CastKind}");
                            throw new UnreachableException();
                        }

                        case CastKind.ReferenceToLValue: return BuildExpr(builder, cast.Operand);
                        case CastKind.LValueToReference: return BuildExpr(builder, cast.Operand);
                        case CastKind.PointerToLValue: return BuildExpr(builder, cast.Operand);
                        case CastKind.Implicit: return BuildExpr(builder, cast.Operand);

                        case CastKind.IntegralSignExtend:
                        {
                            var type = GenerateType(cast.Type);
                            return builder.BuildSExt(BuildExpr(builder, cast.Operand), type, "intsext");
                        }

                        case CastKind.IntegralZeroExtend:
                        {
                            var type = GenerateType(cast.Type);
                            return builder.BuildZExt(BuildExpr(builder, cast.Operand), type, "intzext");
                        }

                        case CastKind.IntegralTruncate:
                        {
                            var type = GenerateType(cast.Type);
                            return builder.BuildTrunc(BuildExpr(builder, cast.Operand), type, "inttrunc");
                        }

                        case CastKind.LValueToRValue:
                        {
                            var type = GenerateType(cast.Type);
                            var load = builder.BuildLoad2(type, BuildExpr(builder, cast.Operand), "lv2rv");
                            load.SetAlignment((uint)cast.Type.Align.Bytes);
                            return load;
                        }
                    }
                }

                case SemaExprCall call:
                {
                    Context.Assert(call.Callee.Type.CanonicalType.Type is SemaTypeFunction, "The call expression only works on functions");

                    switch (call.Callee)
                    {
                        default:
                        {
                            Context.Todo($"Handle calling a callee of kind {call.Callee.GetType().FullName}");
                            throw new UnreachableException();
                        }

                        case SemaExprLookup { ReferencedEntity: SemaDeclFunction staticFunction }:
                        {
                            Context.Assert(staticFunction.VarargsKind != VarargsKind.Laye, call.Location, "Generating code for Laye varargs functions is not supported yet.");

                            var functionInfo = _functionInfos[staticFunction];
                            Context.Assert(!functionInfo.IsSret, $"Calling of SRet functions cannot be compiled outside of {nameof(BuildExprIntoMemory)}; this case must be handled there.");

                            var arguments = new List<LLVMValueRef>(call.Arguments.Count * 2);
                            for (int i = 0; i < staticFunction.ParameterDecls.Count; i++)
                            {
                                BuildArgumentForFunctionCall(builder, arguments, staticFunction.ParameterDecls[i], functionInfo.ParameterInfos[i], call.Arguments[i]);
                            }

                            if (staticFunction.VarargsKind == VarargsKind.C)
                            {
                                for (int i = staticFunction.ParameterDecls.Count; i < call.Arguments.Count; i++)
                                    BuildArgumentForFunctionCallAsCVarargs(builder, arguments, call.Arguments[i]);
                            }

                            var functionReturnType = GenerateType(staticFunction.ReturnType);
                            string callValueName = staticFunction.ReturnType.IsVoid ? "" : "call";
                            // no need to BuildExpr when I can look it up manually right now
                            var callee = _declaredValues[staticFunction];
                            return builder.BuildCall2(LLVM.GlobalGetValueType(callee), callee, arguments.ToArray(), callValueName);
                        }
                    }
                }

                case SemaExprOverloadSet:
                {
                    Context.Unreachable("Overload set expressions should never escape analysis.");
                    throw new UnreachableException();
                }

                case SemaExprLookup lookup:
                {
                    Context.Assert(lookup.ReferencedEntity is not null, lookup.Location, "Lookup was not resolved to an entity during sema, should not get to codegen.");

                    if (lookup.IsRegister)
                    {
                        Context.Assert(lookup.ReferencedEntity is SemaDeclRegister, lookup.Location, "Lookup of register value category did not reference a register declaration.");
                        return BuildLoadFromRegister(builder, lookup.Location, (SemaDeclRegister)lookup.ReferencedEntity);
                    }

                    Context.Assert(_declaredValues.ContainsKey(lookup.ReferencedEntity), lookup.Location, "Entity to lookup was not declared in codegen yet. May have been generated incorrectly.");
                    return _declaredValues[lookup.ReferencedEntity];
                }
            }
        }
    }

    private LLVMValueRef BuildLoadFromRegister(LLVMBuilderRef builder, Location location, SemaDeclRegister declRegister)
    {
        Context.Assert(declRegister.Type.IsInteger && declRegister.Type.Size.Bits is 32 or 64, location, "Register load can only be 32 or 64 bits for now.");

        string mov = declRegister.Type.Size.Bits == 32 ? "movl" : "movq";

        var functy = LLVMTypeRef.CreateFunction(declRegister.Type.Size.Bits == 32 ? LLVMTypeRef.Int32 : LLVMTypeRef.Int64, []);
        var asmcall = LLVMValueRef.CreateConstInlineAsm(functy, $"{mov} %{declRegister.RegisterName}, $0;", "=r", true, true);

        return builder.BuildCall2(functy, asmcall, new ReadOnlySpan<LLVMValueRef>(), $"load.{declRegister.RegisterName}");
    }

    private LLVMValueRef BuildStoreToRegister(LLVMBuilderRef builder, Location location, SemaDeclRegister declRegister, LLVMValueRef value)
    {
        Context.Assert(declRegister.Type.IsInteger && declRegister.Type.Size.Bits is 32 or 64, location, "Register load can only be 32 or 64 bits for now.");

        string mov = declRegister.Type.Size.Bits == 32 ? "movl" : "movq";

        var functy = LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, [declRegister.Type.Size.Bits == 32 ? LLVMTypeRef.Int32 : LLVMTypeRef.Int64]);
        var asmcall = LLVMValueRef.CreateConstInlineAsm(functy, $"{mov} $0, %{declRegister.RegisterName}", "r", true, true);

        return builder.BuildCall2(functy, asmcall, new ReadOnlySpan<LLVMValueRef>(ref value), "");
    }

    private ParameterInfo BuildParameterForFunctionDecl(SemaDeclParam paramDecl, List<LLVMTypeRef> outParameterTypes, CallingConvention cc)
    {
        var paramInfo = new ParameterInfo()
        {
            Class = Classify(paramDecl.ParamType),
            BeginIndex = outParameterTypes.Count,
        };

        if (paramInfo.Class.Lo == ValueClass.Memory)
        {
            if (cc == CallingConvention.Laye && !paramDecl.ParamType.IsMutable)
                paramInfo.Semantics = ParameterSemantics.ConstReference;
            else if (Context.Abi.PassMemoryValuesByAddress)
                paramInfo.Semantics = ParameterSemantics.Reference;
        }

        LLVMTypeRef paramType;
        if (paramInfo.Semantics is ParameterSemantics.Reference or ParameterSemantics.ConstReference)
            paramType = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
        else paramType = GenerateType(paramDecl.ParamType);
        outParameterTypes.Add(paramType);

        return paramInfo;
    }

    private LLVMValueRef BuildParameterReconstitutionForFunctionHeader(LLVMBuilderRef builder, ParameterInfo paramInfo)
    {
        throw new NotImplementedException();
    }

    private void BuildArgumentForFunctionCall(LLVMBuilderRef builder, List<LLVMValueRef> outArguments, SemaDeclParam declParam, ParameterInfo paramInfo, SemaExpr arg)
    {
        int beginArgumentCount = outArguments.Count;

        var argumentType = arg.Type.CanonicalType.Type;
        var argumentValue = BuildExpr(builder, arg);

        switch (paramInfo.Semantics)
        {
            case ParameterSemantics.Value:
            {
                switch (argumentType)
                {
                    default:
                    {
                        Context.Todo(arg.Location, $"Explicitly handle argument passing for value of type {arg.Type.ToDebugString(Colors.Off)} (canonical: {argumentType.ToDebugString(Colors)})");
                        throw new UnreachableException();
                    }

                    case SemaTypeBuiltIn typeBuiltIn when typeBuiltIn.IsInteger && typeBuiltIn.Size.Bits <= 64:
                    case SemaTypePointer or SemaTypeBuffer:
                    {
                        Context.Assert(paramInfo.Class.Lo == ValueClass.Integer, "Expected integer class");
                        Context.Assert(paramInfo.Class.Hi == ValueClass.NoClass, "Expected no class");
                        outArguments.Add(argumentValue);
                    } break;
                }
            } break;

            case ParameterSemantics.Reference:
            case ParameterSemantics.ConstReference:
            {
                // instantiate a temporary
                var argumentAlloca = builder.BuildAlloca(GenerateType(arg.Type), $"{declParam.Name}.argref.storage");
                builder.BuildStore(argumentValue, argumentAlloca);
                // and pass the address of the temporary as the argument
                outArguments.Add(argumentAlloca);
            } break;
        }

        Context.Assert(beginArgumentCount < outArguments.Count, "Building an argument to a function call did not properly generate any argument values.");
    }

    private void BuildArgumentForFunctionCallAsCVarargs(LLVMBuilderRef builder, List<LLVMValueRef> outArguments, SemaExpr arg)
    {
        int beginArgumentCount = outArguments.Count;

        var argumentType = arg.Type.CanonicalType.Type;
        var argumentValue = BuildExpr(builder, arg);

        switch (argumentType)
        {
            default:
            {
                Context.Todo(arg.Location, $"Explicitly handle argument passing for value of type {arg.Type.ToDebugString(Colors.Off)} (canonical: {argumentType.ToDebugString(Colors)})");
                throw new UnreachableException();
            }

            case SemaTypeBuiltIn typeBuiltIn when typeBuiltIn.IsNumeric && typeBuiltIn.Size.Bits <= 64:
            case SemaTypePointer or SemaTypeBuffer:
            {
                outArguments.Add(argumentValue);
            } break;
        }

        Context.Assert(beginArgumentCount < outArguments.Count, "Building an argument to a function call did not properly generate any argument values.");
    }
}
