using System.Text;

using Choir;
using Choir.Driver;
using Choir.IR;

namespace LLVMTest;

internal class Program
{
    static int Main()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        var diag = new StreamingDiagnosticWriter(writer: Console.Error, useColor: !Console.IsErrorRedirected);
        var context = new ChoirContext(diag, ChoirTarget.X86_64, ChoirAbi.WindowxX64, true);
        var lyirModule = new LyirModule(context);
        var lyirFunction = new LyirFunction(Location.Nowhere, LyirTypeVoid.Instance, [new LyirTypeInt(32)], "foo");
        var builder = lyirFunction.AppendBlock();

        return 0;

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
