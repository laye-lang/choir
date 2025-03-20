using Choir.Diagnostics;
using Choir.FrontEnd.Score.Syntax;
using Choir.Source;

namespace Choir.FrontEnd.Score.Diagnostics;

public static class LayeDiagnostic
{
    public static Diagnostic ErrorInvalidBinaryOperands(this LayeContext context, SourceText source, SourceLocation location, LayeTypeQual left, LayeTypeQual right) =>
        context.EmitDiagnostic(LayeDiagnosticSemantic.Error, "LY3001", source, location, [left.Range, right.Range], $"Invalid operands to binary expression ('{left}' and '{right}').");
}
