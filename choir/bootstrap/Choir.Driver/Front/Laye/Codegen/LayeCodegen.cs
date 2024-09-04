using System.Diagnostics;
using System.Reflection.Metadata;

using Choir.Front.Laye.Sema;

using Choir.IR;

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

    private readonly Dictionary<SemaDeclNamed, ChoirValue> _declaredValues = [];

    private int _nameCounter = 0;
    private string NextName(string name = "") => $"{name}{_nameCounter++}";

    private ChoirTypeLoc GenerateType(SemaTypeQual typeQual)
    {
        return GenerateType(typeQual.Type).TypeLoc(typeQual.Location);

        ChoirType GenerateType(SemaType type)
        {
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
                        case BuiltinTypeKind.Void: return ChoirTypeVoid.Instance;
                        case BuiltinTypeKind.Int:
                        {
                            switch (Align.ForBits(builtIn.Size.Bits).Value)
                            {
                                default:
                                {
                                    Context.Diag.ICE(typeQual.Location, $"Currently unsupported Laye integer size: {builtIn.Size.Bits} bits");
                                    throw new UnreachableException();
                                }

                                case 1: return ChoirTypeByte.Instance;
                                case 2: return ChoirTypeShort.Instance;
                                case 4: return ChoirTypeInt.Instance;
                                case 8: return ChoirTypeLong.Instance;
                            }
                        }
                    }
                }
            }
        }
    }

    private ChoirFunction GenerateDefinition(SemaDeclFunction function)
    {
        Context.Assert(function.Body is not null, function.Location, "Attempt to generate code for a function definition when only a declaration is present.");

        var @params = function.ParameterDecls.Select(p =>
        {
            return new ChoirFunctionParam(p.Location, p.Name, GenerateType(p.ParamType));
        }).ToArray();

        var f = new ChoirFunction(Context, function.Location, function.Name, GenerateType(function.ReturnType), @params);
        _declaredValues[function] = f;

        var startBlock = f.AppendBlock(function.Body.Location, "start");

        var builder = new ChoirBuilder(ChoirModule);
        builder.PositionAtEnd(startBlock);

        foreach (var (paramDecl, paramValue) in function.ParameterDecls.Zip(@params))
        {
            var local = builder.BuildAlloca(paramDecl.Location, NextName("param"), paramValue.Type, 1, paramValue.Type.Type.Align);
            var storeParam = builder.BuildStore(paramDecl.Location, local, paramValue);
            _declaredValues[paramDecl] = local;
        }

        BuildStmt(builder, function.Body);
        
        return f;
    }

    private void BuildStmt(ChoirBuilder builder, SemaStmt stmt)
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

                    case EvaluatedConstantKind.Integer: return new ChoirValueLiteralInteger(constant.Location, constant.Value.IntegerValue, type);
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

                    case BinaryOperatorKind.Add | BinaryOperatorKind.Integer:
                    {
                        return builder.BuildIAdd(binaryBuiltIn.Location, NextName("iadd"), type, left, right);
                    }
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

                    case CastKind.LValueToRValue: return builder.BuildLoad(cast.Location, NextName("lv2rv"), GenerateType(cast.Type), BuildExpr(builder, cast.Operand));
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
