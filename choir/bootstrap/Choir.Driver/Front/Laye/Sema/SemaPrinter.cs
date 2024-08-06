namespace Choir.Front.Laye.Sema;

public class SemaPrinter : BaseTreePrinter<BaseSemaNode>
{
    public ChoirContext Context { get; }

    public SemaPrinter(ChoirContext context)
        : base(context.UseColor)
    {
        Context = context;
        ColorBase = CommandLine.Color.Green;
    }

    public void PrintModuleHeader(Module module)
    {
        Console.WriteLine($"{C[ColorMisc]}// Laye Module '{module.SourceFile.FileInfo.FullName}'");
    }
    
    public void PrintModule(Module module)
    {
        PrintModuleHeader(module);
        foreach (var node in module.SemaDecls)
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
        switch (node)
        {
            default: break;

            case SemaTypeQual typeQual:
            {
                Console.Write($"{C[ColorBase]}{typeQual.Qualifiers}");
            } break;

            case SemaDeclField declField:
            {
                Console.Write($"{C[ColorName]}{declField.Name}");
            } break;

            case SemaDeclStruct declStruct:
            {
                Console.Write($"{C[ColorName]}{declStruct.Name}");
            } break;

            case SemaDeclAlias declAlias:
            {
                if (declAlias.IsStrict)
                    Console.Write($"{C[ColorBase]}Strict ");
                Console.Write($"{C[ColorName]}{declAlias.Name}");
            } break;

            case SemaType typeBuiltin:
            {
                Console.Write(typeBuiltin.ToDebugString(C));
            } break;
        }
        
        Console.WriteLine(C.Reset);
        PrintChildren(node.Children);
    }
}
