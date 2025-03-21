using Choir.FrontEnd.Score.Syntax;
using Choir.Source;

namespace Choir.FrontEnd.Score.Diagnostics;

public static class ScoreDiagnostic
{
    #region SC0XXX - Miscellaneous Tooling/Internal Diagnostics

    #endregion

    #region SC1XXX - Lexical Diagnostics

    public static void ErrorUnexpectedCharacter(this ScoreContext context, SourceText source, SourceLocation location) =>
        context.EmitDiagnostic(ScoreDiagnosticSemantic.Error, "SC1001", source, location, [], $"Unexpected character.");

    public static void ErrorUnclosedComment(this ScoreContext context, SourceText source, SourceLocation location) =>
        context.EmitDiagnostic(ScoreDiagnosticSemantic.Error, "SC1002", source, location, [], $"Unclosed comment.");

    public static void ErrorInvalidCharacterInNumberLiteral(this ScoreContext context, SourceText source, SourceLocation location) =>
        context.EmitDiagnostic(ScoreDiagnosticSemantic.Error, "SC1003", source, location, [], $"Invalid character in numeric literal.");

    #endregion

    #region SC2XXX - Syntactic Diagnostics

    #endregion

    #region SC3XXX - Semantic Diagnostics

    public static void ErrorInvalidBinaryOperands(this ScoreContext context, SourceText source, SourceLocation location, ScoreTypeQual left, ScoreTypeQual right) =>
        context.EmitDiagnostic(ScoreDiagnosticSemantic.Error, "SC3001", source, location, [left.Range, right.Range], $"Invalid operands to binary expression ('{left}' and '{right}').");

    #endregion
}
