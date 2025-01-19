using System.Runtime.InteropServices;

using Choir.Driver.Options;

namespace Choir.Driver;

public abstract class BaseLayeDriver<TOptions, TArgParseState>
    where TOptions : BaseLayeDriverOptions<TOptions, TArgParseState>, new()
    where TArgParseState : BaseLayeCompilerDriverArgParseState, new()
{
    public string ProgramName { get; }
    public ChoirContext Context { get; }

    public TOptions Options { get; }

    protected BaseLayeDriver(string programName, DiagnosticWriter diag, TOptions options)
    {
        ProgramName = programName;
        Options = options;

        var abi = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ChoirAbi.WindowxX64 : ChoirAbi.SysV;
        Context = new(diag, ChoirTarget.X86_64, abi, options.OutputColoring)
        {
            EmitVerboseLogs = options.ShowVerboseOutput,
            OmitSourceTextInModuleBinary = options.OmitSourceTextInModuleBinary,
        };
    }

    public abstract int Execute();
}
