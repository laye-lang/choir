using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreSyntaxDecl
    : ScoreSyntaxNode
{
}

public abstract class ScoreSyntaxDeclNamed(ScoreSyntaxName name)
    : ScoreSyntaxDecl
{
    public ScoreSyntaxName Name { get; set; } = name;
    public override SourceRange Range { get; } = name.Range;
}

public sealed class ScoreSyntaxDeclFunc(ScoreToken funcKeywordToken, ScoreSyntaxName funcName,
    ScoreToken openParenToken, ScoreSyntaxDeclFuncParams declParams, ScoreToken closeParenToken,
    ScoreToken? arrowToken, ScoreSyntaxTypeQual? returnType, ScoreSyntaxExpr funcBody)
    : ScoreSyntaxDeclNamed(funcName)
{
    public ScoreToken FuncKeywordToken { get; set; } = funcKeywordToken;
    public ScoreToken OpenParenToken { get; set; } = openParenToken;
    public ScoreSyntaxDeclFuncParams DeclParams { get; set; } = declParams;
    public ScoreToken CloseParenToken { get; set; } = closeParenToken;
    public ScoreToken? ArrowToken { get; set; } = arrowToken;
    public ScoreSyntaxTypeQual? ReturnType { get; set; } = returnType;
    public ScoreSyntaxExpr FuncBody { get; set; } = funcBody;

    public ScoreSyntaxDeclFunc(ScoreToken funcKeywordToken, ScoreSyntaxName funcName, ScoreToken openParenToken,
        ScoreSyntaxDeclFuncParams declParams, ScoreToken closeParenToken, ScoreSyntaxExpr funcBody)
        : this(funcKeywordToken, funcName, openParenToken, declParams, closeParenToken, null, null, funcBody)
    { 
    }

    public override IEnumerable<ScoreSyntaxNode> Children
    {
        get
        {
            yield return FuncKeywordToken;
            yield return Name;
            yield return OpenParenToken;
            yield return DeclParams;
            yield return CloseParenToken;
            if (ArrowToken is not null)
                yield return ArrowToken;
            if (ReturnType is not null)
                yield return ReturnType;
            yield return FuncBody;
        }
    }
}

public class ScoreSyntaxDeclFuncParams(List<ScoreSyntaxDeclFuncParam> declParams, List<ScoreToken> commaTokens)
    : ScoreSyntaxNode
{
    public List<ScoreSyntaxDeclFuncParam> DeclParams { get; set; } = declParams;
    public List<ScoreToken> CommaTokens { get; set; } = commaTokens;

    public override SourceRange Range => DeclParams.Count == 0 ? new() :
        new(DeclParams[0].Range.Begin, DeclParams[^1].Range.End);

    public override IEnumerable<ScoreSyntaxNode> Children => DeclParams
        .Select(p => (p.Range, Node: (ScoreSyntaxNode)p))
        .Concat(CommaTokens.Select(p => (p.Range, Node: (ScoreSyntaxNode)p)))
        .OrderBy(pair => pair.Range)
        .Select(pair => pair.Node);
}

public class ScoreSyntaxDeclFuncParam(ScoreSyntaxName paramName, ScoreToken colonToken, ScoreSyntaxTypeQual paramType)
    : ScoreSyntaxDeclNamed(paramName)
{
    public ScoreToken ColonToken { get; set; } = colonToken;
    public ScoreSyntaxTypeQual ParamType { get; set; } = paramType;

    public override IEnumerable<ScoreSyntaxNode> Children
    {
        get
        {
            yield return Name;
            yield return ColonToken;
            yield return ParamType;
        }
    }
}

public sealed class ScoreSyntaxFuncBodyEmpty(ScoreToken semiColonToken)
    : ScoreSyntaxNode
{
    public ScoreToken SemiColonToken { get; set; } = semiColonToken;
    public override SourceRange Range => SemiColonToken.Range;
}
