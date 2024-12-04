using System.Diagnostics;

using Choir.CommandLine;
using Choir.Front.Laye.Sema;

using LLVMSharp.Interop;

namespace Choir.Front.Laye.Codegen;

public sealed class LayeCodegen(LayeModule module, LLVMModuleRef llvmModule)
{
    public static LLVMModuleRef GenerateIR(LayeModule module)
    {
        var llvmContext = LLVMContextRef.Create();
        var llvmModule = llvmContext.CreateModuleWithName(module.ModuleName);

        var cg = new LayeCodegen(module, llvmModule);

        void GenerateDeclarations(IEnumerable<SemaDeclNamed> decls)
        {
            foreach (var decl in decls)
            {
                if (decl is SemaDeclStruct @struct)
                    cg.GenerateStructDeclaration(@struct);
            }

            foreach (var decl in decls)
            {
                if (decl is SemaDeclFunction function)
                    cg.GenerateFunctionDeclaration(function);
            }
        }

        void GenerateDefinitions(IEnumerable<SemaDeclNamed> decls)
        {
            foreach (var decl in module.Declarations)
            {
                if (decl is SemaDeclStruct @struct)
                    cg.GenerateStructDefinition(@struct);
            }

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

    private SemaDeclFunction? CurrentFunction { get; set; }
    private LLVMValueRef CurrentFunctionValue => CurrentFunction is null ? default : _declaredValues[CurrentFunction];

    private int _nameCounter = 0;
    private string NextName(string name = "") => $"{name}{_nameCounter++}";

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
                    case BuiltinTypeKind.Int: return LLVMTypeRef.CreateInt((uint)Context.Target.SizeOfSizeType.Bits);
                    case BuiltinTypeKind.IntSized: return LLVMTypeRef.CreateInt((uint)builtIn.Size.Bits);
                }
            }

            case SemaTypeStruct typeStruct: return _declaredTypes[typeStruct.DeclStruct];

            case SemaTypeBuffer: return LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
        }
    }

    private void GenerateFunctionDeclaration(SemaDeclFunction function)
    {
        string functionName = Mangler.GetMangledName(function);
        var paramTypes = function.ParameterDecls.Select(p => GenerateType(p.ParamType)).ToArray();
        var functionType = LLVMTypeRef.CreateFunction(GenerateType(function.ReturnType), paramTypes);
        var f = LlvmModule.AddFunction(functionName, functionType);
        _declaredValues[function] = f;
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
        var structType = LlvmContext.CreateNamedStruct(Mangler.GetMangledName(@struct));
        _declaredTypes[@struct] = structType;
    }

    private void GenerateStructDefinition(SemaDeclStruct @struct)
    {
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
                    unsafe
                    {
                        LLVM.BuildMemSet(builder, storage,
                            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, 0),
                            type.SizeOf,
                            (uint)binding.BindingType.Align.Bytes
                        );
                    }
                }
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
                    var type = LLVMTypeRef.Int1;
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
                    var structType = GenerateType(structField.Operand.Type);
                    return builder.BuildStructGEP2(structType, lvalue, (uint)structField.FieldIndex, "fieldidx");
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
                    var callee = BuildExpr(builder, call.Callee);
                    var arguments = call.Arguments.Select(e => BuildExpr(builder, e)).ToArray();
                    var resultType = GenerateType(((SemaTypeFunction)call.Callee.Type.CanonicalType.Type).ReturnType);

                    LLVMTypeRef calleeType = LLVM.GlobalGetValueType(callee);
                    return builder.BuildCall2(calleeType, callee, arguments, "call");
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
}
