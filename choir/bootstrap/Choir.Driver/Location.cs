namespace Choir;

public struct Location(int offset, int length, int fileId)
{
    public int Offset = offset;
    public int Length = length;
    public readonly int FileId = fileId;

    public readonly bool Seekable(ChoirContext context)
    {
        var file = context.GetSourceFileById(FileId);
        if (file is null) return false;
        return Offset >= 0 && Length > 0 && Offset + Length <= file.Text.Length;
    }

    public readonly LocationInfo? Seek(ChoirContext context)
    {
        if (!Seekable(context)) return null;
        var file = context.GetSourceFileById(FileId);

        throw new NotImplementedException();
    }

    public readonly LocationInfoShort? SeekLineColumn(ChoirContext context)
    {
        if (!Seekable(context)) return null;
        var file = context.GetSourceFileById(FileId)!;

        int line = 1;
        int column = 1;

        string text = file.Text;
        for (int i = 0; i < Offset; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                column = 1;
            }
            else column++;
        }

        return new(line, column);
    }

    public readonly ReadOnlySpan<char> Span(ChoirContext context)
    {
        var file = context.GetSourceFileById(FileId);
        if (file is not null) return file.GetSpan(this);
        return "";
    }
}

public readonly struct LocationInfo(int line, int column, string lineText)
{
    public readonly int Line = line;
    public readonly int Column = column;
    public readonly string LineText = lineText;
}

public readonly struct LocationInfoShort(int line, int column)
{
    public readonly int Line = line;
    public readonly int Column = column;
}