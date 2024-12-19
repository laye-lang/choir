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
    LessEqualGreater,
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
    //CaretEqual,

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
    Template,
    Module,
    Alias,
    Delegate,
    Test,
    Import,
    Export,
    From,
    As,
    Static,
    Strict,
    Operator,
    CFlags,

    Mut,
    New,
    Delete,
    Cast,
    Eval,
    Is,
    
    Sizeof,
    Alignof,
    Offsetof,
    Typeof,

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
        TokenKind.QuestionQuestion => 4,

        TokenKind.Or or TokenKind.Xor => 5,
        TokenKind.And => 6,

        TokenKind.EqualEqual or TokenKind.BangEqual => 10,

        TokenKind.Less or TokenKind.LessEqual or
        TokenKind.Greater or TokenKind.GreaterEqual or
        TokenKind.LessColon or TokenKind.LessEqualColon or
        TokenKind.ColonGreater or TokenKind.ColonGreaterEqual => 20,

        TokenKind.Ampersand or TokenKind.Pipe or TokenKind.Tilde or
        TokenKind.LessLess or TokenKind.GreaterGreater or
        TokenKind.GreaterGreaterGreater => 30,

        TokenKind.Plus or TokenKind.Minus or
        TokenKind.PlusPipe or TokenKind.MinusPipe or
        TokenKind.PlusPercent or TokenKind.MinusPercent => 40,

        TokenKind.Star or TokenKind.Slash or TokenKind.Percent or
        TokenKind.SlashColon or TokenKind.PercentColon => 50,

        //TokenKind.Caret => 60,

        _ => -1,
    };

    public static bool IsOverloadableOperatorKind(this TokenKind kind) => kind switch
    {
        TokenKind.LessEqualGreater or
        TokenKind.EqualEqual or TokenKind.BangEqual or
        TokenKind.Less or TokenKind.LessEqual or
        TokenKind.Greater or TokenKind.GreaterEqual or
        TokenKind.LessColon or TokenKind.LessEqualColon or
        TokenKind.ColonGreater or TokenKind.ColonGreaterEqual or
        TokenKind.Ampersand or TokenKind.Pipe or TokenKind.Tilde or
        TokenKind.LessLess or TokenKind.GreaterGreater or
        TokenKind.GreaterGreaterGreater or
        TokenKind.Plus or TokenKind.Minus or
        TokenKind.PlusPipe or TokenKind.MinusPipe or
        TokenKind.PlusPercent or TokenKind.MinusPercent or
        TokenKind.Star or TokenKind.Slash or TokenKind.Percent or
        TokenKind.SlashColon or TokenKind.PercentColon or
        //TokenKind.Caret => true,
        _ => false,
    };

    public static bool IsRightAssociativeBinaryOperator(this TokenKind kind) => kind switch
    {
        TokenKind.QuestionQuestion or TokenKind.Caret => true,
        _ => false,
    };

    public static bool IsAssignmentOperator(this TokenKind kind) => kind switch
    {
        TokenKind.Equal or
        TokenKind.TildeEqual or
        TokenKind.PercentEqual or
        TokenKind.PercentColonEqual or
        TokenKind.AmpersandEqual or
        TokenKind.StarEqual or
        TokenKind.MinusEqual or
        TokenKind.MinusPercentEqual or
        TokenKind.MinusPipeEqual or
        TokenKind.PlusEqual or
        TokenKind.PlusPercentEqual or
        TokenKind.PlusPipeEqual or
        TokenKind.PipeEqual or
        TokenKind.ColonGreaterEqual or
        TokenKind.LessLessEqual or
        TokenKind.GreaterGreaterEqual or
        TokenKind.GreaterGreaterGreaterEqual or
        TokenKind.SlashEqual or
        TokenKind.SlashColonEqual or
        TokenKind.QuestionQuestionEqual // or
        //TokenKind.CaretEqual
            => true,
        _ => false,
    };

    public static TokenKind GetOperatorFromAssignmentOperator(this TokenKind kind) => kind switch
    {
        TokenKind.TildeEqual => TokenKind.Tilde,
        TokenKind.BangEqual => TokenKind.Bang,
        TokenKind.PercentEqual => TokenKind.Percent,
        TokenKind.PercentColonEqual => TokenKind.PercentColon,
        TokenKind.AmpersandEqual => TokenKind.Ampersand,
        TokenKind.StarEqual => TokenKind.Star,
        TokenKind.MinusEqual => TokenKind.Minus,
        TokenKind.MinusPercentEqual => TokenKind.MinusPercent,
        TokenKind.MinusPipeEqual => TokenKind.MinusPipe,
        TokenKind.PlusEqual => TokenKind.Plus,
        TokenKind.PlusPercentEqual => TokenKind.PlusPercent,
        TokenKind.PlusPipeEqual => TokenKind.PlusPipe,
        TokenKind.PipeEqual => TokenKind.Pipe,
        TokenKind.ColonGreaterEqual => TokenKind.ColonGreater,
        TokenKind.LessLessEqual => TokenKind.LessLess,
        TokenKind.GreaterGreaterEqual => TokenKind.GreaterGreater,
        TokenKind.GreaterGreaterGreaterEqual => TokenKind.GreaterGreaterGreater,
        TokenKind.SlashEqual => TokenKind.Slash,
        TokenKind.SlashColonEqual => TokenKind.SlashColon,
        TokenKind.QuestionQuestionEqual => TokenKind.QuestionQuestion,
        //TokenKind.CaretEqual => TokenKind.Caret,
        _ => kind,
    };

    public static string CanonicalOperatorName(this TokenKind kind) => kind switch
    {
        TokenKind.Tilde => "~",
        TokenKind.Bang => "!",
        TokenKind.Percent => "%",
        TokenKind.Ampersand => "&",
        TokenKind.Star => "*",
        TokenKind.OpenParen => "(",
        TokenKind.CloseParen => ")",
        TokenKind.Minus => "-",
        TokenKind.Equal => "=",
        TokenKind.Plus => "+",
        TokenKind.OpenBracket => "[",
        TokenKind.CloseBracket => "]",
        TokenKind.OpenBrace => "{",
        TokenKind.CloseBrace => "}",
        TokenKind.Pipe => "|",
        TokenKind.SemiColon => ";",
        TokenKind.Colon => ":",
        TokenKind.Comma => ",",
        TokenKind.Less => "<",
        TokenKind.Greater => ">",
        TokenKind.Dot => ".",
        TokenKind.Slash => "/",
        TokenKind.Question => "?",
        //TokenKind.Caret => "^",

        TokenKind.TildeEqual => "~=",
        TokenKind.BangEqual => "!=",
        TokenKind.PercentEqual => "%=",
        TokenKind.PercentColon => "%:",
        TokenKind.PercentColonEqual => "%:=",
        TokenKind.AmpersandEqual => "%=",
        TokenKind.StarEqual => "*=",
        TokenKind.MinusEqual => "-=",
        TokenKind.MinusMinus => "--",
        TokenKind.MinusPercent => "-%",
        TokenKind.MinusPipe => "-|",
        TokenKind.MinusPercentEqual => "-%=",
        TokenKind.MinusPipeEqual => "-|=",
        TokenKind.EqualEqual => "==",
        TokenKind.EqualGreater => "=>",
        TokenKind.PlusEqual => "+=",
        TokenKind.PlusPlus => "++",
        TokenKind.PlusPercent => "+%",
        TokenKind.PlusPipe => "+|",
        TokenKind.PlusPercentEqual => "+%=",
        TokenKind.PlusPipeEqual => "+|=",
        TokenKind.PipeEqual => "|=",
        TokenKind.ColonColon => "::",
        TokenKind.ColonGreater => ":>",
        TokenKind.ColonGreaterEqual => ":>=",
        TokenKind.LessEqual => "<=",
        TokenKind.LessEqualGreater => "<=>",
        TokenKind.LessLess => "<<",
        TokenKind.LessLessEqual => "<<=",
        TokenKind.LessMinus => "<-",
        TokenKind.LessColon => "<:",
        TokenKind.LessEqualColon => "<=:",
        TokenKind.GreaterEqual => ">=",
        TokenKind.GreaterGreater => ">>",
        TokenKind.GreaterGreaterEqual => ">>=",
        TokenKind.GreaterGreaterGreater => ">>>",
        TokenKind.GreaterGreaterGreaterEqual => ">>>=",
        TokenKind.SlashEqual => "/=",
        TokenKind.SlashColon => "/:",
        TokenKind.SlashColonEqual => "/:=",
        TokenKind.QuestionQuestion => "??",
        TokenKind.QuestionQuestionEqual => "??=",
        //TokenKind.CaretEqual => "^=",

        _ => "",
    };
}
