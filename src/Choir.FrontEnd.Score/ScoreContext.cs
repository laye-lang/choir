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

    public TypeStore Types;

    public ScoreContext(IDiagnosticConsumer diagConsumer, Target target)
        : base(diagConsumer, target)
    {
        Types = new(this, target);
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

    public sealed class TypeStore(ScoreContext context, Target target)
    {
        private readonly Dictionary<int, ScoreTypeBuiltin> _builtinSizedIntegers = [];

        public ScoreTypePoison Poison { get; } = ScoreTypePoison.Instance;
        public ScoreTypeBuiltin Void { get; } = ScoreTypeBuiltin.Void;
        public ScoreTypeBuiltin Noreturn { get; } = ScoreTypeBuiltin.Noreturn;
        public ScoreTypeBuiltin Bool { get; } = ScoreTypeBuiltin.Bool;
        public ScoreTypeBuiltin Float32 { get; } = ScoreTypeBuiltin.Float32;
        public ScoreTypeBuiltin Float64 { get; } = ScoreTypeBuiltin.Float64;
        public ScoreTypeBuiltin Int { get; } = new(ScoreBuiltinTypeKind.Bool, target.SizeOfPointer, target.AlignOfPointer);

        public ScoreTypeBuiltin BuiltinSizedInteger(int bitWidth)
        {
            if (!_builtinSizedIntegers.TryGetValue(bitWidth, out var builtinType))
                _builtinSizedIntegers[bitWidth] = builtinType = new(ScoreBuiltinTypeKind.IntSized, Size.FromBits(bitWidth), Align.ForBits(bitWidth));
            return builtinType;
        }
    }
}
