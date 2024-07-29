module;

#include <choir/macros.hh>
#include <llvm/ADT/StringExtras.h>
#include <llvm/ADT/SmallString.h>
#include <mutex>
// #include <llvm/Support/UnicodeCharRanges.h>

module choir.laye;
import choir;
import choir.frontend;

using namespace choir;
using namespace choir::laye;

// ============================================================================
//  Internal API
// ============================================================================

// ============================================================================
//  Implementation
// ============================================================================

struct Lexer::Impl : DiagsProducer<Lexer::Impl> {
    std::mutex mutex{};

    Context& context;
    const File& source_file;
    SyntaxTriviaMode trivia_mode;

    const char* current;
    const char* end;

    llvm::SmallString<128> string_builder{};

    explicit Impl(const File& source_file, SyntaxTriviaMode trivia_mode)
        : context(source_file.context()), source_file(source_file), trivia_mode(trivia_mode), current(source_file.data()), end(source_file.data() + source_file.size()) {
    }

    template <typename... Args>
    void Diag(Diagnostic::Level level, Location loc, std::format_string<Args...> fmt, Args&&... args) {
        context.diags().diag(level, loc, fmt, std::forward<Args>(args)...);
    }

    template <typename... Args>
    int Error(std::format_string<Args...> fmt, Args&&... args) {
        Diag(Diagnostic::Level::Error, location(), fmt, std::forward<Args>(args)...);
        return 1;
    }

    auto begin() const -> const char* { return source_file.data(); }
    auto eof() const -> bool { return current >= end; }

    auto advance() -> void { current++; }
    auto peek() const -> char {
        if (current + 1 >= end) return 0;
        return *(current + 1);
    }

    auto location() -> Location {
        return {u32(current - begin()), 1, u16(source_file.file_id())};
    }

    auto consume_trivia(llvm::SmallVectorImpl<SyntaxTrivia>& trivia, bool is_trailing) -> void;
    auto read_token() -> SyntaxToken;
};

auto Lexer::Impl::consume_trivia(llvm::SmallVectorImpl<SyntaxTrivia>& trivia, bool is_trailing) -> void {
    Location start_location = location();

    while (not eof()) {
        char c = *current;
        if (IsWhiteSpace(c)) {
            if (trivia_mode == SyntaxTriviaMode::All) {
                CHOIR_TODO("trivia storage");
            } else {
                while (not eof() and IsWhiteSpace(*current)) {
                    advance();
                }
            }
        } else if (c == '/' && peek() == '/') {
            advance();
            advance();

            if (trivia_mode >= SyntaxTriviaMode::CommentsOnly) {
                CHOIR_TODO("trivia storage");
            } else {
                while (not eof() and *current != '\n') {
                    advance();
                }
            }
        } else break;
    }
}

auto Lexer::Impl::read_token() -> SyntaxToken {
    SyntaxToken token{};
    token.location = location();

    consume_trivia(token.leading_trivia, false);

    if (eof()) {
        token.kind = SyntaxToken::Kind::EndOfFile;
        return token;
    }

    switch (*current) {
        default: {
            token.kind = SyntaxToken::Kind::Invalid;
            Error("Invalid character in Laye source file");
            advance();
        } break;
    }

    consume_trivia(token.trailing_trivia, true);
    return token;
}

// ============================================================================
//  Public API
// ============================================================================

CHOIR_DEFINE_HIDDEN_IMPL(Lexer);
Lexer::Lexer(const File& source_file, SyntaxTriviaMode trivia_mode) : impl(new Impl{source_file, trivia_mode}) {}

auto Lexer::context() const -> Context& {
    std::unique_lock _{impl->mutex};
    return impl->context;
}

auto Lexer::source_file() const -> const File& {
    std::unique_lock _{impl->mutex};
    return impl->source_file;
}

auto Lexer::read_token() -> SyntaxToken {
    std::unique_lock _{impl->mutex};
    return impl->read_token();
}

void Lexer::ReadTokens(SyntaxModule& syntax_module, SyntaxTriviaMode trivia_mode) {
    Lexer lexer{syntax_module.source_file(), trivia_mode};

    SyntaxToken token{};
    do {
        token = lexer.read_token();
        syntax_module.tokens().push_back(token);
    } while (token.kind != SyntaxToken::Kind::EndOfFile);
}

bool choir::laye::IsWhiteSpace(char c) {
    return llvm::isSpace(c);
}

bool choir::laye::IsIdentifierStart(char c) {
    return llvm::isAlnum(c) or c == '_';
}

bool choir::laye::IsIdentifierContinue(char c) {
    return llvm::isAlnum(c) or c == '_';
}

bool choir::laye::IsDecimalDigit(char c) {
    return llvm::isDigit(c);
}

int choir::laye::DigitValue(char c) {
    return c - '0';
}

bool choir::laye::IsDigitInRadix(char c, int radix) {
    CHOIR_TODO("Non-decimal digits");
}

int choir::laye::DigitValueInRadix(char c, int radix) {
    CHOIR_TODO("Non-decimal digits");
}
