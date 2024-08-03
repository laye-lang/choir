namespace Choir;

public struct Location(int offset, int length, int fileId)
{
    public int Offset = offset;
    public int Length = length;
    public readonly int FileId = fileId;

    public readonly LocationInfo Seek(ChoirContext context) => context.Seek(this);
    public readonly ReadOnlySpan<char> Span(ChoirContext context) => context.GetSourceFileById(FileId).GetSpan(this);
}

public readonly struct LocationInfo
{
    public readonly int Line;
    public readonly int Column;
}
