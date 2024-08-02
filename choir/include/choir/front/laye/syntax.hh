#ifndef CHOIR_FRONT_LAYE_SYNTAX_H
#define CHOIR_FRONT_LAYE_SYNTAX_H

#include <choir/core.hh>
#include <choir/core/result.hh>
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
#include <variant>
#include <vector>

namespace choir::laye {
class SyntaxModule;
class SyntaxNode;
enum struct SyntaxTriviaMode;
struct SyntaxTrivia;
struct SyntaxToken;
class Lexer;
class Parser;
class SyntaxGraph;

using SyntaxTokenOrNodePointer = llvm::PointerUnion<const SyntaxToken*, const SyntaxNode*>;

bool IsWhiteSpace(char c);
bool IsIdentifierStart(char c);
bool IsIdentifierContinue(char c);
bool IsDecimalDigit(char c);
int DigitValue(char c);
bool IsDigitInRadix(char c, int radix);
int DigitValueInRadix(char c, int radix);
};

namespace choir::laye {

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

    static auto KindToString(Kind kind) -> String;
};

class SyntaxModule {
    CHOIR_IMMOVABLE(SyntaxModule);

    friend Parser;
    friend SyntaxNode;

    const File& _source_file;

    /// Vector of all tokens in this module.
    std::vector<SyntaxToken> _tokens{};

    /// All syntax nodes owned by this module.
    std::vector<SyntaxNode*> _nodes{};

    /// All declarations at the top-level of this syntax module.
    std::vector<SyntaxNode*> _top_level_nodes{};

    SyntaxToken* _invalid_token = new SyntaxToken{SyntaxToken::Kind::Invalid, {}};

public:
    using Ptr = std::unique_ptr<SyntaxModule>;

    /// Allocator used for allocating strings.
    std::unique_ptr<llvm::BumpPtrAllocator> string_alloc = std::make_unique<llvm::BumpPtrAllocator>();
    llvm::UniqueStringSaver string_saver{*string_alloc};

    explicit SyntaxModule(const File& source_file) : _source_file(source_file) {}
    ~SyntaxModule();

    auto context() const -> Context& { return _source_file.context(); }
    auto source_file() const -> const File& { return _source_file; }

    auto invalid_token() { return _invalid_token; }

    auto tokens() -> std::vector<SyntaxToken>& { return _tokens; }
    auto tokens() const -> std::span<const SyntaxToken> { return _tokens; }

    auto top_level_declarations() const -> std::span<const SyntaxNode* const> { return _top_level_nodes; }
    auto append_top_level_declaration(SyntaxNode* top_level_node) { _top_level_nodes.push_back(top_level_node); }

    void print_tokens(bool use_color = true);
    void print_tree(bool use_color = true);
};

class SyntaxNode {
    CHOIR_IMMOVABLE(SyntaxNode);

public:
    enum struct Kind {
        Invalid,

        ImportInvalidWithTokens,
        ImportPathSimple,
        ImportPathSimpleAliased,
        ImportNamedSimple,
        ImportNamedSimpleAliased,
    };

private:
    Kind _kind;
    Location _location;

public:
    constexpr SyntaxNode(Kind kind, Location location) : _kind(kind), _location(location) {}
    virtual ~SyntaxNode() = default;

    /// Disallow creating syntax nodes without a module reference.
    void* operator new(usz) = delete;
    void* operator new(usz sz, SyntaxModule* module);

    auto kind() const { return _kind; }
    auto location() const { return _location; }

    auto children() const -> std::vector<SyntaxTokenOrNodePointer>;

    static auto KindToString(Kind kind) -> String;
};

/// `import <anything>;`
class SyntaxImportInvalidWithTokens : public SyntaxNode {
    SyntaxToken* _token_import;
    SyntaxToken* _token_semi;
    std::vector<SyntaxToken*> _consumed_tokens;

public:
    SyntaxImportInvalidWithTokens(SyntaxToken* token_import, SyntaxToken* token_semi, std::vector<SyntaxToken*> consumed_tokens)
        : SyntaxNode(Kind::ImportInvalidWithTokens, token_import->location), _token_import(token_import), _token_semi(token_semi), _consumed_tokens(std::move(consumed_tokens)) {
        CHOIR_ASSERT(token_import->kind == SyntaxToken::Kind::Import);
    }

    auto token_import() const -> const SyntaxToken* { return _token_import; }
    auto token_semi() const -> const SyntaxToken* { return _token_semi; }
    auto consumed_tokens() const -> std::span<const SyntaxToken* const> { return _consumed_tokens; }

    [[nodiscard]]
    static auto classof(const SyntaxNode* type) -> bool { return type->kind() == Kind::ImportInvalidWithTokens; }
};

/// `import "foo.laye";`
class SyntaxImportPathSimple : public SyntaxNode {
    SyntaxToken* _token_import;
    SyntaxToken* _token_path;
    SyntaxToken* _token_semi;

public:
    SyntaxImportPathSimple(SyntaxToken* token_import, SyntaxToken* token_path, SyntaxToken* token_semi)
        : SyntaxNode(Kind::ImportPathSimple, token_import->location), _token_import(token_import), _token_path(token_path), _token_semi(token_semi) {
        CHOIR_ASSERT(token_import->kind == SyntaxToken::Kind::Import);
        CHOIR_ASSERT(token_path->kind == SyntaxToken::Kind::LiteralString);
    }

    auto token_import() const { return _token_import; }
    auto token_path() const { return _token_path; }
    auto token_semi() const { return _token_semi; }

    auto path_text() const -> String { return _token_path->text; }

    [[nodiscard]]
    static auto classof(const SyntaxNode* type) -> bool { return type->kind() == Kind::ImportPathSimple; }
};

/// `import "foo.laye" as bar;`
class SyntaxImportPathSimpleAliased : public SyntaxNode {
    SyntaxToken* _token_import;
    SyntaxToken* _token_path;
    SyntaxToken* _token_as;
    SyntaxToken* _token_alias_ident;
    SyntaxToken* _token_semi;

public:
    SyntaxImportPathSimpleAliased(SyntaxToken* token_import, SyntaxToken* token_path, SyntaxToken* token_as, SyntaxToken* token_alias_ident, SyntaxToken* token_semi)
        : SyntaxNode(Kind::ImportPathSimpleAliased, token_import->location), _token_import(token_import), _token_path(token_path), _token_as(token_as), _token_alias_ident(token_alias_ident), _token_semi(token_semi) {
        CHOIR_ASSERT(token_import->kind == SyntaxToken::Kind::Import);
        CHOIR_ASSERT(token_path->kind == SyntaxToken::Kind::LiteralString);
        CHOIR_ASSERT(token_as->kind == SyntaxToken::Kind::As);
    }

    auto token_import() const { return _token_import; }
    auto token_path() const { return _token_path; }
    auto token_as() const { return _token_as; }
    auto token_alias_ident() const { return _token_alias_ident; }
    auto token_semi() const { return _token_semi; }

    auto path_text() const -> String { return _token_path->text; }
    auto alias_name() const -> String { return _token_alias_ident->kind == SyntaxToken::Kind::Identifier ? _token_alias_ident->text : ""; }

    [[nodiscard]]
    static auto classof(const SyntaxNode* type) -> bool { return type->kind() == Kind::ImportPathSimpleAliased; }
};

/// `import foo;`
class SyntaxImportNamedSimple : public SyntaxNode {
    SyntaxToken* _token_import;
    SyntaxToken* _token_name;
    SyntaxToken* _token_semi;

public:
    SyntaxImportNamedSimple(SyntaxToken* token_import, SyntaxToken* token_name, SyntaxToken* token_semi)
        : SyntaxNode(Kind::ImportNamedSimple, token_import->location), _token_import(token_import), _token_name(token_name), _token_semi(token_semi) {
        CHOIR_ASSERT(token_import->kind == SyntaxToken::Kind::Import);
        CHOIR_ASSERT(token_name->kind == SyntaxToken::Kind::Identifier);
        CHOIR_ASSERT(token_semi->kind == SyntaxToken::Kind::SemiColon);
    }

    auto token_import() const { return _token_import; }
    auto token_name() const { return _token_name; }
    auto token_semi() const { return _token_semi; }

    auto name_text() const -> String { return _token_name->text; }

    [[nodiscard]]
    static auto classof(const SyntaxNode* type) -> bool { return type->kind() == Kind::ImportNamedSimple; }
};

/// `import foo as bar;`
class SyntaxImportNamedSimpleAliased : public SyntaxNode {
    SyntaxToken* _token_import;
    SyntaxToken* _token_name;
    SyntaxToken* _token_as;
    SyntaxToken* _token_alias_ident;
    SyntaxToken* _token_semi;

public:
    SyntaxImportNamedSimpleAliased(SyntaxToken* token_import, SyntaxToken* token_name, SyntaxToken* token_as, SyntaxToken* token_alias_ident, SyntaxToken* token_semi)
        : SyntaxNode(Kind::ImportNamedSimpleAliased, token_import->location), _token_import(token_import), _token_name(token_name), _token_as(token_as), _token_alias_ident(token_alias_ident), _token_semi(token_semi) {
        CHOIR_ASSERT(token_import->kind == SyntaxToken::Kind::Import);
        CHOIR_ASSERT(token_name->kind == SyntaxToken::Kind::Identifier);
        CHOIR_ASSERT(token_as->kind == SyntaxToken::Kind::As);
        CHOIR_ASSERT(token_alias_ident->kind == SyntaxToken::Kind::Identifier);
        CHOIR_ASSERT(token_semi->kind == SyntaxToken::Kind::SemiColon);
    }

    auto token_import() const { return _token_import; }
    auto token_name() const { return _token_name; }
    auto token_as() const { return _token_as; }
    auto token_alias_ident() const { return _token_alias_ident; }
    auto token_semi() const { return _token_semi; }

    auto name_text() const -> String { return _token_name->text; }
    auto alias_name() const -> String { return _token_alias_ident->text; }

    [[nodiscard]]
    static auto classof(const SyntaxNode* type) -> bool { return type->kind() == Kind::ImportNamedSimpleAliased; }
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

class Parser {
    CHOIR_DECLARE_HIDDEN_IMPL(Parser);

public:
    explicit Parser(const File& source_file);

    auto context() const -> Context&;
    auto module() const -> SyntaxModule*;
    auto tokens() const -> std::span<const SyntaxToken>;

    auto eof() const -> bool;

    auto parse_statement() -> SyntaxNode*;
    auto parse_declaration() -> SyntaxNode*;
    auto parse_expression() -> SyntaxNode*;

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

#endif // CHOIR_FRONT_LAYE_SYNTAX_H
