using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public sealed class ScoreSyntaxUnit(SourceText source, List<ScoreToken> tokens)
    : ScoreSyntaxNode
{
    public SourceText Source { get; set; } = source;

    public List<ScoreToken> Tokens { get; set; } = tokens;
    public List<ScoreSyntaxNode> TopLevelNodes { get; set; } = [];

    public override IEnumerable<ScoreSyntaxNode> Children
    {
        get
        {
            foreach (var child in TopLevelNodes)
                yield return child;
        }
    }
}

public sealed class ScoreSyntaxEndOfUnit(ScoreToken endOfFileToken)
    : ScoreSyntaxNode
{
    public ScoreToken EndOfFileToken { get; set; } = endOfFileToken;
    public override IEnumerable<ScoreSyntaxNode> Children => [EndOfFileToken];
}
