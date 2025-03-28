namespace Choir.Front.Laye.Sema;

public class SemaPrinter : BaseTreePrinter<BaseSemaNode>
{
    private readonly ScopePrinter _scopePrinter;
    private readonly bool _printScopes;

    public ChoirContext Context { get; }

    public SemaPrinter(ChoirContext context, bool printScopes)
        : base(context.UseColor)
    {
        _scopePrinter = new(context.UseColor);
        _printScopes = printScopes;

        Context = context;
        ColorBase = CommandLine.Color.Red;
    }

    public void PrintModuleHeader(LayeModule module)
    {
        Console.WriteLine($"{C[ColorMisc]}// Laye Module '{(module.ModuleName)}'");

        if (_printScopes)
        {
            if (module.ModuleScope.Count > 0)
                _scopePrinter.PrintScope(module.ModuleScope, "Module Scope");

            if (module.ExportScope.Count > 0)
                _scopePrinter.PrintScope(module.ExportScope, "Exports");
        }
    }

    public void PrintModule(LayeModule module)
    {
        PrintModuleHeader(module);
        foreach (var node in module.Declarations)
            Print(node);
    }

    protected virtual void PrintSemaNodeHeader(BaseSemaNode node)
    {
        Console.Write($"{C[ColorBase]}{node.GetType().Name} ");
        if (node is SemaDecl nodeDecl)
            Console.Write($"{C[ColorLocation]}<{nodeDecl.Location.Offset}> ");
        else if (node is SemaStmt nodeStmt)
            Console.Write($"{C[ColorLocation]}<{nodeStmt.Location.Offset}> ");
        else if (node is SemaExpr nodeExpr)
            Console.Write($"{C[ColorLocation]}<{nodeExpr.Location.Offset}> ");
    }

    protected override void Print(BaseSemaNode node)
    {
        PrintSemaNodeHeader(node);

        if (node.Dependence != ExprDependence.None)
        {
            Console.Write(C[ColorBase]);
            Console.Write($"({node.Dependence}) ");
        }

        if (node is SemaExpr expr)
        {
            Console.Write(C[ColorBase]);
            Console.Write($"{expr.ValueCategory} ");
            if (expr.IsDiscardable)
            Console.Write("Discardable ");
            Console.Write($"{expr.Type.ToDebugString(C)} ");
        }

        switch (node)
        {
            default: break;

            case SemaTypeQual typeQual:
            {
                Console.Write($"{C[ColorBase]}{typeQual.Qualifiers}");
            } break;

            case SemaDeclBinding declBinding:
            {
                Console.Write($"{declBinding.BindingType.ToDebugString(C)} {C[ColorName]}{declBinding.Name}");
            } break;

            case SemaDeclParam declParameter:
            {
                if (declParameter.IsRefParam)
                    Console.Write($"{C.LayeKeyword()}ref ");
                Console.Write($"{declParameter.ParamType.ToDebugString(C)} {C[ColorName]}{declParameter.Name}");
            } break;

            case SemaDeclFunction declFunction:
            {
                Console.Write(declFunction.FunctionType(Context).ToDebugString(C));
                Console.Write($" {C[ColorName]}{declFunction.Name}");
            } break;

            case SemaDeclField declField:
            {
                Console.Write($"{declField.FieldType.ToDebugString(C)} {C[ColorName]}{declField.Name}");
            } break;

            case SemaDeclStruct declStruct:
            {
                Console.Write($"{C[ColorName]}{declStruct.Name}");
            } break;

            case SemaDeclAlias declAlias:
            {
                if (declAlias.IsStrict)
                    Console.Write($"{C[ColorBase]}Strict ");
                Console.Write($"{C[ColorName]}{declAlias.Name} = {declAlias.AliasedType.ToDebugString(C)}");
            } break;

            case SemaType type:
            {
                Console.Write(type.ToDebugString(C));
            } break;

            case SemaExprLiteralInteger literalInteger:
            {
                Console.Write(literalInteger.LiteralValue);
            } break;

            case SemaExprEvaluatedConstant evaluatedConstant:
            {
                switch (evaluatedConstant.Value.Kind)
                {
                    default: break;
                    case EvaluatedConstantKind.Integer:
                        Console.Write(evaluatedConstant.Value.IntegerValue);
                        break;
                }
            } break;

            case SemaExprCast cast:
            {
                Console.Write(C[ColorBase]);
                Console.Write(cast.CastKind);
            } break;

            case SemaExprLookup lookupSimple:
            {
                Console.Write(C.LayeName());
                Console.Write(lookupSimple.ReferencedEntity?.Name ?? "<no decl>");
            } break;

            case SemaExprBinaryBuiltIn binaryBuiltIn:
            {
                Console.Write(C[ColorBase]);
                Console.Write(binaryBuiltIn.OperatorToken.Location.Span(Context).ToString());
                Console.Write(C[ColorBase]);
                Console.Write(" (");
                Console.Write(binaryBuiltIn.Kind);
                Console.Write(")");
            } break;

            case SemaExprField field:
            {
                Console.Write(C.LayeName());
                Console.Write(field.FieldText);
            } break;
        }
        
        Console.WriteLine(C.Default);
        PrintChildren(node.Children);
    }
}
