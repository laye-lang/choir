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
    : ScoreSyntaxNode
{
    public SourceRange Range { get; } = range;

    // The name types *must* be hashable so we can look them up during semantic analysis at least somewhat efficiently.
    // Making GetHashCode abstract ensures every leaf type must implement it somewhere.
    // Note that this will be more complicated for some names, such as `operator cast(mut int)` since this will place a similar constraint on the type hierarchy as a whole.
    public abstract override int GetHashCode();
}

public sealed class ScoreSyntaxNameIdentifier(ScoreToken identifierToken, string spelling)
    : ScoreSyntaxName(identifierToken.Range)
{
    public ScoreToken IdentifierToken { get; } = identifierToken;
    public string Spelling { get; } = spelling;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [identifierToken];
    public override int GetHashCode() => Spelling.GetHashCode();
}

public sealed class ScoreSyntaxNameOperator(OverloadableOperator @operator, IReadOnlyList<ScoreToken> operatorTokens)
    : ScoreSyntaxName(new(operatorTokens[0].Range.Begin, operatorTokens[^1].Range.End))
{
    public OverloadableOperator Operator { get; } = @operator;
    public IReadOnlyList<ScoreToken> OperatorTokens { get; } = operatorTokens;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = operatorTokens;
    public override int GetHashCode() => HashCode.Combine(Operator);
}

public sealed class ScoreSyntaxNameOperatorCast(ScoreToken castKeywordToken, ScoreToken openParenToken, ScoreTypeQual castType, ScoreToken closeParenToken)
    : ScoreSyntaxName(new(castKeywordToken.Range.Begin, closeParenToken.Range.End))
{
    public ScoreToken CastKeywordToken { get; } = castKeywordToken;
    public ScoreToken OpenParenToken { get; } = openParenToken;
    public ScoreTypeQual CastType { get; } = castType;
    public ScoreToken CloseParenToken { get; } = closeParenToken;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [castKeywordToken, openParenToken, castType, closeParenToken];
    public override int GetHashCode() => HashCode.Combine(7, CastType);
}
