using Choir.Diagnostics;
using Choir.Formatting;
using Choir.FrontEnd.Score.Diagnostics;
using Choir.Source;

namespace Choir.FrontEnd.Score;

public sealed class ScoreContext
    : ChoirContext
{
    private readonly Dictionary<ScoreDiagnosticSemantic, DiagnosticLevel> _semanticLevels = new()
    {
        { ScoreDiagnosticSemantic.Note, DiagnosticLevel.Note },
        { ScoreDiagnosticSemantic.Remark, DiagnosticLevel.Remark },
        { ScoreDiagnosticSemantic.Warning, DiagnosticLevel.Warning },
        { ScoreDiagnosticSemantic.Error, DiagnosticLevel.Error },
    };

    public ScoreContext(IDiagnosticConsumer diagConsumer)
        : base(diagConsumer)
    {
    }

    public Diagnostic EmitDiagnostic(ScoreDiagnosticSemantic semantic, string id, SourceText source,
        SourceLocation location, SourceRange[] ranges, string message)
    {
        return Diag.Emit(_semanticLevels[semantic], id, source, location, ranges, message);
    }

    public Diagnostic EmitDiagnostic(ScoreDiagnosticSemantic semantic, string id, SourceText source,
        SourceLocation location, SourceRange[] ranges, Markup message)
    {
        return Diag.Emit(_semanticLevels[semantic], id, source, location, ranges, message);
    }

    public Diagnostic EmitDiagnostic(ScoreDiagnosticSemantic semantic, string id, SourceText source,
        SourceLocation location, SourceRange[] ranges, MarkupInterpolatedStringHandler message)
    {
        return Diag.Emit(_semanticLevels[semantic], id, source, location, ranges, message.Markup);
    }
}
