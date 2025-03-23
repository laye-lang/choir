using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreSyntaxDecl
    : ScoreSyntaxNode
{
    public abstract SourceRange Range { get; }
}

public abstract class ScoreSyntaxDeclNamed(ScoreSyntaxName name)
    : ScoreSyntaxDecl
{
    public ScoreSyntaxName Name { get; set; } = name;
    public override SourceRange Range { get; } = name.Range;
}

public sealed class ScoreSyntaxDeclFunc(ScoreToken funcKeywordToken, ScoreSyntaxName funcName,
    ScoreToken openParenToken, ScoreSyntaxDeclFuncParams declParams, ScoreToken closeParenToken)
    : ScoreSyntaxDeclNamed(funcName)
{
    public ScoreToken FuncKeywordToken { get; set; } = funcKeywordToken;
    public ScoreToken OpenParenToken { get; set; } = openParenToken;
    public ScoreSyntaxDeclFuncParams DeclParams { get; set; } = declParams;
    public ScoreToken CloseParenToken { get; set; } = closeParenToken;

    public override IEnumerable<ScoreSyntaxNode> Children
    {
        get
        {
            yield return FuncKeywordToken;
            yield return Name;
            yield return OpenParenToken;
            yield return DeclParams;
            yield return CloseParenToken;
        }
    }
}

public class ScoreSyntaxDeclFuncParams(List<ScoreSyntaxDeclFuncParam> declParams, List<ScoreToken> commaTokens)
    : ScoreSyntaxNode
{
    public List<ScoreSyntaxDeclFuncParam> DeclParams { get; set; } = declParams;
    public List<ScoreToken> CommaTokens { get; set; } = commaTokens;

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
