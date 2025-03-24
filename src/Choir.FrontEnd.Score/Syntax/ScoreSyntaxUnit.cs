using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public sealed class ScoreSyntaxUnit(SourceText source, IReadOnlyList<ScoreToken> tokens, IReadOnlyList<ScoreSyntaxNode> topLevelNodes)
    : ScoreSyntaxNode(new())
{
    public SourceText Source { get; } = source;

    public IReadOnlyList<ScoreToken> Tokens { get; } = tokens;
    public IReadOnlyList<ScoreSyntaxNode> TopLevelNodes { get; } = topLevelNodes;

    public override IEnumerable<ScoreSyntaxNode> Children { get; } = topLevelNodes;
}

public sealed class ScoreSyntaxEndOfUnit(ScoreToken endOfFileToken)
    : ScoreSyntaxNode(endOfFileToken.Range)
{
    public ScoreToken EndOfFileToken { get; set; } = endOfFileToken;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [endOfFileToken];
}
