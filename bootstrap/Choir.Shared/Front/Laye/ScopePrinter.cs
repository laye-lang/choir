using Choir.Front.Laye.Sema;

namespace Choir.Front.Laye;

public sealed class ScopePrinter : BaseTreePrinter<(string Name, SemaDeclNamed DeclNamed)>
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

    protected override void Print((string Name, SemaDeclNamed DeclNamed) info)
    {
        Console.WriteLine($"{C[ColorName]}{info.Name}");
        PrintEntity(info.DeclNamed);
    }

    private void PrintEntity(SemaDeclNamed node)
    {
    }
}
