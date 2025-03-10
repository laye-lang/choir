namespace Choir.Diagnostics;

public sealed class FormattedDiagnosticWriter
    : IDiagnosticConsumer
{
    public int WarningCount { get; private set; }
    public int ErrorCount { get; private set; }

    public TextWriter Writer { get; }

    private readonly List<Diagnostic> _diagnosticGroup = new(10);

    public FormattedDiagnosticWriter(TextWriter writer)
    {
        Writer = writer;
    }

    public void Consume(Diagnostic diag)
    {
        if (diag.Level != DiagnosticLevel.Note)
            Flush();

        _diagnosticGroup.Add(diag);
    }

    public void Flush()
    {
        if (_diagnosticGroup.Count == 0)
            return;
    }
}
