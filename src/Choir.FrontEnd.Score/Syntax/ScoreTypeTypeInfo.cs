using Choir.Formatting;

namespace Choir.FrontEnd.Score.Syntax;

public sealed class ScoreTypeTypeInfo
    : ScoreType
{
    public static readonly ScoreTypeTypeInfo Instance = new();

    private ScoreTypeTypeInfo()
    {
    }

    public override void BuildSpelling(MarkupBuilder builder) =>
        builder.Append(MarkupSemantic.KeywordType, "typeinfo");
}
