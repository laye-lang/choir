using System.Numerics;

namespace Choir.Front.Laye.Syntax;

public class SyntaxToken(TokenKind kind, Location location) : SyntaxNode(location)
{
    public TokenKind Kind { get; set; } = kind;
    public string TextValue { get; init; } = "";
    public BigInteger IntegerValue { get; init; } = BigInteger.Zero;

    public void Dump(ChoirContext context, TextWriter writer)
    {
        var sourceFile = context.GetSourceFileById(Location.FileId);
        if (sourceFile is not null)
            writer.Write(sourceFile.FileInfo.FullName);
        else writer.Write("<no-source>");

        if (Location.SeekLineColumn(context) is { } locationInfo)
            writer.Write($"({locationInfo.Line}, {locationInfo.Column}): ");
        else writer.Write(": ");

        writer.Write(Kind);
        switch (Kind)
        {
            default:
            {
                if (sourceFile is not null)
                    writer.Write($" '{sourceFile.GetSpan(Location)}'");
            } break;

            case TokenKind.EndOfFile: break;
        }

        writer.WriteLine();
    }
}
