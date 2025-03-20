namespace Choir.Diagnostics;

public interface IDiagnosticConsumer
    : IDisposable
{
    void IDisposable.Dispose() => GC.SuppressFinalize(this);

    public void Consume(Diagnostic diag);
    public void Flush();
}
