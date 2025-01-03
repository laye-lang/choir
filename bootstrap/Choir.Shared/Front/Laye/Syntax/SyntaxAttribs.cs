
namespace Choir.Front.Laye.Syntax;

public abstract class SyntaxAttrib(Location location) : SyntaxNode(location);

public sealed class SyntaxAttribForeign(SyntaxToken tokenForeign, SyntaxToken? tokenName)
    : SyntaxAttrib(tokenForeign.Location)
{
    public SyntaxToken TokenForegin { get; } = tokenForeign;
    public SyntaxToken? TokenName { get; } = tokenName;
    public bool HasForeignName => TokenName is not null;
    public string? ForeignNameText => TokenName?.TextValue;
    public override IEnumerable<SyntaxNode> Children { get; } = tokenName is not null ? [tokenForeign, tokenName] : [tokenForeign];
}

public sealed class SyntaxAttribCallconv(SyntaxToken tokenCallconv, SyntaxToken tokenKind)
    : SyntaxAttrib(tokenCallconv.Location)
{
    public SyntaxToken TokenForegin { get; } = tokenCallconv;
    public SyntaxToken TokenKind { get; } = tokenKind;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenCallconv, tokenKind];
}

public sealed class SyntaxAttribExport(SyntaxToken tokenExport)
    : SyntaxAttrib(tokenExport.Location)
{
    public SyntaxToken TokenForegin { get; } = tokenExport;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenExport];
}

public sealed class SyntaxAttribInline(SyntaxToken tokenInline)
    : SyntaxAttrib(tokenInline.Location)
{
    public SyntaxToken TokenForegin { get; } = tokenInline;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenInline];
}

public sealed class SyntaxAttribDiscardable(SyntaxToken tokenDiscardable)
    : SyntaxAttrib(tokenDiscardable.Location)
{
    public SyntaxToken TokenForegin { get; } = tokenDiscardable;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenDiscardable];
}