using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Choir.Diagnostics;
using Choir.Formatting;
using Choir.Source;

namespace Choir;

public class ChoirContext
{
    public DiagnosticEngine Diag { get; }
    public Target Target { get; }

    public ChoirContext(IDiagnosticConsumer diagConsumer, Target target)
    {
        Diag = new(diagConsumer);
        Target = target;
    }

    public void Assert([DoesNotReturnIf(false)] bool condition, string message,
        [CallerArgumentExpression(nameof(condition))] string conditionExpressionText = "")
    {
        if (condition) return;
        Diag.Emit(DiagnosticLevel.Fatal, $"Assertion failed: {message}\nCondition: {conditionExpressionText}");
        throw new UnreachableException();
    }

    public void Assert([DoesNotReturnIf(false)] bool condition, SourceText source, SourceLocation location, string message,
        [CallerArgumentExpression(nameof(condition))] string conditionExpressionText = "")
    {
        if (condition) return;
        Diag.Emit(DiagnosticLevel.Fatal, source, location, $"Assertion failed: {message}\nCondition: {conditionExpressionText}");
        throw new UnreachableException();
    }

    public void Assert([DoesNotReturnIf(false)] bool condition, Markup message,
        [CallerArgumentExpression(nameof(condition))] string conditionExpressionText = "")
    {
        if (condition) return;
        Diag.Emit(DiagnosticLevel.Fatal, new MarkupSequence(["Assertion failed: ", message, $"\nCondition: {conditionExpressionText}"]));
        throw new UnreachableException();
    }

    public void Assert([DoesNotReturnIf(false)] bool condition, SourceText source, SourceLocation location, Markup message,
        [CallerArgumentExpression(nameof(condition))] string conditionExpressionText = "")
    {
        if (condition) return;
        Diag.Emit(DiagnosticLevel.Fatal, source, location, new MarkupSequence(["Assertion failed: ", message, $"\nCondition: {conditionExpressionText}"]));
        throw new UnreachableException();
    }

    public void Assert([DoesNotReturnIf(false)] bool condition, MarkupInterpolatedStringHandler message,
        [CallerArgumentExpression(nameof(condition))] string conditionExpressionText = "")
    {
        if (condition) return;
        Diag.Emit(DiagnosticLevel.Fatal, new MarkupSequence(["Assertion failed: ", message.Markup, $"\nCondition: {conditionExpressionText}"]));
        throw new UnreachableException();
    }

    public void Assert([DoesNotReturnIf(false)] bool condition, SourceText source, SourceLocation location, MarkupInterpolatedStringHandler message,
        [CallerArgumentExpression(nameof(condition))] string conditionExpressionText = "")
    {
        if (condition) return;
        Diag.Emit(DiagnosticLevel.Fatal, source, location, new MarkupSequence(["Assertion failed: ", message.Markup, $"\nCondition: {conditionExpressionText}"]));
        throw new UnreachableException();
    }

    [DoesNotReturn]
    public void Todo(string message)
    {
        Diag.Emit(DiagnosticLevel.Fatal, $"TODO: {message}");
        throw new UnreachableException();
    }

    [DoesNotReturn]
    public void Todo(SourceText source, SourceLocation location, string message)
    {
        Diag.Emit(DiagnosticLevel.Fatal, source, location, $"TODO: {message}");
        throw new UnreachableException();
    }

    [DoesNotReturn]
    public void Todo(Markup message)
    {
        Diag.Emit(DiagnosticLevel.Fatal, new MarkupSequence(["TODO: ", message]));
        throw new UnreachableException();
    }

    [DoesNotReturn]
    public void Todo(SourceText source, SourceLocation location, Markup message)
    {
        Diag.Emit(DiagnosticLevel.Fatal, source, location, new MarkupSequence(["TODO: ", message]));
        throw new UnreachableException();
    }

    [DoesNotReturn]
    public void Todo(MarkupInterpolatedStringHandler message)
    {
        Diag.Emit(DiagnosticLevel.Fatal, new MarkupSequence(["TODO: ", message.Markup]));
        throw new UnreachableException();
    }

    [DoesNotReturn]
    public void Todo(SourceText source, SourceLocation location, MarkupInterpolatedStringHandler message)
    {
        Diag.Emit(DiagnosticLevel.Fatal, source, location, new MarkupSequence(["TODO: ", message.Markup]));
        throw new UnreachableException();
    }

    [DoesNotReturn]
    public void Unreachable([CallerFilePath] string callerFilePath = "", [CallerLineNumber] int callerLineNumber = 0)
    {
        Diag.Emit(DiagnosticLevel.Fatal, $"Reached unreachable code on line {callerLineNumber} in \"{callerFilePath}\".");
        throw new UnreachableException();
    }

    [DoesNotReturn]
    public void Unreachable(string message)
    {
        Diag.Emit(DiagnosticLevel.Fatal, message);
        throw new UnreachableException();
    }

    [DoesNotReturn]
    public void Unreachable(Markup message)
    {
        Diag.Emit(DiagnosticLevel.Fatal, message);
        throw new UnreachableException();
    }

    [DoesNotReturn]
    public void Unreachable(MarkupInterpolatedStringHandler message)
    {
        Diag.Emit(DiagnosticLevel.Fatal, message.Markup);
        throw new UnreachableException();
    }
}
