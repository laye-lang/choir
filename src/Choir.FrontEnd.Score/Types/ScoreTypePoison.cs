using Choir.Formatting;

namespace Choir.FrontEnd.Score.Types;

public sealed class ScoreTypePoison
    : ScoreType
{
    public static readonly ScoreTypePoison Instance = new();

    public override Size Size { get; } = Size.Zero;

    private ScoreTypePoison()
    {
    }

    public override void BuildSpelling(MarkupBuilder builder) => builder.Append(MarkupSemantic.KeywordType, "<poison>");
    public override int GetHashCode() => HashCode.Combine(17, Id);
    public override bool TypeEquals(ScoreType other, ScoreTypeComparison comp = ScoreTypeComparison.WithIdenticalQualifiers) => false;
}
