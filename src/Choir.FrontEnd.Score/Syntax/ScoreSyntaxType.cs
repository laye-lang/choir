﻿using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreSyntaxType(SourceRange range)
    : ScoreSyntaxNode(range)
{
}

public sealed class ScoreSyntaxTypeQual(ScoreSyntaxType? underlyingSyntaxType, SourceRange range = default)
    : ScoreSyntaxNode(underlyingSyntaxType?.Range ?? range)
{
    public ScoreSyntaxType? UnderlyingSyntaxType { get; set; } = underlyingSyntaxType;
    public ScoreToken? ReadAccessKeywordToken { get; set; }
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = underlyingSyntaxType is null ? [] : [underlyingSyntaxType];
}

public sealed class ScoreSyntaxTypeBuiltin(ScoreToken typeKeywordToken)
    : ScoreSyntaxType(typeKeywordToken.Range)
{
    public ScoreToken TypeKeywordToken { get; set; } = typeKeywordToken;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [typeKeywordToken];
}
