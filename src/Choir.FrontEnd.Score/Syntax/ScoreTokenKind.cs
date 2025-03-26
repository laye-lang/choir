﻿namespace Choir.FrontEnd.Score.Syntax;

public enum ScoreTokenKind
    : ushort
{
    Invalid = 0,

    Bang = '!',
    Pound = '#',
    Dollar = '$',
    Percent = '%',
    Ampersand = '&',
    OpenParen = '(',
    CloseParen = ')',
    Star = '*',
    Plus = '+',
    Comma = ',',
    Minus = '-',
    Dot = '.',
    Slash = '/',
    Colon = ':',
    SemiColon = ';',
    Less = '<',
    Equal = '=',
    Greater = '>',
    Question = '?',
    At = '@',
    OpenSquare = '[',
    BackSlash = '\\',
    CloseSquare = ']',
    Caret = '^',
    Underscore = '_',
    Backtick = '`',
    OpenCurly = '{',
    Pipe = '|',
    CloseCurly = '}',
    Tilde = '~',

    EndOfFile = 256,
    UnexpectedCharacter,
    Missing,

    BangEqual,
    PercentEqual,
    AmpersandEqual,
    AmpersandAmpersand,
    StarEqual,
    PlusEqual,
    MinusEqual,
    MinusGreater,
    DotDot,
    SlashEqual,
    ColonColon,
    LessEqual,
    LessEqualGreater,
    LessLess,
    LessLessEqual,
    EqualEqual,
    GreaterEqual,
    GreaterGreater,
    GreaterGreaterEqual,
    GreaterGreaterGreater,
    GreaterGreaterGreaterEqual,
    PipeEqual,
    TildeEqual,

    Identifier,
    LiteralInteger,
    LiteralFloat,
    LiteralString,

    #region Reserved Keywords

    Internal,
    Private,
    Protected,
    Public,

    Abstract,
    Const,
    Extern,
    Override,
    Readonly,
    Writeonly,
    Sealed,
    Static,
    Virtual,

    Func,
    Operator,
    Let,
    Struct,
    Variant,
    Union,
    Enum,
    Alias,
    Trait,
    Ref,
    Varargs,

    Module,
    Export,
    Import,

    True,
    False,
    Nil,
    This,
    Base,
    New,
    Delete,

    Sizeof,
    Alignof,
    Offsetof,
    Countof,
    Rankof,
    Typeof,

    If,
    Else,
    While,
    For,
    Do,
    Switch,
    Case,
    Default,
    Defer,

    Return,
    Break,
    Continue,
    Goto,
    Yield,

    Void,
    Noreturn,
    Bool,
    Int,
    IntSized,
    FloatSized,

    Is,
    Not,
    And,
    Or,

    #endregion

    #region Contextual Keywords

    #endregion
}
