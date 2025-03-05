using Choir.Diagnostics;
using Choir.FrontEnd.Laye.Semantics;
using Choir.Source;

namespace Choir.FrontEnd.Laye.Diagnostics;

public static class LayeDiagnostic
{
    public static Diagnostic ErrorInvalidBinaryOperands(SourceText source, SourceLocation location, LayeTypeQual left, LayeTypeQual right) =>
        new(DiagnosticLevel.Error, "LY3001", source, location, [left.Range, right.Range], $"Invalid operands to binary expression ('{left}' and '{right}').");
}
