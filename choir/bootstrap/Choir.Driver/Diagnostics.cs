using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Choir.CommandLine;

namespace Choir;

public enum DiagnosticKind
{
    Note,
    Warning,
    Error,
    ICE,
}

public static class DiagnosticKindExtensions
{
    public static string ToDiagnosticNameString(this DiagnosticKind kind) => kind switch
    {
        DiagnosticKind.Note => "Note",
        DiagnosticKind.Warning => "Warning",
        DiagnosticKind.Error => "Error",
        DiagnosticKind.ICE => "Internal Compiler Error",
        _ => throw new UnreachableException(),
    };
}

public enum DiagnosticLocationStyle
{
    LineColumn,
    ByteOffset,
}

public readonly struct DiagnosticInfo(DiagnosticKind kind, Location? location, string message,
    bool includeStackTrace = false, string? errorCode = null)
{
    public readonly DiagnosticKind Kind = kind;
    public readonly Location? Location = location;
    public readonly string Message = message;
    public readonly bool IncludeStackTrace = includeStackTrace;
    public readonly string? ErrorCode = errorCode;
}

public abstract class DiagnosticWriter(ChoirContext? context, bool? useColor = null)
{
    public ChoirContext? Context { get; } = context;
    public Colors Colors { get; } = new Colors(useColor ?? context?.UseColor ?? false);

    internal Action<DiagnosticKind>? OnIssue;

    public virtual void Flush()
    {
    }

    protected abstract void IssueInternal(DiagnosticKind kind, Location? location, string message, bool includeStackTrace);
    public void Issue(DiagnosticKind kind, Location? location, string message, [DoesNotReturnIf(true)] bool exit = false)
    {
        OnIssue?.Invoke(kind);
        IssueInternal(kind, location, message, includeStackTrace: exit);
        if (exit)
        {
            Flush();
            Environment.Exit(1);
        }
    }

    public void Note(string message) => Issue(DiagnosticKind.Note, null, message);
    public void Note(Location location, string message) => Issue(DiagnosticKind.Note, location, message);

    public void Warning(string message) => Issue(DiagnosticKind.Warning, null, message);
    public void Warning(Location location, string message) => Issue(DiagnosticKind.Warning, location, message);

    public void Error(string message) => Issue(DiagnosticKind.Error, null, message);
    public void Error(Location location, string message) => Issue(DiagnosticKind.Error, location, message);

    [DoesNotReturn]
    public void ICE(string message) => Issue(DiagnosticKind.ICE, null, message, true);
    [DoesNotReturn]
    public void ICE(Location location, string message) => Issue(DiagnosticKind.ICE, location, message, true);
}

public class StreamingDiagnosticWriter(ChoirContext? context = null, TextWriter? writer = null, bool? useColor = null)
    : DiagnosticWriter(context, useColor)
{
    private int _errorsWritten = 0;
    private bool _printedErrorLimitMessage = false;
    private bool _printed = false;

    private readonly List<DiagnosticInfo> _group = [];

    public TextWriter Writer { get; } = writer ?? Console.Error;

    public override void Flush()
    {
        if (_group.Count == 0) return;
        if (_printed) Writer.WriteLine();
        _printed = true;
        bool isConsole = Console.LargestWindowWidth != 0;
        string groupText = RenderDiagnosticGroup(_group, isConsole ? Math.Max(Console.WindowWidth, 80) : 80);
        Writer.Write(groupText);
        Writer.Write(Colors.Reset);
        _group.Clear();
    }

    protected (Rune[] Runes, int Columns) TakeColumns(ref Rune[] runes, int n)
    {
        const int TabSize = 4;

        List<Rune> buffer = [];
        int columns = 0;

        int i = 0;
        for (; i < runes.Length && columns < n; i++)
        {
            if (runes[i] == new Rune('\x1B'))
            {
                for (; i < runes.Length && runes[i] != new Rune('m'); i++){}
            }
            else if (runes[i] == new Rune('\t'))
            {
                columns += TabSize;
                for (int j = 0; j < TabSize; j++)
                    buffer.Add(new Rune(' '));
            }
            else if (runes[i] > new Rune(31))
            {
                columns += 1; // TODO(local): column count for utf-32 characters
            }

            buffer.Add(runes[i]);
        }

        runes = runes[i..];
        return ([.. buffer], columns);
    }

    protected int TextWidth(Rune[] runes)
    {
        Rune[] runes2 = [.. runes];
        return TakeColumns(ref runes2, int.MaxValue).Columns;
    }

    protected virtual string FormatDiagnostic(DiagnosticInfo diag, Location? previousLocation)
    {
        var builder = new StringBuilder();

        void PrintExtraData()
        {
        }

        void PrintStackTrace()
        {
            if (!diag.IncludeStackTrace) return;
        }

        if (Context is null || diag.Location is not {} diagLocation || diagLocation.Seek(Context) is not {} seekLocation)
        {
            if (Context is not null && diag.Location is not null && Context.GetSourceFileById(diag.Location.Value.FileId) is {} file)
                builder.AppendLine($"{Colors.White}{file.FileInfo.FullName}:");
            
            builder.AppendLine($"{Colors.ForDiagnostic(diag.Kind)}{diag.Kind.ToDiagnosticNameString()}: {Colors.Reset}{Colors.Default}{Colors.Bold}{diag.Message}{Colors.Reset}");
            PrintExtraData();
            PrintStackTrace();
            return builder.ToString();
        }

        int lineNumber = seekLocation.Line;
        int column = seekLocation.Column;
        int lineStart = seekLocation.LineStart;
        int lineLength = seekLocation.LineLength;
        string lineText = seekLocation.LineText;
        int columnOffset = column - 1;

        string before = columnOffset >= lineText.Length ? "" : lineText[..columnOffset];
        string range = columnOffset >= lineText.Length ? "" : lineText.Substring(columnOffset, Math.Min(diagLocation.Length, lineLength - columnOffset));
        string after = columnOffset + diagLocation.Length > lineLength
            ? ""
            : lineText[(columnOffset + diagLocation.Length)..];
        
        before = before.Replace("\t", "    ").TrimEnd('\n');
        range = range.Replace("\t", "    ").TrimEnd('\n');
        after = after.Replace("\t", "    ").TrimEnd('\n');

        builder.Append('\v');
        
        var locFile = Context.GetSourceFileById(diag.Location.Value.FileId)!;
        builder.Append(Colors.White);
        if (previousLocation is null || previousLocation.Value.FileId != diagLocation.FileId)
            builder.Append($"{locFile.FileInfo.FullName}:");
        
        builder.AppendLine($"{lineNumber}:{column}:");
        builder.Append($"{Colors.ForDiagnostic(diag.Kind)}{diag.Kind.ToDiagnosticNameString()}: ");
        builder.AppendLine($"{Colors.Reset}{Colors.Default}{Colors.Bold}{diag.Message}{Colors.Reset}").AppendLine();

        string lineNumberText = lineNumber.ToString();
        int digitCount = Math.Max(3, lineNumberText.Length);
        for (int i = 0; i < digitCount - lineNumberText.Length; i++)
            builder.Append(' ');
        builder.Append($"{lineNumber} │ {before}");
        builder.Append($"{Colors.ForDiagnostic(diag.Kind)}{range}{Colors.Reset}");
        builder.AppendLine(after);

        for (int i = 0; i < digitCount + 1; i++)
            builder.Append(' ');
        builder.Append("│ ");
        for (int i = 0, leadingSpaces = TextWidth([.. before.EnumerateRunes()]); i < leadingSpaces; i++)
            builder.Append(' ');
        
        builder.Append($"{Colors.ForDiagnostic(diag.Kind)}");
        for (int i = 0, squiggleCount = Math.Max(1, TextWidth([.. range.EnumerateRunes()])); i < squiggleCount; i++)
            builder.Append(i == 0 ? '^' : '~');

        builder.Append(Colors.Reset);

        PrintExtraData();
        PrintStackTrace();
        return builder.ToString();
    }

    protected virtual string RenderDiagnosticGroup(IReadOnlyList<DiagnosticInfo> group, int columns)
    {
        if (group.Count == 0)
        {
            ICE("Attempt to render a diagnostic group with 0 diagnostics.");
            throw new UnreachableException();
        }

        int columnsRem = columns - 3;

        var builder = new StringBuilder();

        bool isFirstLine = true;
        bool wasPrevMultiLine = false;

        Location? previousLocation = null;
        for (int diagIndex = 0, groupCount = group.Count; diagIndex < groupCount; diagIndex++)
        {
            if (diagIndex > 0)
                builder.AppendLine("│");
                
            var diag = group[diagIndex];
            string diagText = FormatDiagnostic(diag, previousLocation);
            string[] diagLines = diagText.TrimEnd('\n').Split('\n').Select(s => s.TrimEnd('\r')).ToArray();

            if (groupCount == 1 && diagLines.Length == 1 && diag.Location is null && !diagLines[0].ContainsAny('\v', '\f') && TextWidth([.. diagLines[0].EnumerateRunes()]) <= columnsRem)
            {
                builder.Append(Colors.Reset);
                builder.Append("── ");
                builder.Append(diagLines[0]);
                builder.AppendLine(Colors.Reset);
                break;
            }

            for (int i = 0, lineCount = diagLines.Length; i < lineCount; i++)
            {
                string line = diagLines[i];

                void EmitLeading(bool isLastLineSegment, bool isSegmentEmpty = false)
                {
                    string leading;
                    if (isLastLineSegment && diagIndex == groupCount - 1 && i == lineCount - 1)
                        leading = "╰─";
                    else
                    {
                        leading = isFirstLine ? "╭─" : i == 0 ? "├─" : diagIndex > 0 ? "┆ " : "│ ";
                        isFirstLine = false;
                    }

                    builder.Append(Colors.Reset);
                    builder.Append(leading);
                    if (!isSegmentEmpty) builder.Append(' ');
                    builder.Append(Colors.Reset);
                }

                bool addLine = line.StartsWith('\r');
                if (addLine && !builder.EndsWith("|\n"))
                {
                    line = line[1..];
                    EmitLeading(false, true);
                    builder.AppendLine();
                }

                if (wasPrevMultiLine && line.Length != 0 && !addLine)
                {
                    EmitLeading(false, true);
                    builder.AppendLine();
                }

                wasPrevMultiLine = false;

                if (line.ContainsAny('\v', '\f'))
                {
                    var utf32 = line.EnumerateRunes().ToArray();
                    if (utf32.Length < columnsRem || TextWidth(utf32) < columnsRem)
                    {
                        EmitLeading(true);
                        builder.AppendLine(line.Replace('\f', ' ').Replace("\v", ""));
                    }
                    else
                    {
                        int hang;
                        if (utf32.Contains(new Rune('\v')))
                        {
                            int vIndex = Array.IndexOf(utf32, new Rune('\v'));
                            var start = utf32.Take(vIndex).ToArray();
                            hang = TextWidth(start);

                            utf32 = utf32.Skip(vIndex + 1).ToArray();
                            EmitLeading(false);
                            builder.AppendRunes(start);
                        }
                        else
                        {
                            EmitLeading(false);
                            for (hang = 0; hang < utf32.Length && utf32[hang] != new Rune(' '); hang++)
                                builder.AppendRune(utf32[hang]);
                            utf32 = utf32.Skip(hang).ToArray();
                        }

                        var hangIndent = new Rune[hang];
                        for (int hi = 0; hi < hang; hi++)
                            hangIndent[hi] = new Rune(' ');
                        
                        void EmitRestOfLine(Rune[] restOfLine, Span<Rune[]> parts)
                        {
                        }

                        if (utf32.Contains(new Rune('\f')))
                        {
                            var parts = utf32.Split(new Rune('\f'));
                            EmitRestOfLine(parts[0], parts.AsSpan()[1..]);
                        }
                        else
                        {
                            int chunkSize = columnsRem - hang;
                            var first = TakeColumns(ref utf32, chunkSize).Runes;
                            var chunks = new List<Rune[]>(8);
                            while (utf32.Length > 0)
                            {
                                var chunk = TakeColumns(ref utf32, chunkSize).Runes;
                                chunks.Add(chunk);
                            }
                            EmitRestOfLine(first, chunks.ToArray());
                        }
                    }
                }
                else
                {
                    EmitLeading(true, line.Length == 0);
                    builder.AppendLine(line);
                }
            }

            previousLocation = diag.Location;
        }

        return builder.ToString();
    }

    protected override void IssueInternal(DiagnosticKind kind, Location? location, string message, bool includeStackTrace)
    {
        if (kind == DiagnosticKind.Error && Context is not null)
        {
            if (_errorsWritten > Context.ErrorLimit)
            {
                if (!_printedErrorLimitMessage)
                {
                    _printedErrorLimitMessage = true;
                    _group.Add(new DiagnosticInfo(DiagnosticKind.Error, null, $"Too many errors emitted (> {Context.ErrorLimit}). Further errors will not be shown.", false));
                    _group.Add(new DiagnosticInfo(DiagnosticKind.Note, null, $"Use '--error-limit <limit>' to show more errors.", false));
                    Flush();
                }

                return;
            }

            _errorsWritten++;
        }

        if (_group.Count > 0 && kind != DiagnosticKind.Note)
            Flush();

        _group.Add(new DiagnosticInfo(kind, location, message, includeStackTrace));
    }
}
