using System.Diagnostics;

using Choir.Formatting;
using Choir.Source;

namespace Choir.Diagnostics;

public sealed class DiagnosticEngine
    : IDisposable
{
    public IDiagnosticConsumer Consumer { get; }

    private bool _ignoreFollowingNotes = false;

    public DiagnosticEngine(IDiagnosticConsumer consumer)
    {
        Consumer = consumer;
    }

    private void OnDiagnosticIgnore()
    {
        _ignoreFollowingNotes = true;
    }

    private void OnDiagnosticEmit()
    {
        _ignoreFollowingNotes = false;
    }

    public void Dispose()
    {
        Flush();
        Consumer.Dispose();
    }

    public void Flush()
    {
        Consumer.Flush();
    }

    public Diagnostic Emit(Diagnostic diagnostic)
    {
        if (diagnostic.Level == DiagnosticLevel.Error && Consumer.ErrorCount > 10)
        {
            OnDiagnosticIgnore();
            return diagnostic;
        }

        if (diagnostic.Level == DiagnosticLevel.Ignore)
        {
            OnDiagnosticIgnore();
            return diagnostic;
        }

        if (diagnostic.Level == DiagnosticLevel.Note && _ignoreFollowingNotes)
            return diagnostic;

        OnDiagnosticEmit();
        Consumer.Consume(diagnostic);

        if (diagnostic.Level == DiagnosticLevel.Fatal)
        {
            Environment.Exit(1);
            throw new UnreachableException();
        }

        return diagnostic;
    }

    public Diagnostic Emit(DiagnosticLevel level, string id, SourceText source,
        SourceLocation location, SourceRange[] ranges, string message)
    {
        return Emit(new Diagnostic(level, id, source, location, ranges, new MarkupLiteral(message)));
    }

    public Diagnostic Emit(DiagnosticLevel level, string id, SourceText source,
        SourceLocation location, SourceRange[] ranges, Markup message)
    {
        return Emit(new Diagnostic(level, id, source, location, ranges, message));
    }

    public Diagnostic Emit(DiagnosticLevel level, string id, SourceText source,
        SourceLocation location, SourceRange[] ranges, MarkupInterpolatedStringHandler message)
    {
        return Emit(new Diagnostic(level, id, source, location, ranges, message.Markup));
    }
}
