using System.Diagnostics;
using System.Text;
using Choir.CommandLine;

namespace Choir;

public enum DiagnosticKind
{
    Note,
    Warning,
    Error,
    Fatal,
    ICE,
}

public enum DiagnosticLocationStyle
{
    LineColumn,
    ByteOffset,
}

public abstract class DiagnosticWriter(ChoirContext? context, bool? useColor = null)
{
    public ChoirContext? Context { get; } = context;
    public Colors Colors { get; } = new Colors(useColor ?? context?.UseColor ?? false);

    internal Action<DiagnosticKind>? OnIssue;

    protected abstract void IssueInternal(DiagnosticKind kind, Location? location, string message);
    public void Issue(DiagnosticKind kind, Location? location, string message)
    {
        OnIssue?.Invoke(kind);
        IssueInternal(kind, location, message);

        if (kind >= DiagnosticKind.Fatal)
            Environment.Exit(1);
    }

    public void Note(string message) => Issue(DiagnosticKind.Note, null, message);
    public void Note(Location location, string message) => Issue(DiagnosticKind.Note, location, message);

    public void Warning(string message) => Issue(DiagnosticKind.Warning, null, message);
    public void Warning(Location location, string message) => Issue(DiagnosticKind.Warning, location, message);

    public void Error(string message) => Issue(DiagnosticKind.Error, null, message);
    public void Error(Location location, string message) => Issue(DiagnosticKind.Error, location, message);

    public void Fatal(string message) => Issue(DiagnosticKind.Fatal, null, message);
    public void Fatal(Location location, string message) => Issue(DiagnosticKind.Fatal, location, message);

    public void ICE(string message) => Issue(DiagnosticKind.ICE, null, message);
    public void ICE(Location location, string message) => Issue(DiagnosticKind.ICE, location, message);
}

public class StreamingDiagnosticWriter(ChoirContext? context = null, TextWriter? writer = null, bool? useColor = null) : DiagnosticWriter(context, useColor)
{
    private int _errorsWritten = 0;
    private bool _printedErrorLimitMessage = false;
    private bool _printed = false;

    public TextWriter Writer { get; } = writer ?? Console.Error;

    protected void WriteKind(DiagnosticKind kind)
    {
        switch (kind)
        {
            case DiagnosticKind.Note: Writer.Write($"{Colors.White}note"); break;
            case DiagnosticKind.Warning: Writer.Write($"{Colors.Yellow}warning"); break;
            case DiagnosticKind.Error: Writer.Write($"{Colors.Red}error"); break;
            case DiagnosticKind.Fatal: Writer.Write($"{Colors.Magenta}fatal"); break;
            case DiagnosticKind.ICE: Writer.Write($"{Colors.Cyan}internal compiler error"); break;
        }
    }

    protected override void IssueInternal(DiagnosticKind kind, Location? location, string message)
    {
        if (kind == DiagnosticKind.Error && Context is not null && _errorsWritten > Context.ErrorLimit)
        {
            if (!_printedErrorLimitMessage)
            {
                _printedErrorLimitMessage = true;
                Writer.WriteLine($"choir: {Colors.Red}error: {Colors.White}too many errors emitted (> {Context.ErrorLimit}). further errors will not be shown.{Colors.Reset}");
                Writer.WriteLine($"choir: {Colors.White}note: {Colors.White}use '--error-limit <limit>' to show more errors.{Colors.Reset}");
            }

            return;
        }

        if (_printed && kind != DiagnosticKind.Note && Context is not null)
            Writer.WriteLine();

        _printed = true;

        if (location is not null && Context is null)
        {
            ICE("Attempt to issue a diagnostic with location information when no context is present.");
            throw new UnreachableException();
        }

        Writer.Write(Colors.Reset);

        if (location is {} loc && loc.Seekable(Context!))
        {
            Debug.Assert(Context is not null);

            var sourceFile = Context.GetSourceFileById(loc.FileId);
            Writer.Write($"{sourceFile!.FileInfo.FullName}");

            if (Context.DiagnosticLocationStyle == DiagnosticLocationStyle.LineColumn)
            {
                var locInfo = loc.SeekLineColumn(Context)!.Value;
                Writer.Write($"({locInfo.Line}:{locInfo.Column}): ");
            }
            else
            {
                int byteOffset = Encoding.UTF8.GetByteCount(sourceFile.Text.AsSpan().Slice(0, loc.Offset));
                Writer.Write($"[{byteOffset}]: ");
            }
        }
        else
        {
            Writer.Write("choir: ");
        }
        
        WriteKind(kind);
        Writer.Write($"{Colors.Reset}: ");

        Writer.WriteLine($"{Colors.White}{message}{Colors.Reset}");

        // TODO(local): write the relevant source text, if any
    }
}
