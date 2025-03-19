using Choir.Diagnostics;
using Choir.Source;

namespace Choir.Driver;

public sealed class ChoirDriver
    : ICompilerDriver
{
    public static ChoirDriver Create()
    {
        return new ChoirDriver();
    }

    private ChoirDriver()
    {
    }

    public int Execute()
    {
        var writer = new FormattedDiagnosticWriter(Console.Error);
        using var diag = new DiagnosticEngine(writer);

        var source = new SourceText("foo.laye", "foo bar");
        diag.Emit(DiagnosticLevel.Warning, "LY0001", source, new(0), [], "This is a test message.");

        return 0;
    }
}
