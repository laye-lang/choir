using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreSyntaxDecl(SourceRange range)
    : ScoreSyntaxNode(range)
{
}

public abstract class ScoreSyntaxDeclNamed(ScoreSyntaxName name)
    : ScoreSyntaxDecl(name.Range)
{
    public ScoreSyntaxName Name { get; } = name;
}

public sealed class ScoreSyntaxDeclFunc(ScoreToken funcKeywordToken, ScoreSyntaxName funcName,
    ScoreToken openParenToken, ScoreSyntaxDeclFuncParams declParams, ScoreToken closeParenToken,
    ScoreToken? arrowToken, ScoreSyntaxTypeQual? returnType, ScoreSyntaxFuncBody funcBody)
    : ScoreSyntaxDeclNamed(funcName)
{
    public ScoreToken FuncKeywordToken { get; } = funcKeywordToken;
    public ScoreToken OpenParenToken { get; } = openParenToken;
    public ScoreSyntaxDeclFuncParams DeclParams { get; } = declParams;
    public ScoreToken CloseParenToken { get; } = closeParenToken;
    public ScoreToken? ArrowToken { get; } = arrowToken;
    public ScoreSyntaxTypeQual? ReturnType { get; } = returnType;
    public ScoreSyntaxFuncBody FuncBody { get; } = funcBody;

    public ScoreSyntaxDeclFunc(ScoreToken funcKeywordToken, ScoreSyntaxName funcName, ScoreToken openParenToken,
        ScoreSyntaxDeclFuncParams declParams, ScoreToken closeParenToken, ScoreSyntaxFuncBody funcBody)
        : this(funcKeywordToken, funcName, openParenToken, declParams, closeParenToken, null, null, funcBody)
    { 
    }

    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [funcKeywordToken, funcName, openParenToken, declParams, closeParenToken,
        .. new ScoreSyntaxNode?[] { arrowToken, returnType }.Where(n => n is not null).Cast<ScoreSyntaxNode>(), funcBody];
}

public class ScoreSyntaxDeclFuncParams(List<ScoreSyntaxDeclFuncParam> declParams, List<ScoreToken> commaTokens)
    : ScoreSyntaxNode(declParams.Count == 0 ? new() : new(declParams[0].Range.Begin, declParams[^1].Range.End))
{
    public List<ScoreSyntaxDeclFuncParam> DeclParams { get; } = declParams;
    public List<ScoreToken> CommaTokens { get; } = commaTokens;

    public override IEnumerable<ScoreSyntaxNode> Children { get; } = declParams
        .Select(p => (p.Range, Node: (ScoreSyntaxNode)p))
        .Concat(commaTokens.Select(p => (p.Range, Node: (ScoreSyntaxNode)p)))
        .OrderBy(pair => pair.Range)
        .Select(pair => pair.Node);
}

public class ScoreSyntaxDeclFuncParam(ScoreSyntaxName paramName, ScoreToken colonToken, ScoreSyntaxTypeQual paramType)
    : ScoreSyntaxDeclNamed(paramName)
{
    public ScoreToken ColonToken { get; } = colonToken;
    public ScoreSyntaxTypeQual ParamType { get; } = paramType;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [paramName, colonToken, paramType];
}

public abstract class ScoreSyntaxFuncBody(SourceRange range)
    : ScoreSyntaxNode(range)
{
}

public sealed class ScoreSyntaxFuncBodyEmpty(ScoreToken semiColonToken)
    : ScoreSyntaxFuncBody(semiColonToken.Range)
{
    public ScoreToken SemiColonToken { get; } = semiColonToken;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [semiColonToken];
}

public sealed class ScoreSyntaxFuncBodyImplicitReturn(ScoreToken equalToken, ScoreSyntaxExpr returnValue)
    : ScoreSyntaxFuncBody(returnValue.Range)
{
    public ScoreToken EqualToken { get; } = equalToken;
    public ScoreSyntaxExpr ReturnValue { get; } = returnValue;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [equalToken, returnValue];
}

public sealed class ScoreSyntaxFuncBodyCompound(ScoreSyntaxExprCompound compound)
    : ScoreSyntaxFuncBody(compound.Range)
{
    public ScoreSyntaxExprCompound Compound { get; } = compound;
    public override IEnumerable<ScoreSyntaxNode> Children { get; } = [compound];
}
