namespace Choir.Diagnostics;

public interface IDiagnosticConsumer
{
    public int WarningCount { get; }
    public int ErrorCount { get; }

    public void Consume(Diagnostic diag);
    public void Flush();
}
