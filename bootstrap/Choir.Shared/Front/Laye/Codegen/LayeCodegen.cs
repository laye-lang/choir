using System.Diagnostics;

using Choir.CommandLine;
using Choir.Front.Laye.Sema;

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

    private readonly Dictionary<SemaDeclNamed, LLVMValueRef> _declaredValues = [];
    private readonly Dictionary<SemaDeclNamed, LLVMTypeRef> _declaredTypes = [];
    private readonly Dictionary<SemaDeclFunction, FunctionInfo> _functionInfos = [];

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
                Context.Todo($"Classify {type.ToDebugString(Colors.Off)} ({type.GetType().FullName})");
            }
        }
    }

    private LLVMTypeRef GenerateType(SemaTypeQual typeQual)
    {
        var type = typeQual.Type;
        switch (type)
        {
            default:
            {
                Context.Diag.ICE(typeQual.Location, $"Unimplemented Laye type in Choir codegen: {type.GetType().FullName}");
                throw new UnreachableException();
            }

            case SemaTypeBuiltIn builtIn:
            {
                switch (builtIn.Kind)
                {
                    default:
                    {
                        Context.Diag.ICE(typeQual.Location, $"Unimplemented Laye built in type kind in Choir codegen: {builtIn.Kind}");
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
                                Context.Diag.ICE(typeQual.Location, $"Unimplemented Laye float size in Choir codegen: {builtIn.ToDebugString(Context.UseColor ? Colors.On : Colors.Off)}");
                                throw new UnreachableException();
                            }

                            case 32: return LLVMTypeRef.Float;
                            case 64: return LLVMTypeRef.Double;
                            case 80: return LLVMTypeRef.X86FP80;
                            case 128: return LLVMTypeRef.FP128;
                        }
                    }
                }
            }

            case SemaTypeStruct typeStruct:
            {
                return LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)type.Size.Bytes);
                //return _declaredTypes[typeStruct.DeclStruct];
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
        var startBlock = f.AppendBasicBlock("start");

        var builder = LlvmContext.CreateBuilder();
        EnterBlock(builder, startBlock);

        for (int i = 0; i < function.ParameterDecls.Count; i++)
        {
            var paramType = f.Params[i].TypeOf;
            var paramLocal = builder.BuildAlloca(paramType, "param");
            builder.BuildStore(f.GetParam((uint)i), paramLocal);
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

    private void GenerateStructDeclaration(SemaDeclStruct @struct)
    {
        return;
        var structType = LlvmContext.CreateNamedStruct(Mangler.GetMangledName(@struct));
        _declaredTypes[@struct] = structType;
    }

    private void GenerateStructDefinition(SemaDeclStruct @struct)
    {
        return;
        var structType = _declaredTypes[@struct];
        var fieldTypes = @struct.FieldDecls.Select(f => GenerateType(f.FieldType)).ToArray();
        structType.StructSetBody(fieldTypes, false);
    }

    private LLVMBasicBlockRef EnterBlock(LLVMBuilderRef builder, LLVMBasicBlockRef bb)
    {
        Context.Assert(CurrentFunction is not null, "Not currently within a function definition.");
        Context.Assert(builder != default, CurrentFunction.Location, "Invalid builder.");
        Context.Assert(bb != default, CurrentFunction.Location, "Invalid basic block.");
        Context.Assert(bb.Parent != default, CurrentFunction!.Location, "Block is not inside a function.");
        Context.Assert(bb.Parent == CurrentFunctionValue, CurrentFunction!.Location, "Block is not inside this function.");

        if (builder.InsertBlock != default && builder.InsertBlock.Terminator != default && bb != CurrentFunctionValue.EntryBasicBlock)
        {
            builder.BuildBr(bb);
        }

        builder.PositionAtEnd(bb);
        return bb;
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

                if (binding.InitialValue is not null)
                {
                    var init = BuildExpr(builder, binding.InitialValue);
                    var store = builder.BuildStore(init, storage);
                    store.SetAlignment((uint)binding.BindingType.Align.Bytes);
                }
                else
                {
                    builder.BuildMemSet(storage, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, 0), binding.BindingType.Size, binding.BindingType.Align);
                }
            } break;

            case SemaStmtXyzzy:
            {
                // Nothing happens.
            } break;

            case SemaStmtCompound compound:
            {
                foreach (var child in compound.Statements)
                {
                    BuildStmt(builder, child);
                    if (child.ControlFlow != StmtControlFlow.Fallthrough)
                        break;
                }
            } break;

            case SemaStmtReturnVoid @return:
            {
                builder.BuildRetVoid();
            } break;

            case SemaStmtReturnValue @return:
            {
                var value = BuildExpr(builder, @return.Value);
                builder.BuildRet(value);
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
                var target = BuildExpr(builder, assign.Target);
                Context.Assert(target.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind, "Can't assign to non-pointer target.");
                var value = BuildExpr(builder, assign.Value);
                var store = builder.BuildStore(value, target);
                store.SetAlignment((uint)assign.Value.Type.Align.Bytes);
            } break;

            case SemaStmtDiscard expr:
            {
                BuildExpr(builder, expr.Expr);
            } break;

            case SemaStmtExpr expr:
            {
                BuildExpr(builder, expr.Expr);
            } break;
        }
    }

    private LLVMValueRef BuildExpr(LLVMBuilderRef builder, SemaExpr expr)
    {
        unsafe
        {
            switch (expr)
            {
                default:
                {
                    Context.Diag.ICE(expr.Location, $"Unimplemented Laye node in Choir builder/codegen: {expr.GetType().FullName}");
                    throw new UnreachableException();
                }

                case SemaExprLiteralBool literalBool:
                {
                    var type = GenerateType(expr.Type);
                    return LLVMValueRef.CreateConstInt(type, literalBool.LiteralValue ? 1ul : 0ul, false);
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
                                var C = new Colors(Context.UseColor);
                                Context.Assert(false, $"Unhandled type for string literal {expr.Type.ToDebugString(C)} in LLVM code generator.");
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

                        case BinaryOperatorKind.Add | BinaryOperatorKind.Integer: return builder.BuildAdd(left, right, "iadd");
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
                            var functionInfo = _functionInfos[staticFunction];

                            // no need to BuildExpr when I can look it up manually right now
                            var callee = _declaredValues[staticFunction];

                            int argumentOffset = functionInfo.IsSret ? 1 : 0;
                            var arguments = new List<LLVMValueRef>(call.Arguments.Count * 2 + argumentOffset);

                            for (int i = 0; i < call.Arguments.Count; i++)
                            {
                                BuildArgumentForFunctionCall(builder, arguments, staticFunction.ParameterDecls[i], functionInfo.ParameterInfos[i], call.Arguments[i]);
                            }

                            var functionReturnType = GenerateType(staticFunction.ReturnType);

                            LLVMTypeRef resultType;
                            LLVMValueRef sretAlloca = default;

                            if (functionInfo.IsSret)
                            {
                                resultType = LLVMTypeRef.Void;
                                sretAlloca = builder.BuildAlloca(functionReturnType, "sret.storage");
                                arguments[0] = sretAlloca;
                            }
                            else resultType = functionReturnType;

                            LLVMTypeRef calleeType = LLVM.GlobalGetValueType(callee);
                            string callValueName = resultType.Kind == LLVMTypeKind.LLVMVoidTypeKind ? "" : "call";
                            var callValue = builder.BuildCall2(calleeType, callee, arguments.ToArray(), callValueName);

                            if (functionInfo.IsSret)
                            {
                                var sretLoad = builder.BuildLoad2(functionReturnType, sretAlloca);
                                sretLoad.SetAlignment((uint)staticFunction.ReturnType.Align.Bytes);
                                return sretLoad;
                            }
                            else return callValue;
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
                    Context.Assert(_declaredValues.ContainsKey(lookup.ReferencedEntity), lookup.Location, "Entity to lookup was not declared in codegen yet. May have been generated incorrectly.");
                    return _declaredValues[lookup.ReferencedEntity];
                }
            }
        }
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
                        Context.Todo(arg.Location, $"Explicitly handle argument passing for value of type {arg.Type.ToDebugString(Colors.Off)} (canonical: {argumentType.ToDebugString(Colors.Off)})");
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
}
