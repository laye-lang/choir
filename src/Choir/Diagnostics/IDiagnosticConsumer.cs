namespace Choir.Diagnostics;

public interface IDiagnosticConsumer
    : IDisposable
{
    public int WarningCount { get; }
    public int ErrorCount { get; }

    void IDisposable.Dispose() => GC.SuppressFinalize(this);

    public void Consume(Diagnostic diag);
    public void Flush();
}
