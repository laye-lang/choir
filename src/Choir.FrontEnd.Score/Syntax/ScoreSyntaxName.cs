using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public enum OverloadableOperator
{
    Bang,
    BangEqual,
    Percent,
    Ampersand,
    Star,
    Plus,
    Minus,
    Slash,
    Less,
    LessEqual,
    LessGreaterEqual,
    LessLess,
    EqualEqual,
    Greater,
    GreaterEqual,
    GreaterGreater,
    GreaterGreaterGreater,
    Pipe,
    Tilde,

    Call,
    True,
    False,
}

public abstract class ScoreSyntaxName(SourceRange range)
    : ScoreSyntaxNode(range)
{
}

public sealed class ScoreSyntaxNameIdentifier(ScoreToken identifierToken, string spelling)
    : ScoreSyntaxName(identifierToken.Range)
{
    public ScoreToken IdentifierToken { get; } = identifierToken;
    public string Spelling { get; } = spelling;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [identifierToken];
}

public sealed class ScoreSyntaxNameOperator(OverloadableOperator @operator, IReadOnlyList<ScoreToken> operatorTokens)
    : ScoreSyntaxName(new(operatorTokens[0].Range.Begin, operatorTokens[^1].Range.End))
{
    public OverloadableOperator Operator { get; } = @operator;
    public IReadOnlyList<ScoreToken> OperatorTokens { get; } = operatorTokens;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = operatorTokens;
}

public sealed class ScoreSyntaxNameOperatorCast(ScoreToken castKeywordToken, ScoreToken openParenToken, ScoreSyntaxTypeQual castType, ScoreToken closeParenToken)
    : ScoreSyntaxName(new(castKeywordToken.Range.Begin, closeParenToken.Range.End))
{
    public ScoreToken CastKeywordToken { get; } = castKeywordToken;
    public ScoreToken OpenParenToken { get; } = openParenToken;
    public ScoreSyntaxTypeQual CastType { get; } = castType;
    public ScoreToken CloseParenToken { get; } = closeParenToken;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [castKeywordToken, openParenToken, castType, closeParenToken];
}
