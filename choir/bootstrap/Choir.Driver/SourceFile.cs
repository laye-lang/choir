namespace Choir;

public class SourceFile : IEquatable<SourceFile>
{
    public static bool operator ==(SourceFile a, SourceFile b) =>  a.Equals(b);
    public static bool operator !=(SourceFile a, SourceFile b) => !a.Equals(b);

    public ChoirContext Context { get; }

    public FileInfo FileInfo { get; }
    public int FileId { get; }
    public string Text { get; }
    
    internal SourceFile(ChoirContext context, FileInfo fileInfo, int fileId, string text)
    {
        Context = context;
        FileInfo = fileInfo;
        FileId = fileId;
        Text = text;
    }

    public ReadOnlySpan<char> GetSpan(Location location)
    {
        if (location.FileId != FileId)
            Context.Diag.ICE("Attempt to get the span of a location in the wrong file.");

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
