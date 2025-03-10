using Choir.Diagnostics;

namespace Choir.FrontEnd.C.Diagnostics;

public static class CDiagnostic
{
    public static Diagnostic ErrorInvalidBinaryOperands(this DiagnosticEngine engine) =>
        engine.Emit(new());
}
