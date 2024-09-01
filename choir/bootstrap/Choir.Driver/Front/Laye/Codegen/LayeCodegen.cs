using System.Diagnostics;

using Choir.Front.Laye.Sema;

using Choir.IR;

using ClangSharp;

namespace Choir.Front.Laye.Codegen;

public sealed class LayeCodegen(Module module)
{
    public static void GenerateIR(TranslationUnit tu)
    {
        foreach (var module in tu.Modules)
            GenerateIR(module);
    }

    public static void GenerateIR(Module module)
    {
        var cm = new ChoirModule(module.Context, module.SourceFile.FileInfo.FullName);
        module.ChoirModule = cm;

        var cg = new LayeCodegen(module);

        // forward declare things first
        foreach (var decl in module.SemaDecls)
        {
            ChoirValue forwardDecl;
            if (decl is SemaDeclFunction function)
                forwardDecl = cg.GenerateDeclaration(function);
            else throw new NotImplementedException($"for decl type {decl.GetType().FullName}");
            cm.AddGlobal(forwardDecl);
        }

        // generate definitions
        foreach (var decl in module.SemaDecls)
        {
            ChoirValue declDef;
            if (decl is SemaDeclFunction function && function.Body is not null)
                declDef = cg.GenerateDefinition(function);
            else throw new NotImplementedException($"for decl type {decl.GetType().FullName}");
            cm.AddGlobal(declDef);
        }
    }

    public ChoirContext Context { get; } = module.Context;
    public Module Module { get; } = module;
    public ChoirModule ChoirModule { get; } = module.ChoirModule!;

    private ChoirTypeLoc GenerateType(SemaTypeQual typeQual)
    {
        return GenerateType(typeQual.Type).TypeLoc(typeQual.Location);

        ChoirType GenerateType(SemaType type)
        {
            switch (type)
            {
                default:
                {
                    Context.Assert(false, typeQual.Location, $"Unimplemented Laye type in Choir codegen: {type.GetType().FullName}");
                    throw new UnreachableException();
                }

                case SemaTypeBuiltIn builtIn:
                {
                    switch (builtIn.Kind)
                    {
                        default:
                        {
                            Context.Assert(false, typeQual.Location, $"Unimplemented Laye built in type kind in Choir codegen: {builtIn.Kind}");
                            throw new UnreachableException();
                        }

                        case BuiltinTypeKind.NoReturn:
                        case BuiltinTypeKind.Void: return ChoirTypeVoid.Instance;

                        case BuiltinTypeKind.Int:
                        {
                            switch (builtIn.Size.Bytes)
                            {
                                default:
                                {
                                    Context.Assert(false, typeQual.Location, $"Unimplemented Laye int type size in Choir codegen: {builtIn.Size.Bytes} bytes");
                                    throw new UnreachableException();
                                }

                                case 4: return ChoirTypeI32.Instance;
                                case 8: return ChoirTypeI64.Instance;
                            }
                        }
                    }
                }
            }
        }
    }

    private ChoirFunction GenerateDeclaration(SemaDeclFunction function)
    {
        var @params = function.ParameterDecls.Select(p =>
        {
            return new ChoirFunctionParam(p.Location, p.Name, GenerateType(p.ParamType));
        }).ToArray();
        var f = new ChoirFunction(function.Location, function.Name, GenerateType(function.ReturnType), @params);
        return f;
    }


    private ChoirFunction GenerateDefinition(SemaDeclFunction function)
    {
        Context.Assert(function.Body is not null, function.Location, "Attempt to generate code for a function definition when only a declaration is present.");
        
        var f = GenerateDeclaration(function);
        var startBlock = f.AppendBlock(function.Body.Location, "start");

        var builder = new ChoirBuilder(ChoirModule);
        builder.PositionAtEnd(startBlock);

        BuildStmt(builder, function.Body);
        
        return f;
    }

    private void BuildStmt(ChoirBuilder builder, SemaStmt stmt)
    {
        switch (stmt)
        {
            default:
            {
                Context.Assert(false, stmt.Location, $"Unimplemented Laye node in Choir builder/codegen: {stmt.GetType().FullName}");
                throw new UnreachableException();
            }

            case SemaStmtCompound compound:
            {
                foreach (var child in compound.Statements)
                    BuildStmt(builder, child);
            } break;

            case SemaStmtReturnValue @return:
            {
                var value = BuildExpr(builder, @return.Value);
                builder.BuildRet(@return.Location, value);
            } break;
        }
    }

    private ChoirValue BuildExpr(ChoirBuilder builder, SemaExpr expr)
    {
        switch (expr)
        {
            default:
            {
                Context.Assert(false, expr.Location, $"Unimplemented Laye node in Choir builder/codegen: {expr.GetType().FullName}");
                throw new UnreachableException();
            }

            case SemaExprEvaluatedConstant constant:
            {
                var type = GenerateType(constant.Type);
                switch (constant.Value.Kind)
                {
                    default:
                    {
                        Context.Assert(false, expr.Location, $"Unimplemented Laye constant kind in Choir builder/codegen: {constant.Value.Kind}");
                        throw new UnreachableException();
                    }

                    case EvaluatedConstantKind.Integer: return new ChoirValueLiteralInteger(constant.Location, constant.Value.IntegerValue, type);
                }
            }
        }
    }
}
