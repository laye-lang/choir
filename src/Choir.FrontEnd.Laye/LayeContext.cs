using Choir.Diagnostics;
using Choir.Formatting;
using Choir.FrontEnd.Score.Diagnostics;
using Choir.Source;

namespace Choir.FrontEnd.Score;

public sealed class LayeContext
    : ChoirContext
{
    private readonly Dictionary<LayeDiagnosticSemantic, DiagnosticLevel> _semanticLevels = new()
    {
        { LayeDiagnosticSemantic.Note, DiagnosticLevel.Note },
        { LayeDiagnosticSemantic.Remark, DiagnosticLevel.Remark },
        { LayeDiagnosticSemantic.Warning, DiagnosticLevel.Warning },
        { LayeDiagnosticSemantic.Error, DiagnosticLevel.Error },
    };

    public LayeContext(IDiagnosticConsumer diagConsumer)
        : base(diagConsumer)
    {
    }

    public Diagnostic EmitDiagnostic(LayeDiagnosticSemantic semantic, string id, SourceText source,
        SourceLocation location, SourceRange[] ranges, string message)
    {
        return Diag.Emit(_semanticLevels[semantic], id, source, location, ranges, message);
    }

    public Diagnostic EmitDiagnostic(LayeDiagnosticSemantic semantic, string id, SourceText source,
        SourceLocation location, SourceRange[] ranges, Markup message)
    {
        return Diag.Emit(_semanticLevels[semantic], id, source, location, ranges, message);
    }

    public Diagnostic EmitDiagnostic(LayeDiagnosticSemantic semantic, string id, SourceText source,
        SourceLocation location, SourceRange[] ranges, MarkupInterpolatedStringHandler message)
    {
        return Diag.Emit(_semanticLevels[semantic], id, source, location, ranges, message.Markup);
    }
}
