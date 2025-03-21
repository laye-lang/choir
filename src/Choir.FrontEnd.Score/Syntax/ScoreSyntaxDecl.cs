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

public sealed class ScoreSyntaxDeclFunc(ScoreToken funcKeywordToken, ScoreSyntaxName funcName, ScoreToken openParenToken, ScoreToken closeParenToken)
    : ScoreSyntaxDeclNamed(funcName)
{
    public ScoreToken FuncKeywordToken { get; set; } = funcKeywordToken;
    public ScoreToken OpenParenToken { get; set; } = openParenToken;
    public ScoreToken CloseParenToken { get; set; } = closeParenToken;

    public override IEnumerable<ScoreSyntaxNode> Children
    {
        get
        {
            yield return FuncKeywordToken;
            yield return Name;
            yield return OpenParenToken;
            yield return CloseParenToken;
        }
    }
}

public class ScoreSyntaxDeclFuncParam(ScoreSyntaxName paramName, ScoreToken colonToken, ScoreTypeQual paramType)
    : ScoreSyntaxDeclNamed(paramName)
{
    public ScoreToken ColonToken { get; set; } = colonToken;
    public ScoreTypeQual ParamType { get; set; } = paramType;

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
