namespace Choir.Source;

public readonly struct SourceRange
    : IComparable<SourceRange>
{
    public readonly SourceLocation Begin;
    public readonly SourceLocation End;

    public int Length => End - Begin;

    public SourceRange(SourceLocation begin, SourceLocation end)
    {
        if (begin > end)
        {
            (end, begin) = (begin, end);
        }

        Begin = begin;
        End = end;
    }

    public SourceRange FullRange(SourceRange other) => new(SourceLocation.Min(Begin, other.Begin), SourceLocation.Max(End, other.End));

    public override string ToString() => $"{nameof(SourceRange)}({Begin.Offset}, {End.Offset})";
    public override int GetHashCode() => HashCode.Combine(Begin.Offset, End.Offset);

    public int CompareTo(SourceRange other) => Begin.Offset.CompareTo(other.Begin.Offset);
}
