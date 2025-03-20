using Choir.Source;

namespace Choir.FrontEnd.Score.Diagnostics;

public static class ScoreDiagnostic
{
    public static void ErrorUnexpectedCharacter(this ScoreContext context, SourceText source, SourceLocation location) =>
        context.EmitDiagnostic(ScoreDiagnosticSemantic.Error, "SC1001", source, location, [], $"Unexpected character.");

    public static void ErrorUnclosedComment(this ScoreContext context, SourceText source, SourceLocation location) =>
        context.EmitDiagnostic(ScoreDiagnosticSemantic.Error, "SC1002", source, location, [], $"Unclosed comment.");

    public static void ErrorInvalidBinaryOperands(this ScoreContext context, SourceText source, SourceLocation location) =>
        context.EmitDiagnostic(ScoreDiagnosticSemantic.Error, "SC3001", source, location, [], $"Invalid operands to binary expression ('left' and 'right').");
}
