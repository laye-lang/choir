using System.Text;

namespace Choir.Driver.LayeC;

internal class Program
{
    static int Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        using var diag = new StreamingDiagnosticWriter(writer: Console.Error, useColor: !Console.IsErrorRedirected);
        return LayecHighLevelDriver.RunWithArgs(diag, args);
    }
}
