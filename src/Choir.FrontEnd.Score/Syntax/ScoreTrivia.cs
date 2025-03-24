using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreTrivia(SourceRange range)
    : ScoreSyntaxNode(range)
{
}

public sealed class ScoreTriviaList(IReadOnlyList<ScoreTrivia> trivia, bool isLeading)
    : ScoreSyntaxNode(trivia.Count == 0 ? new() : new(trivia[0].Range.Begin, trivia[^1].Range.End))
{
    public IReadOnlyList<ScoreTrivia> Trivia { get; } = trivia;
    public bool IsLeading { get; set; } = isLeading;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = trivia;
}

public sealed class ScoreTriviaWhiteSpace(SourceRange range) : ScoreTrivia(range);
public sealed class ScoreTriviaNewLine(SourceRange range) : ScoreTrivia(range);
public sealed class ScoreTriviaShebangComment(SourceRange range) : ScoreTrivia(range);
public sealed class ScoreTriviaLineComment(SourceRange range) : ScoreTrivia(range);
public sealed class ScoreTriviaDelimitedComment(SourceRange range) : ScoreTrivia(range);
