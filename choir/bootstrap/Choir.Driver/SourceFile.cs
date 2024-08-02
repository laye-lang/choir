using System.Diagnostics;

namespace Choir;

public class SourceFile
{
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

        if (location.Length <= 0)
            return "";

        if (location.Offset < 0 || location.Offset + location.Length >= Text.Length)
            Context.Diag.ICE("Attempt to get a span outside the bounds of the file's source text.");
        
        return Text.AsSpan().Slice(location.Offset, location.Length);
    }
}
