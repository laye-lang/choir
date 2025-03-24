using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreSyntaxType(SourceRange range)
    : ScoreSyntaxNode(range)
{
}

public sealed class ScoreSyntaxTypeQual(ScoreSyntaxType? underlyingSyntaxType, SourceRange range = default)
    : ScoreSyntaxNode(underlyingSyntaxType?.Range ?? range)
{
    public ScoreSyntaxType? UnderlyingSyntaxType { get; } = underlyingSyntaxType;
    public ScoreToken? ReadAccessKeywordToken { get; init; }
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = underlyingSyntaxType is null ? [] : [underlyingSyntaxType];
}

public sealed class ScoreSyntaxTypeBuiltin(ScoreToken typeKeywordToken)
    : ScoreSyntaxType(typeKeywordToken.Range)
{
    public ScoreToken TypeKeywordToken { get; } = typeKeywordToken;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [typeKeywordToken];
}

public sealed class ScoreSyntaxTypeBuffer(ScoreToken openSquareToken, ScoreToken starToken, ScoreToken closeSquareToken, ScoreSyntaxTypeQual elementType)
    : ScoreSyntaxType(new(openSquareToken.Range.Begin, elementType.Range.End))
{
    public ScoreToken OpenSquareToken { get; } = openSquareToken;
    public ScoreToken StarToken { get; } = starToken;
    public ScoreToken CloseSquareToken { get; } = closeSquareToken;
    public ScoreSyntaxTypeQual ElementType { get; } = elementType;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [openSquareToken, starToken, closeSquareToken, elementType];
}
