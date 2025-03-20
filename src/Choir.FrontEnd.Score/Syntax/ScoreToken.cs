using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public sealed class ScoreToken(ScoreTokenKind kind, SourceRange range, ScoreTriviaList leadingTrivia, ScoreTriviaList trailingTrivia)
        : ScoreSyntaxNode
{
    public ScoreTokenKind Kind { get; set; } = kind;
    public SourceRange Range { get; set; } = range;

    public ScoreTriviaList LeadingTrivia { get; set; } = leadingTrivia;
    public ScoreTriviaList TrailingTrivia { get; set; } = trailingTrivia;

    public string? StringValue { get; set; }

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
