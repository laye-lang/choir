namespace Choir.Front.Laye.Syntax;

public enum TokenKind : ushort
{
    Invalid = 0,

    __PrintableTokenStart__ = 32,

    Tilde = '~',
    Bang = '!',
    Percent = '%',
    Ampersand = '&',
    Star = '*',
    OpenParen = '(',
    CloseParen = ')',
    Minus = '-',
    Equal = '=',
    Plus = '+',
    OpenBracket = '[',
    CloseBracket = ']',
    OpenBrace = '{',
    CloseBrace = '}',
    Pipe = '|',
    SemiColon = ';',
    Colon = ':',
    Comma = ',',
    Less = '<',
    Greater = '>',
    Dot = '.',
    Slash = '/',
    Question = '?',
    Caret = '^',
    
    __PrintableTokenEnd__ = 128,
    
    __MultiByteStart__ = 256,
    
    EndOfFile,
    Missing,

    Identifier,
    Global,

    LiteralInteger,
    LiteralFloat,
    LiteralString,
    LiteralRune,

    TildeEqual,
    BangEqual,
    PercentEqual,
    PercentColon,
    PercentColonEqual,
    AmpersandEqual,
    StarEqual,
    MinusEqual,
    MinusMinus,
    MinusPercent,
    MinusPipe,
    MinusPercentEqual,
    MinusPipeEqual,
    EqualEqual,
    EqualGreater,
    PlusEqual,
    PlusPlus,
    PlusPercent,
    PlusPipe,
    PlusPercentEqual,
    PlusPipeEqual,
    PipeEqual,
    ColonColon,
    ColonGreater,
    ColonGreaterEqual,
    LessEqual,
    LessLess,
    LessLessEqual,
    LessMinus,
    LessColon,
    LessEqualColon,
    GreaterEqual,
    GreaterGreater,
    GreaterGreaterEqual,
    GreaterGreaterGreater,
    GreaterGreaterGreaterEqual,
    SlashEqual,
    SlashColon,
    SlashColonEqual,
    QuestionQuestion,
    QuestionQuestionEqual,
    CaretEqual,

    Var,
    Void,
    NoReturn,
    Bool,
    BoolSized,
    Int,
    IntSized,
    FloatSized,

    BuiltinFFIBool,
    BuiltinFFIChar,
    BuiltinFFIShort,
    BuiltinFFIInt,
    BuiltinFFILong,
    BuiltinFFILongLong,
    BuiltinFFIFloat,
    BuiltinFFIDouble,
    BuiltinFFILongDouble,

    True,
    False,
    Nil,

    If,
    Else,
    For,
    While,
    Do,
    Switch,
    Case,
    Default,
    Return,
    Break,
    Continue,
    Fallthrough,
    Yield,
    Unreachable,

    Defer,
    Discard,
    Goto,
    Xyzzy,
    Assert,
    Try,
    Catch,

    Struct,
    Variant,
    Enum,
    //TokenStrict,
    Template,
    Module,
    Alias,
    Delegate,
    Test,
    Import,
    Export,
    From,
    As,
    Operator,

    Mut,
    New,
    Delete,
    Cast,
    Eval,
    Is,
    
    Sizeof,
    Alignof,
    Offsetof,

    Not,
    And,
    Or,
    Xor,

    Varargs,
    Const,
    Foreign,
    Inline,
    Callconv,
    Pure,
    Discardable,
}

public static class TokenKindExtensions
{
    public static bool CanBeBinaryOperator(this TokenKind kind) => kind.GetBinaryOperatorPrecedence() >= 0;
    public static int GetBinaryOperatorPrecedence(this TokenKind kind) => kind switch
    {
        TokenKind.Or or TokenKind.Xor => 5,
        TokenKind.And => 6,

        TokenKind.EqualEqual or TokenKind.BangEqual => 10,

        TokenKind.Less or TokenKind.LessEqual or
        TokenKind.Greater or TokenKind.GreaterEqual => 20,

        TokenKind.Ampersand or TokenKind.Pipe or TokenKind.Tilde or
        TokenKind.LessLess or TokenKind.GreaterGreater or
        TokenKind.GreaterGreaterGreater => 30,

        TokenKind.Plus or TokenKind.Minus => 40,

        TokenKind.Star or TokenKind.Slash or TokenKind.Percent => 40,

        _ => -1,
    };
}
