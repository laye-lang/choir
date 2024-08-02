namespace Choir;

public struct Location
{
    public int Offset;
    public short Length;
    public readonly short FileId;

    public readonly LocationInfo Seek(ChoirContext context) => context.Seek(this);
    public readonly ReadOnlySpan<char> Span(ChoirContext context) => context.GetSourceFileById(FileId).GetSpan(this);
}

public readonly struct LocationInfo
{
    public readonly int Line;
    public readonly int Column;
}
