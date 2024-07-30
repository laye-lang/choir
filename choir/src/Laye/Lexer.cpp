module;

#include <choir/macros.hh>
#include <llvm/ADT/SmallString.h>
#include <llvm/ADT/StringExtras.h>
#include <llvm/ADT/StringMap.h>
#include <mutex>
// #include <llvm/Support/UnicodeCharRanges.h>

module choir.laye;
import choir;
import choir.frontend;

using namespace choir;
using namespace choir::laye;

llvm::StringMap<SyntaxToken::Kind> keyword_kinds{
    {"var", SyntaxToken::Kind::Var},
    {"void", SyntaxToken::Kind::Void},
    {"noreturn", SyntaxToken::Kind::NoReturn},
    {"bool", SyntaxToken::Kind::Bool},
    {"boolsized", SyntaxToken::Kind::BoolSized},
    {"int", SyntaxToken::Kind::Int},
    {"intsized", SyntaxToken::Kind::IntSized},
    {"floatsized", SyntaxToken::Kind::FloatSized},
    {"true", SyntaxToken::Kind::True},
    {"false", SyntaxToken::Kind::False},
    {"nil", SyntaxToken::Kind::Nil},
    {"if", SyntaxToken::Kind::If},
    {"else", SyntaxToken::Kind::Else},
    {"for", SyntaxToken::Kind::For},
    {"while", SyntaxToken::Kind::While},
    {"do", SyntaxToken::Kind::Do},
    {"switch", SyntaxToken::Kind::Switch},
    {"case", SyntaxToken::Kind::Case},
    {"default", SyntaxToken::Kind::Default},
    {"return", SyntaxToken::Kind::Return},
    {"break", SyntaxToken::Kind::Break},
    {"continue", SyntaxToken::Kind::Continue},
    {"fallthrough", SyntaxToken::Kind::Fallthrough},
    {"yield", SyntaxToken::Kind::Yield},
    {"unreachable", SyntaxToken::Kind::Unreachable},
    {"defer", SyntaxToken::Kind::Defer},
    {"discard", SyntaxToken::Kind::Discard},
    {"goto", SyntaxToken::Kind::Goto},
    {"xyzzy", SyntaxToken::Kind::Xyzzy},
    {"assert", SyntaxToken::Kind::Assert},
    {"try", SyntaxToken::Kind::Try},
    {"catch", SyntaxToken::Kind::Catch},
    {"struct", SyntaxToken::Kind::Struct},
    {"variant", SyntaxToken::Kind::Variant},
    {"enum", SyntaxToken::Kind::Enum},
    {"template", SyntaxToken::Kind::Template},
    {"alias", SyntaxToken::Kind::Alias},
    {"test", SyntaxToken::Kind::Test},
    {"import", SyntaxToken::Kind::Import},
    {"export", SyntaxToken::Kind::Export},
    {"operator", SyntaxToken::Kind::Operator},
    {"mut", SyntaxToken::Kind::Mut},
    {"new", SyntaxToken::Kind::New},
    {"delete", SyntaxToken::Kind::Delete},
    {"cast", SyntaxToken::Kind::Cast},
    {"is", SyntaxToken::Kind::Is},
    {"sizeof", SyntaxToken::Kind::Sizeof},
    {"alignof", SyntaxToken::Kind::Alignof},
    {"offsetof", SyntaxToken::Kind::Offsetof},
    {"not", SyntaxToken::Kind::Not},
    {"and", SyntaxToken::Kind::And},
    {"or", SyntaxToken::Kind::Or},
    {"xor", SyntaxToken::Kind::Xor},
    {"varargs", SyntaxToken::Kind::Varargs},
    {"const", SyntaxToken::Kind::Const},
    {"foreign", SyntaxToken::Kind::Foreign},
    {"inline", SyntaxToken::Kind::Inline},
    {"callconv", SyntaxToken::Kind::Callconv},
    {"pure", SyntaxToken::Kind::Pure},
    {"discardable", SyntaxToken::Kind::Discardable},
};

// ============================================================================
//  Internal API
// ============================================================================

// ============================================================================
//  Implementation
// ============================================================================

struct Lexer::Impl : DiagsProducer<Lexer::Impl> {
    std::mutex mutex{};

    SyntaxModule& syntax_module;
    Context& context;
    const File& source_file;
    SyntaxTriviaMode trivia_mode;

    const char* current_ptr;
    const char* end_ptr;

    llvm::SmallString<128> string_builder{};

    explicit Impl(SyntaxModule& syntax_module, SyntaxTriviaMode trivia_mode)
        : syntax_module(syntax_module), context(syntax_module.context()), source_file(syntax_module.source_file()), trivia_mode(trivia_mode), current_ptr(source_file.data()), end_ptr(source_file.data() + source_file.size()) {
    }

    template <typename... Args>
    void Diag(Diagnostic::Level level, Location loc, std::format_string<Args...> fmt, Args&&... args) {
        context.diags().diag(level, loc, fmt, std::forward<Args>(args)...);
    }

    template <typename... Args>
    int ErrorNoLoc(std::format_string<Args...> fmt, Args&&... args) {
        Diag(Diagnostic::Level::Error, location(), fmt, std::forward<Args>(args)...);
        return 1;
    }

    auto begin() const -> const char* { return source_file.data(); }
    auto eof() const -> bool { return current_ptr >= end_ptr; }

    auto advance() -> void { current_ptr++; }
    auto current() const { return eof() ? 0 : *current_ptr; }
    auto peek() const -> char {
        if (current_ptr + 1 >= end_ptr) return 0;
        return *(current_ptr + 1);
    }

    auto location() -> Location {
        return {u32(current_ptr - begin()), 1, u16(source_file.file_id())};
    }

    auto consume_trivia(llvm::SmallVectorImpl<SyntaxTrivia>& trivia, bool is_trailing) -> void;
    auto read_token() -> SyntaxToken;

    void read_identifier_token(SyntaxToken& token, bool transform_keywords = true);
};

auto Lexer::Impl::consume_trivia(llvm::SmallVectorImpl<SyntaxTrivia>& trivia, bool is_trailing) -> void {
    Location start_location = location();

    while (not eof()) {
        char c = current();
        if (IsWhiteSpace(c)) {
            if (trivia_mode == SyntaxTriviaMode::All) {
                CHOIR_TODO("trivia storage");
            } else {
                while (not eof() and IsWhiteSpace(current())) {
                    advance();
                }
            }
        } else if (c == '/' && peek() == '/') {
            advance();
            advance();

            if (trivia_mode >= SyntaxTriviaMode::CommentsOnly) {
                CHOIR_TODO("trivia storage");
            } else {
                while (not eof() and current() != '\n') {
                    advance();
                }
            }
        } else break;
    }
}

auto Lexer::Impl::read_token() -> SyntaxToken {
    SyntaxToken token{};

    consume_trivia(token.leading_trivia, false);
    token.location = location();

    if (eof()) {
        token.kind = SyntaxToken::Kind::EndOfFile;
        return token;
    }

    switch (current()) {
        case '(': {
            advance();
            token.kind = SyntaxToken::Kind::OpenParen;
        } break;
        case ')': {
            advance();
            token.kind = SyntaxToken::Kind::CloseParen;
        } break;

        case '[': {
            advance();
            token.kind = SyntaxToken::Kind::OpenBracket;
        } break;
        case ']': {
            advance();
            token.kind = SyntaxToken::Kind::CloseBracket;
        } break;

        case '{': {
            advance();
            token.kind = SyntaxToken::Kind::OpenBrace;
        } break;
        case '}': {
            advance();
            token.kind = SyntaxToken::Kind::CloseBrace;
        } break;

        case '.': {
            advance();
            token.kind = SyntaxToken::Kind::Dot;
        } break;
        case ',': {
            advance();
            token.kind = SyntaxToken::Kind::Comma;
        } break;
        case ';': {
            advance();
            token.kind = SyntaxToken::Kind::SemiColon;
        } break;

        case '=': {
            advance();
            if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::EqualEqual;
            } else if (current() == '>') {
                advance();
                token.kind = SyntaxToken::Kind::EqualGreater;
            } else {
                token.kind = SyntaxToken::Kind::Equal;
            }
        } break;

        case '!': {
            advance();
            if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::BangEqual;
            } else {
                token.kind = SyntaxToken::Kind::Bang;
            }
        } break;

        case '?': {
            advance();
            if (current() == '?') {
                advance();
                if (current() == '=') {
                    advance();
                    token.kind = SyntaxToken::Kind::QuestionQuestionEqual;
                } else {
                    token.kind = SyntaxToken::Kind::QuestionQuestion;
                }
            } else {
                token.kind = SyntaxToken::Kind::Question;
            }
        } break;

        case '+': {
            advance();
            if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::PlusEqual;
            } else if (current() == '+') {
                advance();
                token.kind = SyntaxToken::Kind::PlusPlus;
            } else if (current() == '%') {
                advance();
                if (current() == '=') {
                    advance();
                    token.kind = SyntaxToken::Kind::PlusPercentEqual;
                } else {
                    token.kind = SyntaxToken::Kind::PlusPercent;
                }
            } else if (current() == '|') {
                advance();
                if (current() == '=') {
                    advance();
                    token.kind = SyntaxToken::Kind::PlusPipeEqual;
                } else {
                    token.kind = SyntaxToken::Kind::PlusPipe;
                }
            } else {
                token.kind = SyntaxToken::Kind::Plus;
            }
        } break;

        case '-': {
            advance();
            if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::MinusEqual;
            } else if (current() == '-') {
                advance();
                token.kind = SyntaxToken::Kind::MinusMinus;
            } else if (current() == '%') {
                advance();
                if (current() == '=') {
                    advance();
                    token.kind = SyntaxToken::Kind::MinusPercentEqual;
                } else {
                    token.kind = SyntaxToken::Kind::MinusPercent;
                }
            } else if (current() == '|') {
                advance();
                if (current() == '=') {
                    advance();
                    token.kind = SyntaxToken::Kind::MinusPipeEqual;
                } else {
                    token.kind = SyntaxToken::Kind::MinusPipe;
                }
            } else {
                token.kind = SyntaxToken::Kind::Minus;
            }
        } break;

        case '*': {
            advance();
            if (current() == '*') {
                advance();
                if (current() == '=') {
                    advance();
                    token.kind = SyntaxToken::Kind::StarStarEqual;
                } else {
                    token.kind = SyntaxToken::Kind::StarStar;
                }
            } else if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::StarEqual;
            } else {
                token.kind = SyntaxToken::Kind::Star;
            }
        } break;

        case '/': {
            advance();
            if (current() == ':') {
                advance();
                if (current() == '=') {
                    advance();
                    token.kind = SyntaxToken::Kind::SlashColonEqual;
                } else {
                    token.kind = SyntaxToken::Kind::SlashColon;
                }
            } else if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::SlashEqual;
            } else {
                token.kind = SyntaxToken::Kind::Slash;
            }
        } break;

        case '%': {
            advance();
            if (current() == ':') {
                advance();
                if (current() == '=') {
                    advance();
                    token.kind = SyntaxToken::Kind::PercentColonEqual;
                } else {
                    token.kind = SyntaxToken::Kind::PercentColon;
                }
            } else if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::PercentEqual;
            } else {
                token.kind = SyntaxToken::Kind::Percent;
            }
        } break;

        case '&': {
            advance();
            if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::AmpersandEqual;
            } else {
                token.kind = SyntaxToken::Kind::Ampersand;
            }
        } break;

        case '|': {
            advance();
            if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::PipeEqual;
            } else {
                token.kind = SyntaxToken::Kind::Pipe;
            }
        } break;

        case '~': {
            advance();
            if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::TildeEqual;
            } else {
                token.kind = SyntaxToken::Kind::Tilde;
            }
        } break;

        case '<': {
            advance();
            if (current() == ':') {
                advance();
                token.kind = SyntaxToken::Kind::LessColon;
            } else if (current() == '=') {
                advance();
                if (current() == ':') {
                    advance();
                    token.kind = SyntaxToken::Kind::LessEqualColon;
                } else {
                    token.kind = SyntaxToken::Kind::LessEqual;
                }
            } else if (current() == '<') {
                advance();
                if (current() == '=') {
                    advance();
                    token.kind = SyntaxToken::Kind::LessLessEqual;
                } else {
                    token.kind = SyntaxToken::Kind::LessLess;
                }
            } else if (current() == '-') {
                advance();
                token.kind = SyntaxToken::Kind::LessMinus;
            } else {
                token.kind = SyntaxToken::Kind::Less;
            }
        } break;

        case '>': {
            advance();
            if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::GreaterEqual;
            } else if (current() == '>') {
                advance();
                if (current() == '=') {
                    advance();
                    token.kind = SyntaxToken::Kind::GreaterGreaterEqual;
                } else {
                    token.kind = SyntaxToken::Kind::GreaterGreater;
                }
            } else {
                token.kind = SyntaxToken::Kind::Greater;
            }
        } break;

        case ':': {
            advance();
            if (current() == '>') {
                advance();
                token.kind = SyntaxToken::Kind::ColonGreater;
            } else if (current() == ':') {
                advance();
                token.kind = SyntaxToken::Kind::ColonColon;
            } else {
                token.kind = SyntaxToken::Kind::Colon;
            }
        } break;

        case '@': {
            advance();
            if (IsIdentifierStart(current())) {
                read_identifier_token(token, false);
            } else {
                Error(location(), "Expected an identifier. String-based identifiers are currently not support.");
            }
        } break;

        default: {
            if (IsIdentifierStart(current())) {
                read_identifier_token(token);
            } else {
                token.kind = SyntaxToken::Kind::Invalid;
                ErrorNoLoc("Invalid character in Laye source file");
                advance();
            }
        } break;
    }

    token.location.len = location().pos - token.location.pos;
    consume_trivia(token.trailing_trivia, true);

    return token;
}

void Lexer::Impl::read_identifier_token(SyntaxToken& token, bool transform_keywords) {
    auto start_location = token.location;

    string_builder.clear();
    while (not eof() and IsIdentifierContinue(current())) {
        string_builder.push_back(current());
        advance();
    }

    token.text = String::Save(syntax_module.string_saver, string_builder);
    token.kind = SyntaxToken::Kind::Identifier;
    //token.location.len = location().pos - token.location.pos;

    if (transform_keywords) {
        auto kind = keyword_kinds.find(string_builder);
        if (kind != keyword_kinds.end()) {
            token.kind = kind->getValue();
        }
    }

    string_builder.clear();
}

// ============================================================================
//  Public API
// ============================================================================

CHOIR_DEFINE_HIDDEN_IMPL(Lexer);
Lexer::Lexer(SyntaxModule& syntax_module, SyntaxTriviaMode trivia_mode) : impl(new Impl{syntax_module, trivia_mode}) {}

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
    Lexer lexer{syntax_module, trivia_mode};

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
