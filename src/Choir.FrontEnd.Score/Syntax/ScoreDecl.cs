using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreDecl
    : ScoreSyntaxNode
{
    public abstract SourceRange Range { get; }
}

public abstract class ScoreDeclNamed(ScoreSyntaxName name)
    : ScoreDecl
{
    public ScoreSyntaxName Name { get; set; } = name;
    public override SourceRange Range { get; } = name.Range;
}

public sealed class ScoreDeclFunc(ScoreToken funcKeywordToken, ScoreToken funcNameToken, ScoreToken openParenToken, ScoreToken closeParenToken)
    : ScoreDeclNamed(new ScoreSyntaxNameIdentifier(funcNameToken))
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

public class ScoreDeclFuncParam(ScoreToken paramNameToken, ScoreToken colonToken, ScoreTypeQual paramType)
    : ScoreDeclNamed(new ScoreSyntaxNameIdentifier(paramNameToken))
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
