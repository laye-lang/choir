using Choir.Front.Laye.Sema;

namespace Choir.Front.Laye;

public sealed class ScopePrinter : BaseTreePrinter<(string Name, Symbol Symbol)>
{
    public ScopePrinter(bool useColor)
        : base(useColor)
    {
        ColorBase = CommandLine.Color.Yellow;
    }

    public void PrintScope(Scope scope, string scopeName)
    {
        Console.WriteLine($"{C[ColorBase]}{scopeName}");
        PrintChildren(scope.SelectMany(kv => kv.Symbols.Select(s => (kv.Name, s))));
    }

    protected override void Print((string Name, Symbol Symbol) info)
    {
        if (info.Symbol is NamespaceSymbol @namespace)
        {
            Console.WriteLine($"{C[ColorBase]}Scope {C[ColorName]}{info.Name}");
            PrintChildren(@namespace.Symbols.SelectMany(kv => kv.Symbols.Select(s => (kv.Name, s))));
        }
        else if (info.Symbol is EntitySymbol entity)
        {
            Console.WriteLine($"{C[ColorBase]}Entity {C[ColorName]}{info.Name} :: ");
            PrintEntity(entity.Entity);
        }
    }

    private void PrintEntity(SemaDecl node)
    {
    }
}
