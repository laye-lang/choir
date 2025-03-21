namespace Choir.Source;

public sealed class SourceText(string name, string text)
    : IEquatable<SourceText>
{
    private static int _counter = 0;

    public readonly int Id = Interlocked.Increment(ref _counter);
    public readonly string Name = name;
    public readonly string Text = text;
    public readonly int Length = text.Length;

    public override string ToString() => $"[{Id}] \"{Name}\"";
    public override int GetHashCode() => HashCode.Combine(Id);
    public override bool Equals(object? obj) => Equals(obj as SourceText);
    public bool Equals(SourceText? other) => other is not null && Id == other.Id;

    public string GetTextInRange(SourceRange range)
    {
        return Text[range.Begin.Offset..range.End.Offset];
    }
}
