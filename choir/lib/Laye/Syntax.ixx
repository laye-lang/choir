module;

#include <choir/macros.hh>
#include <llvm/ADT/SmallVector.h>
#include <memory>

export module choir.laye:syntax;
import choir;
import choir.frontend;

export namespace choir::laye {
class SyntaxModule;
class SyntaxNode;
struct SyntaxToken;
class Lexer;
class Parser;
class SyntaxUnit;
};

namespace choir::laye {

class SyntaxModule {
    CHOIR_IMMOVABLE(SyntaxModule);

    friend Parser;
    const File& _source_file;

public:
    using Ptr = std::unique_ptr<SyntaxModule>;

    explicit SyntaxModule(const File& source_file) : _source_file(source_file) {}

    auto context() const -> Context& { return _source_file.context(); }
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

struct SyntaxToken {
};

class Lexer : DiagsProducer<std::nullptr_t> {
    CHOIR_IMMOVABLE(Lexer);

    friend DiagsProducer;

    Context& _context;

    explicit Lexer(const File& source_file)
        : _context(source_file.context()) {
    }

public:
    auto context() const -> Context& { return _context; }

    static auto ReadTokens(const File& source_file) -> std::vector<SyntaxToken>;
};

class Parser : DiagsProducer<std::nullptr_t> {
    CHOIR_IMMOVABLE(Parser);

    friend DiagsProducer;

    Context& _context;
    SyntaxModule::Ptr _module;
    std::vector<SyntaxToken> _tokens;

    explicit Parser(const File& source_file, std::vector<SyntaxToken> tokens)
        : _context(source_file.context()), _module{std::make_unique<SyntaxModule>(source_file)}, _tokens(std::move(tokens)) {
    }

public:
    auto context() const -> Context& { return _context; }

    static auto Parse(const File& source_file) -> SyntaxModule::Ptr;
};

class SyntaxUnit {
    CHOIR_IMMOVABLE(SyntaxUnit);

    Context& _context;

public:
    explicit SyntaxUnit(Context& context) : _context(context){}

    auto context() const -> Context& { return _context; }

    void parse(File::Path source_file_path);
};

}; // namespace choir::laye
