namespace Choir;

public class SourceFile : IEquatable<SourceFile>
{
    public static bool operator ==(SourceFile a, SourceFile b) =>  a.Equals(b);
    public static bool operator !=(SourceFile a, SourceFile b) => !a.Equals(b);

    public ChoirContext Context { get; }

    public string FilePath { get; }
    public int FileId { get; }
    public string Text { get; }
    public bool IsTextless { get; }
    
    internal SourceFile(ChoirContext context, string filePath, int fileId, string text, bool isTextless)
    {
        Context = context;
        FilePath = filePath;
        FileId = fileId;
        Text = text;
        IsTextless = isTextless;
    }

    public ReadOnlySpan<char> GetSpan(Location location)
    {
        if (location.FileId != FileId)
            Context.Diag.ICE("Attempt to get the span of a location in the wrong file.");

        if (IsTextless)
            Context.Diag.ICE("Attempt to get the span of a location in a file with no text (for example, a deserialized module).");

        if (location.Length <= 0 || location.Offset == Text.Length && location.Length == 1)
            return "";

        if (location.Offset < 0 || location.Offset + location.Length > Text.Length)
            Context.Diag.ICE("Attempt to get a span outside the bounds of the file's source text.");
        
        return Text.AsSpan().Slice(location.Offset, location.Length);
    }

    public override int GetHashCode() => FileId.GetHashCode();
    public override bool Equals(object? obj) => obj is SourceFile other && Equals(other);
    public bool Equals(SourceFile? other) => ReferenceEquals(other, this);
}
