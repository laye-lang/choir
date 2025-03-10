namespace Choir.Formatting;

public enum MarkupSemantic
{
    Entity,
    EntityParameter,
    EntityLocal,
    EntityGlobal,
    EntityMember,
    EntityFunction,
    EntityType,
    EntityTypeValue,
    EntityTypeStruct,
    EntityTypeEnum,
    EntityNamespace,

    Keyword,
    KeywordControlFlow,
    KeywordOperator,
    KeywordType,
    KeywordQualifier,

    Literal,
    LiteralNumber,
    LiteralString,
    LiteralInvalid,
    LiteralKeyword,

    Punctuation,
    PunctuationDelimiter,
    PunctuationOperator,
}
