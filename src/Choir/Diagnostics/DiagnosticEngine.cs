using System.Diagnostics;

using Choir.Formatting;
using Choir.Source;

namespace Choir.Diagnostics;

public sealed class DiagnosticEngine(IDiagnosticConsumer consumer)
    : IDisposable
{
    public IDiagnosticConsumer Consumer { get; } = consumer;

    private bool _ignoreFollowingNotes = false;

    public int ErrorCount { get; private set; } = 0;
    public bool HasEmittedErrors => ErrorCount > 0;

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
        if (diagnostic.Level == DiagnosticLevel.Error && ErrorCount > 10)
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

        if (diagnostic.Level >= DiagnosticLevel.Error)
            ErrorCount++;

        OnDiagnosticEmit();
        Consumer.Consume(diagnostic);

        if (diagnostic.Level == DiagnosticLevel.Fatal)
        {
            Flush();

            Environment.Exit(1);
            throw new UnreachableException();
        }

        return diagnostic;
    }

    public Diagnostic Emit(DiagnosticLevel level, string message)
    {
        return Emit(new Diagnostic(level, new MarkupLiteral(message)));
    }

    public Diagnostic Emit(DiagnosticLevel level, SourceText source, SourceLocation location, string message)
    {
        return Emit(new Diagnostic(level, null, source, location, [], new MarkupLiteral(message)));
    }

    public Diagnostic Emit(DiagnosticLevel level, SourceText source, SourceLocation location, SourceRange[] ranges, string message)
    {
        return Emit(new Diagnostic(level, null, source, location, ranges, new MarkupLiteral(message)));
    }

    public Diagnostic Emit(DiagnosticLevel level, string id, SourceText source,
        SourceLocation location, SourceRange[] ranges, string message)
    {
        return Emit(new Diagnostic(level, id, source, location, ranges, new MarkupLiteral(message)));
    }

    public Diagnostic Emit(DiagnosticLevel level, Markup message)
    {
        return Emit(new Diagnostic(level, message));
    }

    public Diagnostic Emit(DiagnosticLevel level, SourceText source, SourceLocation location, Markup message)
    {
        return Emit(new Diagnostic(level, null, source, location, [], message));
    }

    public Diagnostic Emit(DiagnosticLevel level, SourceText source,
        SourceLocation location, SourceRange[] ranges, Markup message)
    {
        return Emit(new Diagnostic(level, null, source, location, ranges, message));
    }

    public Diagnostic Emit(DiagnosticLevel level, string id, SourceText source,
        SourceLocation location, SourceRange[] ranges, Markup message)
    {
        return Emit(new Diagnostic(level, id, source, location, ranges, message));
    }

    public Diagnostic Emit(DiagnosticLevel level, MarkupInterpolatedStringHandler message)
    {
        return Emit(new Diagnostic(level, message.Markup));
    }

    public Diagnostic Emit(DiagnosticLevel level, SourceText source, SourceLocation location, MarkupInterpolatedStringHandler message)
    {
        return Emit(new Diagnostic(level, null, source, location, [], message.Markup));
    }

    public Diagnostic Emit(DiagnosticLevel level, SourceText source,
        SourceLocation location, SourceRange[] ranges, MarkupInterpolatedStringHandler message)
    {
        return Emit(new Diagnostic(level, null, source, location, ranges, message.Markup));
    }

    public Diagnostic Emit(DiagnosticLevel level, string id, SourceText source,
        SourceLocation location, SourceRange[] ranges, MarkupInterpolatedStringHandler message)
    {
        return Emit(new Diagnostic(level, id, source, location, ranges, message.Markup));
    }
}
