
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Choir;

// check the following envs first
// - CHOIR_CC
// - CHOIR_CXX
// - CC
// - CXX
// - PATH
// else no C compiler found
public sealed class ChoirCCDriver
{
    private readonly DiagnosticWriter _diag;
    private readonly string[] _args;

    public ChoirCCDriver(DiagnosticWriter diag, string[] args)
    {
        _diag = diag;
        _args = args;
    }

    public int Execute()
    {
        string cc = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cl" : "cc";

        if (Environment.GetEnvironmentVariable("CC") is string ccEnv)
            cc = ccEnv;

        // Console.WriteLine($"{cc} {string.Join(" ", _args)}");

        var process = Process.Start(cc, _args);
        process.WaitForExit();

        return process.ExitCode;
    }
}
