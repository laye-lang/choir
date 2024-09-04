using System.Diagnostics;

using LLVMSharp.Interop;

using Choir.Front.Laye.Sema;

namespace Choir.Front.Laye.Codegen;

public sealed class LayeCodegen(Module module, LLVMContextRef llvmContext)
{
    public static void GenerateIR(TranslationUnit tu)
    {
        foreach (var module in tu.Modules)
            GenerateIR(module);
    }

    public static void GenerateIR(Module module)
    {
        var llvmContext = LLVMContextRef.Create();
        
        var llvmModule = llvmContext.CreateModuleWithName(module.SourceFile.FileInfo.FullName);
        module.LlvmModule = llvmModule;

        //var diBuilder = llvmModule.CreateDIBuilder();
        //diBuilder.CreateCompileUnit(LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC, diBuilder.CreateFile(module.SourceFile.FileInfo.FullName, ""), "Choir Compiler", 0, "", 0, "", LLVMDWARFEmissionKind.LLVMDWARFEmissionFull, 0, 0, 0, "", "");

        var cg = new LayeCodegen(module, llvmContext);

        // generate definitions
        foreach (var decl in module.SemaDecls)
        {
            LLVMValueRef declDef;
            if (decl is SemaDeclFunction function && function.Body is not null)
                declDef = cg.GenerateDefinition(function);
            else throw new NotImplementedException($"for decl type {decl.GetType().FullName}");
        }
    }

    public ChoirContext Context { get; } = module.Context;
    public Module Module { get; } = module;
    public LLVMContextRef LlvmContext { get; } = llvmContext;
    public LLVMModuleRef LlvmModule { get; } = module.LlvmModule!.Value;

    private readonly Dictionary<SemaDeclNamed, LLVMValueRef> _declaredValues = [];

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
                    case BuiltinTypeKind.Int: return LLVMTypeRef.CreateInt((uint)builtIn.Size.Bits);
                }
            }
        }
    }

    private LLVMValueRef GenerateDeclaration(SemaDeclFunction function)
    {
        var paramTypes = function.ParameterDecls.Select(p => GenerateType(p.ParamType)).ToArray();
        var functionType = LLVMTypeRef.CreateFunction(GenerateType(function.ReturnType), paramTypes);
        var f = LlvmModule.AddFunction(function.Name, functionType);
        return _declaredValues[function] = f;
    }

    private LLVMValueRef GenerateDefinition(SemaDeclFunction function)
    {
        Context.Assert(function.Body is not null, function.Location, "Attempt to generate code for a function definition when only a declaration is present.");

        var paramTypes = function.ParameterDecls.Select(p => GenerateType(p.ParamType)).ToArray();
        var functionType = LLVMTypeRef.CreateFunction(GenerateType(function.ReturnType), paramTypes);
        var f = LlvmModule.AddFunction(function.Name, functionType);
        // if this function was forward declared, we overwrite that with the definition
        _declaredValues[function] = f;

        var startBlock = f.AppendBasicBlock("start");

        var builder = LlvmContext.CreateBuilder();
        builder.PositionAtEnd(startBlock);

        for (int i = 0; i < function.ParameterDecls.Count; i++)
        {
            var paramType = paramTypes[i];
            var paramLocal = builder.BuildAlloca(paramType, "param");
            builder.BuildStore(f.GetParam((uint)i), paramLocal);
            _declaredValues[function.ParameterDecls[i]] = paramLocal;
        }

        BuildStmt(builder, function.Body);
        
        return f;
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

            case SemaStmtCompound compound:
            {
                foreach (var child in compound.Statements)
                    BuildStmt(builder, child);
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
        }
    }

    private LLVMValueRef BuildExpr(LLVMBuilderRef builder, SemaExpr expr)
    {
        switch (expr)
        {
            default:
            {
                Context.Diag.ICE(expr.Location, $"Unimplemented Laye node in Choir builder/codegen: {expr.GetType().FullName}");
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
                }
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

                    case CastKind.LValueToRValue: return builder.BuildLoad2(GenerateType(cast.Type), BuildExpr(builder, cast.Operand), "lv2rv");
                }
            }

            case SemaExprLookupSimple lookupSimple:
            {
                Context.Assert(lookupSimple.ReferencedEntity is not null, lookupSimple.Location, "Lookup was not resolved to an entity during sema, should not get to codegen.");
                Context.Assert(_declaredValues.ContainsKey(lookupSimple.ReferencedEntity), lookupSimple.Location, "Entity to lookup was not declared in codegen yet. May have been generated incorrectly.");
                return _declaredValues[lookupSimple.ReferencedEntity];
            }
        }
    }
}
