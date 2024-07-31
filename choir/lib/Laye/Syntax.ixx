module;

#include <choir/macros.hh>
#include <llvm/ADT/APFloat.h>
#include <llvm/ADT/APInt.h>
#include <llvm/ADT/DirectedGraph.h>
#include <llvm/ADT/SmallVector.h>
#include <llvm/Analysis/DDG.h>
#include <llvm/Support/Allocator.h>
#include <llvm/Support/StringSaver.h>
#include <memory>
#include <span>
#include <vector>

export module choir.laye:syntax;
import choir;
import choir.frontend;

export namespace choir::laye {
class SyntaxModule;
class SyntaxNode;
enum struct SyntaxTriviaMode;
struct SyntaxTrivia;
struct SyntaxToken;
class Lexer;
class Parser;
class SyntaxGraph;

bool IsWhiteSpace(char c);
bool IsIdentifierStart(char c);
bool IsIdentifierContinue(char c);
bool IsDecimalDigit(char c);
int DigitValue(char c);
bool IsDigitInRadix(char c, int radix);
int DigitValueInRadix(char c, int radix);
}; // namespace choir::laye

namespace choir::laye {

class SyntaxModule {
    CHOIR_IMMOVABLE(SyntaxModule);

    friend Parser;
    const File& _source_file;

    /// Vector of all tokens in this module.
    std::vector<SyntaxToken> _tokens{};

public:
    using Ptr = std::unique_ptr<SyntaxModule>;

    /// Allocator used for allocating strings.
    std::unique_ptr<llvm::BumpPtrAllocator> string_alloc = std::make_unique<llvm::BumpPtrAllocator>();
    llvm::UniqueStringSaver string_saver{*string_alloc};

    explicit SyntaxModule(const File& source_file) : _source_file(source_file) {}

    auto context() const -> Context& { return _source_file.context(); }
    auto source_file() const -> const File& { return _source_file; }

    auto tokens() -> std::vector<SyntaxToken>& { return _tokens; }
    auto tokens() const -> std::span<const SyntaxToken> { return _tokens; }
};

class SyntaxNode {
    CHOIR_IMMOVABLE(SyntaxNode);

public:
    enum struct Kind {
        Invalid,
    };

private:
    Kind _kind;

public:
    SyntaxNode(Kind kind) : _kind(kind) {}
};

enum struct SyntaxTriviaMode {
    None,
    DocumentationOnly,
    CommentsOnly,
    All,
};

struct SyntaxTrivia {
    enum struct Kind {
        Invalid,
        WhiteSpace,
        LineComment,
        BlockComment,
        DocComment,
    };
};

struct SyntaxToken {
    enum struct Kind {
        Invalid,
        EndOfFile,

        Tilde,
        Bang,
        Percent,
        Ampersand,
        Star,
        OpenParen,
        CloseParen,
        Minus,
        Equal,
        Plus,
        OpenBracket,
        CloseBracket,
        OpenBrace,
        CloseBrace,
        Pipe,
        SemiColon,
        Colon,
        Comma,
        Less,
        Greater,
        Dot,
        Slash,
        Question,

        Identifier,
        Global,

        LiteralInteger,
        LiteralFloat,
        LiteralString,
        LiteralRune,

        SlashColon,
        PercentColon,
        QuestionQuestion,
        QuestionQuestionEqual,
        PlusPlus,
        PlusPercent,
        PlusPipe,
        MinusMinus,
        MinusPercent,
        MinusPipe,
        Caret,
        LessColon,
        LessLess,
        ColonGreater,
        GreaterGreater,
        EqualEqual,
        BangEqual,
        PlusEqual,
        PlusPercentEqual,
        PlusPipeEqual,
        MinusEqual,
        MinusPercentEqual,
        MinusPipeEqual,
        SlashEqual,
        SlashColonEqual,
        StarEqual,
        CaretEqual,
        PercentEqual,
        PercentColonEqual,
        LessEqual,
        LessEqualColon,
        GreaterEqual,
        ColonGreaterEqual,
        AmpersandEqual,
        PipeEqual,
        TildeEqual,
        LessLessEqual,
        GreaterGreaterEqual,
        EqualGreater,
        LessMinus,
        ColonColon,

        Strict,
        From,
        As,

        Var,
        Void,
        NoReturn,
        Bool,
        BoolSized,
        Int,
        IntSized,
        FloatSized,

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
        Alias,
        Test,
        Import,
        Export,
        Operator,

        Mut,
        New,
        Delete,
        Cast,
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
    };

    Kind kind = Kind::Invalid;
    Location location{};

    bool artificial = false;

    String text{};
    llvm::APInt integer_value{};
    llvm::APFloat float_value{0.0f};

    llvm::SmallVector<SyntaxTrivia, 0> leading_trivia{};
    llvm::SmallVector<SyntaxTrivia, 0> trailing_trivia{};

    auto spelling(const Context& context) const -> llvm::StringRef { return location.text(context); }

    static auto KindToString(Kind kind) -> String {
        switch (kind) {
            default: "Unknown??";
            case Kind::Invalid: return "Invalid";
            case Kind::EndOfFile: return "EndOfFile";
            case Kind::Tilde: return "Tilde";
            case Kind::Bang: return "Bang";
            case Kind::Percent: return "Percent";
            case Kind::Ampersand: return "Ampersand";
            case Kind::Star: return "Star";
            case Kind::OpenParen: return "OpenParen";
            case Kind::CloseParen: return "CloseParen";
            case Kind::Minus: return "Minus";
            case Kind::Equal: return "Equal";
            case Kind::Plus: return "Plus";
            case Kind::OpenBracket: return "OpenBracket";
            case Kind::CloseBracket: return "CloseBracket";
            case Kind::OpenBrace: return "OpenBrace";
            case Kind::CloseBrace: return "CloseBrace";
            case Kind::Pipe: return "Pipe";
            case Kind::SemiColon: return "SemiColon";
            case Kind::Colon: return "Colon";
            case Kind::Comma: return "Comma";
            case Kind::Less: return "Less";
            case Kind::Greater: return "Greater";
            case Kind::Dot: return "Dot";
            case Kind::Slash: return "Slash";
            case Kind::Question: return "Question";
            case Kind::Identifier: return "Identifier";
            case Kind::Global: return "Global";
            case Kind::LiteralInteger: return "LiteralInteger";
            case Kind::LiteralFloat: return "LiteralFloat";
            case Kind::LiteralString: return "LiteralString";
            case Kind::LiteralRune: return "LiteralRune";
            case Kind::SlashColon: return "SlashColon";
            case Kind::PercentColon: return "PercentColon";
            case Kind::QuestionQuestion: return "QuestionQuestion";
            case Kind::QuestionQuestionEqual: return "QuestionQuestionEqual";
            case Kind::PlusPlus: return "PlusPlus";
            case Kind::PlusPercent: return "PlusPercent";
            case Kind::PlusPipe: return "PlusPipe";
            case Kind::MinusMinus: return "MinusMinus";
            case Kind::MinusPercent: return "MinusPercent";
            case Kind::MinusPipe: return "MinusPipe";
            case Kind::Caret: return "Caret";
            case Kind::LessColon: return "LessColon";
            case Kind::LessLess: return "LessLess";
            case Kind::ColonGreater: return "ColonGreater";
            case Kind::GreaterGreater: return "GreaterGreater";
            case Kind::EqualEqual: return "EqualEqual";
            case Kind::BangEqual: return "BangEqual";
            case Kind::PlusEqual: return "PlusEqual";
            case Kind::PlusPercentEqual: return "PlusPercentEqual";
            case Kind::PlusPipeEqual: return "PlusPipeEqual";
            case Kind::MinusEqual: return "MinusEqual";
            case Kind::MinusPercentEqual: return "MinusPercentEqual";
            case Kind::MinusPipeEqual: return "MinusPipeEqual";
            case Kind::SlashEqual: return "SlashEqual";
            case Kind::SlashColonEqual: return "SlashColonEqual";
            case Kind::StarEqual: return "StarEqual";
            case Kind::CaretEqual: return "CaretEqual";
            case Kind::PercentEqual: return "PercentEqual";
            case Kind::PercentColonEqual: return "PercentColonEqual";
            case Kind::LessEqual: return "LessEqual";
            case Kind::LessEqualColon: return "LessEqualColon";
            case Kind::GreaterEqual: return "GreaterEqual";
            case Kind::ColonGreaterEqual: return "ColonGreaterEqual";
            case Kind::AmpersandEqual: return "AmpersandEqual";
            case Kind::PipeEqual: return "PipeEqual";
            case Kind::TildeEqual: return "TildeEqual";
            case Kind::LessLessEqual: return "LessLessEqual";
            case Kind::GreaterGreaterEqual: return "GreaterGreaterEqual";
            case Kind::EqualGreater: return "EqualGreater";
            case Kind::LessMinus: return "LessMinus";
            case Kind::ColonColon: return "ColonColon";
            case Kind::Strict: return "Strict";
            case Kind::From: return "From";
            case Kind::As: return "As";
            case Kind::Var: return "Var";
            case Kind::Void: return "Void";
            case Kind::NoReturn: return "NoReturn";
            case Kind::Bool: return "Bool";
            case Kind::BoolSized: return "BoolSized";
            case Kind::Int: return "Int";
            case Kind::IntSized: return "IntSized";
            case Kind::FloatSized: return "FloatSized";
            case Kind::True: return "True";
            case Kind::False: return "False";
            case Kind::Nil: return "Nil";
            case Kind::If: return "If";
            case Kind::Else: return "Else";
            case Kind::For: return "For";
            case Kind::While: return "While";
            case Kind::Do: return "Do";
            case Kind::Switch: return "Switch";
            case Kind::Case: return "Case";
            case Kind::Default: return "Default";
            case Kind::Return: return "Return";
            case Kind::Break: return "Break";
            case Kind::Continue: return "Continue";
            case Kind::Fallthrough: return "Fallthrough";
            case Kind::Yield: return "Yield";
            case Kind::Unreachable: return "Unreachable";
            case Kind::Defer: return "Defer";
            case Kind::Discard: return "Discard";
            case Kind::Goto: return "Goto";
            case Kind::Xyzzy: return "Xyzzy";
            case Kind::Assert: return "Assert";
            case Kind::Try: return "Try";
            case Kind::Catch: return "Catch";
            case Kind::Struct: return "Struct";
            case Kind::Variant: return "Variant";
            case Kind::Enum: return "Enum";
            case Kind::Template: return "Template";
            case Kind::Alias: return "Alias";
            case Kind::Test: return "Test";
            case Kind::Import: return "Import";
            case Kind::Export: return "Export";
            case Kind::Operator: return "Operator";
            case Kind::Mut: return "Mut";
            case Kind::New: return "New";
            case Kind::Delete: return "Delete";
            case Kind::Cast: return "Cast";
            case Kind::Is: return "Is";
            case Kind::Sizeof: return "Sizeof";
            case Kind::Alignof: return "Alignof";
            case Kind::Offsetof: return "Offsetof";
            case Kind::Not: return "Not";
            case Kind::And: return "And";
            case Kind::Or: return "Or";
            case Kind::Xor: return "Xor";
            case Kind::Varargs: return "Varargs";
            case Kind::Const: return "Const";
            case Kind::Foreign: return "Foreign";
            case Kind::Inline: return "Inline";
            case Kind::Callconv: return "Callconv";
            case Kind::Pure: return "Pure";
            case Kind::Discardable: return "Discardable";
        }
    }
};

class Lexer {
    CHOIR_DECLARE_HIDDEN_IMPL(Lexer);

public:
    explicit Lexer(SyntaxModule& syntax_module, SyntaxTriviaMode trivia_mode = SyntaxTriviaMode::None);

    auto context() const -> Context&;
    auto source_file() const -> const File&;

    auto read_token() -> SyntaxToken;

    static void ReadTokens(SyntaxModule& syntax_module, SyntaxTriviaMode trivia_mode = SyntaxTriviaMode::None);
};

class Parser : DiagsProducer<Parser> {
    CHOIR_IMMOVABLE(Parser);

    friend DiagsProducer;

    Context& _context;
    SyntaxModule::Ptr _module;

    explicit Parser(const File& source_file)
        : _context(source_file.context()), _module{std::make_unique<SyntaxModule>(source_file)} {
        Lexer::ReadTokens(*_module);
    }

    auto tokens() const -> std::span<const SyntaxToken> { return _module->tokens(); }

public:
    auto context() const -> Context& { return _context; }

    static auto Parse(const File& source_file) -> SyntaxModule::Ptr;
};

class SyntaxGraph {
    CHOIR_IMMOVABLE(SyntaxGraph);

    Context& _context;
    bool _lex_only;

    DirectedGraph<SyntaxModule*> _module_graph{};
    std::vector<std::unique_ptr<SyntaxModule>> _modules{};

public:
    explicit SyntaxGraph(Context& context, bool lex_only) : _context(context), _lex_only(lex_only) {}

    auto context() const -> Context& { return _context; }
    auto lex_only() const { return _lex_only; }

    void add_file(File::Path source_file_path);

    auto ordered_modules() const -> std::vector<SyntaxModule*> { return std::move(_module_graph.ordered_elements().elements); }
};

}; // namespace choir::laye
