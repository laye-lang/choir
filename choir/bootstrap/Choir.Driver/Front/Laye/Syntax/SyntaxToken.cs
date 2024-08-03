using System.Numerics;

namespace Choir.Front.Laye.Syntax;

public class SyntaxToken(SyntaxKind kind, Location location) : SyntaxNode(kind, location)
{
    public string TextValue { get; init; } = "";
    public BigInteger IntegerValue { get; init; } = BigInteger.Zero;
}
