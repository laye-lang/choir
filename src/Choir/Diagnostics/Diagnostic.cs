using Choir.Formatting;
using Choir.Source;

namespace Choir.Diagnostics;

public readonly struct Diagnostic
{
    public readonly DiagnosticLevel Level;
    public readonly string? Id;
    public readonly SourceText? Source;
    public readonly SourceLocation Location;
    public readonly IReadOnlyList<SourceRange> Ranges;
    public readonly Markup Message;

    public Diagnostic(DiagnosticLevel level, Markup message)
    {
        Level = level;
        Ranges = [];
        Message = message;
    }

    public Diagnostic(DiagnosticLevel level, string? id, SourceText source, SourceLocation location, SourceRange[] ranges, Markup message)
    {
        Level = level;
        Id = id;
        Source = source;
        Location = location;
        Ranges = ranges;
        Message = message;
    }

    public Diagnostic(DiagnosticLevel level, MarkupInterpolatedStringHandler message)
    {
        Level = level;
        Ranges = [];
        Message = message.Markup;
    }

    public Diagnostic(DiagnosticLevel level, string? id, SourceText? source,
        SourceLocation location, SourceRange[] ranges, MarkupInterpolatedStringHandler message)
    {
        Level = level;
        Id = id;
        Source = source;
        Location = location;
        Ranges = ranges;
        Message = message.Markup;
    }
}
