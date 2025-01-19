using Choir.Driver.Options;

namespace Choir.Driver;

public class LayecLowLevelDriver
    : BaseLayeDriver<LayecLowLevelDriverOptions, BaseLayeCompilerDriverArgParseState>
{
    protected LayecLowLevelDriver(string programName, DiagnosticWriter diag, LayecLowLevelDriverOptions options)
        : base(programName, diag, options)
    {
    }

    public override int Execute()
    {
        throw new NotImplementedException();
    }
}
