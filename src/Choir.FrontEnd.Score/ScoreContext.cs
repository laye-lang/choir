using Choir.Diagnostics;
using Choir.Formatting;
using Choir.FrontEnd.Score.Diagnostics;
using Choir.FrontEnd.Score.Types;
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

    private readonly TypeStore _typeStore;

    public ScoreContext(IDiagnosticConsumer diagConsumer, Target target)
        : base(diagConsumer, target)
    {
        _typeStore = new(this, target);
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

    private sealed class TypeStore(ScoreContext context, Target target)
    {
        private readonly ScoreTypeBuiltin _builtinVoid = new(ScoreBuiltinTypeKind.Void, Size.Zero, Align.ByteAligned);
        private readonly ScoreTypeBuiltin _builtinNoreturn = new(ScoreBuiltinTypeKind.Noreturn, Size.Zero, Align.ByteAligned);
        private readonly ScoreTypeBuiltin _builtinBool = new(ScoreBuiltinTypeKind.Bool, Size.FromBytes(1), Align.ByteAligned);
        private readonly ScoreTypeBuiltin _builtinInt = new(ScoreBuiltinTypeKind.Bool, target.SizeOfPointer, target.AlignOfPointer);
        private readonly Dictionary<int, ScoreTypeBuiltin> _builtinSizedIntegers = [];
        private readonly ScoreTypeBuiltin _builtinFloat32 = new(ScoreBuiltinTypeKind.FloatSized, Size.FromBytes(4), Align.ForBytes(4));
        private readonly ScoreTypeBuiltin _builtinFloat64 = new(ScoreBuiltinTypeKind.FloatSized, Size.FromBytes(8), Align.ForBytes(8));
    }
}
