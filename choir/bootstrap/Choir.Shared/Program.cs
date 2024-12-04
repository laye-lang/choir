using System.Text;

using Choir.Driver;

namespace Choir;

#if false
public static class Program
{
    public static int Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        var diag = new StreamingDiagnosticWriter(writer: Console.Error, useColor: !Console.IsErrorRedirected);
        return ChoirDriver.RunWithArgs(diag, args);
    }
}
#endif
