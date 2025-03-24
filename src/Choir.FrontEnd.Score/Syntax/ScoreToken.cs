using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public sealed class ScoreToken(ScoreTokenKind kind, SourceRange range, ScoreTriviaList leadingTrivia, ScoreTriviaList trailingTrivia)
    : ScoreSyntaxNode(range)
{
    public ScoreTokenKind Kind { get; } = kind;

    public ScoreTriviaList LeadingTrivia { get; } = leadingTrivia;
    public ScoreTriviaList TrailingTrivia { get; } = trailingTrivia;

    public override IEnumerable<ScoreSyntaxNode> Children
    {
        get
        {
            if (LeadingTrivia.Trivia.Count != 0)
                yield return LeadingTrivia;
            if (TrailingTrivia.Trivia.Count != 0)
                yield return TrailingTrivia;
        }
    }
}
