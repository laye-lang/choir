using System.Numerics;

namespace Choir.Front.Laye.Syntax;

public class SyntaxToken(TokenKind kind, Location location) : SyntaxNode(location)
{
    public TokenKind Kind { get; set; } = kind;
    public string TextValue { get; init; } = "";
    public BigInteger IntegerValue { get; init; } = BigInteger.Zero;
}
