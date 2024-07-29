module;

#include <choir/macros.hh>
#include <llvm/ADT/APFloat.h>
#include <llvm/ADT/APInt.h>
#include <llvm/ADT/SmallVector.h>
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
};

class Lexer {
    CHOIR_DECLARE_HIDDEN_IMPL(Lexer);

public:
    explicit Lexer(const File& source_file, SyntaxTriviaMode trivia_mode = SyntaxTriviaMode::None);

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

public:
    explicit SyntaxGraph(Context& context) : _context(context) {}

    auto context() const -> Context& { return _context; }

    void add_file(File::Path source_file_path);
};

}; // namespace choir::laye
