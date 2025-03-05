using Choir.Formatting;

namespace Choir.FrontEnd.Laye.Semantics;

public sealed class LayeTypeTypeInfo
    : LayeType
{
    public static readonly LayeTypeTypeInfo Instance = new();

    private LayeTypeTypeInfo()
    {
    }

    public override void BuildSpelling(MarkupBuilder builder) =>
        builder.Append(MarkupSemantic.KeywordType, "typeinfo");
}
