using System.Text;

using Choir;
using Choir.Driver;

namespace LLVMTest;

internal class Program
{
    static int Main()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        int exitCode = CompileCore();
        if (0 != exitCode) return exitCode;

        exitCode = CompileBasic();
        if (0 != exitCode) return exitCode;

        return 0;
    }

    static int CompileCore()
    {
        var diag = new StreamingDiagnosticWriter(writer: Console.Error, useColor: !Console.IsErrorRedirected);
        return LayecDriver.RunWithArgs(diag, ["lib/laye/core/entry.laye"]);
    }

    static int CompileBasic()
    {
        var diag = new StreamingDiagnosticWriter(writer: Console.Error, useColor: !Console.IsErrorRedirected);
        return LayecDriver.RunWithArgs(diag, ["test/laye/basic.laye", "core.mod"]);
    }
}
