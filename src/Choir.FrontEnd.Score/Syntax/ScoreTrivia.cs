using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreTrivia(SourceRange range)
    : ScoreSyntaxNode
{
    public SourceRange Range { get; set; } = range;
}

public sealed class ScoreTriviaList(List<ScoreTrivia> trivia, bool isLeading)
    : ScoreSyntaxNode
{
    public List<ScoreTrivia> Trivia { get; set; } = trivia;
    public bool IsLeading { get; set; } = isLeading;
    public override IEnumerable<ScoreSyntaxNode> Children => Trivia;
}

public sealed class ScoreTriviaWhiteSpace(SourceRange range) : ScoreTrivia(range);
public sealed class ScoreTriviaNewLine(SourceRange range) : ScoreTrivia(range);
public sealed class ScoreTriviaShebangComment(SourceRange range) : ScoreTrivia(range);
public sealed class ScoreTriviaLineComment(SourceRange range) : ScoreTrivia(range);
public sealed class ScoreTriviaDelimitedComment(SourceRange range) : ScoreTrivia(range);
