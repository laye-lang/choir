#include <choir/macros.hh>
#include <choir/front/laye.hh>
#include <llvm/ADT/APFloat.h>
#include <llvm/ADT/APInt.h>
#include <llvm/ADT/SmallString.h>
#include <llvm/ADT/StringExtras.h>
#include <llvm/ADT/StringMap.h>
#include <llvm/Support/ConvertUTF.h>
#include <llvm/Support/Error.h>
#include <mutex>
// #include <llvm/Support/UnicodeCharRanges.h>

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
    auto current() const -> char { return eof() ? 0 : *current_ptr; }
    auto peek() const -> char {
        if (current_ptr + 1 >= end_ptr) return 0;
        return *(current_ptr + 1);
    }

    auto location() -> Location {
        return {u32(current_ptr - begin()), 1, u16(source_file.file_id())};
    }

    auto consume_trivia(llvm::SmallVectorImpl<SyntaxTrivia>& trivia, bool is_trailing) -> void;
    auto read_token() -> SyntaxToken;

    void update_token_location(SyntaxToken& token) {
        token.location.len = location().pos - token.location.pos;
    }

    void read_token_from_digit_start(SyntaxToken& token, bool transform_keywords = true);
    void read_token_from_identifier_start(SyntaxToken& token, bool transform_keywords = true);
    void read_string(SyntaxToken& token);
    void read_rune(SyntaxToken& token);
    auto read_escape_sequence() -> i32;

    enum struct NumericComponentDigit {
        AnyBase10,
        AnyBase16,
        AnyBase36,
    };

    bool read_numeric_component_into_builder(i32 radix, NumericComponentDigit digit_mode = NumericComponentDigit::AnyBase10);
    void continue_read_numeric_token_from_radix(SyntaxToken& token, i32 radix);
    void continue_read_numeric_token_from_float_point(SyntaxToken& token, i32 radix);
    bool read_float_exponent();
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
            if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::StarEqual;
            } else {
                token.kind = SyntaxToken::Kind::Star;
            }
        } break;

        case '^': {
            advance();
            if (current() == '=') {
                advance();
                token.kind = SyntaxToken::Kind::CaretEqual;
            } else {
                token.kind = SyntaxToken::Kind::Caret;
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
                read_token_from_identifier_start(token, false);
            } else {
                read_string(token);
                token.kind = SyntaxToken::Kind::Identifier;
                // TODO(local): probably validate this is a sensible identifier by some rules
            }
        } break;

        case '"': {
            read_string(token);
        } break;

        case '\'': {
            read_rune(token);
        } break;

        case '0':
        case '1':
        case '2':
        case '3':
        case '4':
        case '5':
        case '6':
        case '7':
        case '8':
        case '9': {
            read_token_from_digit_start(token);
        } break;

        default: {
            if (IsIdentifierStart(current())) {
                read_token_from_identifier_start(token);
            } else {
                token.kind = SyntaxToken::Kind::Invalid;
                ErrorNoLoc("Invalid character in laye source file");
                advance();
            }
        } break;
    }

    update_token_location(token);
    consume_trivia(token.trailing_trivia, true);

    return token;
}

void Lexer::Impl::read_token_from_identifier_start(SyntaxToken& token, bool transform_keywords) {
    // if we're calling this function, we should have already determined that a number could not have possible started here.
    // this is important because we'd end up with cyclic calls if at least one of those paths didn't make this assumption.
    // (laye identifiers can start with decimal digits)

    string_builder.clear();
    while (not eof() and IsIdentifierContinue(current())) {
        string_builder.push_back(current());
        advance();
    }

    token.text = String::Save(syntax_module.string_saver, string_builder);
    token.kind = SyntaxToken::Kind::Identifier;
    // token.location.len = location().pos - token.location.pos;

    if (transform_keywords) {
        auto kind = keyword_kinds.find(string_builder);
        if (kind != keyword_kinds.end()) {
            token.kind = kind->getValue();
        }
    }

    string_builder.clear();
}

bool Lexer::Impl::read_numeric_component_into_builder(i32 radix, NumericComponentDigit digit_mode) {
    auto is_valid_digit = [radix, digit_mode](char c, bool is_strict) -> bool {
        if (digit_mode == NumericComponentDigit::AnyBase10) {
            return IsDecimalDigit(c);
        } else if (digit_mode == NumericComponentDigit::AnyBase16) {
            return IsDigitInRadix(c, 16);
        } else {
            return IsDigitInRadix(c, is_strict ? radix : 36);
        }
    };

    bool is_valid_literal_to_parse = true;

    while (not eof() and (is_valid_digit(current(), false) or current() == '_')) {
        if (current() == '_') {
            auto underscore_location = location();
            underscore_location.len--;

            while (current() == '_') {
                underscore_location.len++;
                advance();
            }

            if (eof() or not is_valid_digit(current(), false)) {
                Error(underscore_location, "Numeric literal component cannot end with '_'.");
                while (not eof() and IsIdentifierContinue(current())) {
                    advance();
                }

                break;
            }

            if (string_builder.empty()) {
                Error(underscore_location, "Numeric literal component cannot start with '_'.");
            }
        }

        if (not is_valid_digit(current(), true)) {
            is_valid_literal_to_parse = false;
            ErrorNoLoc("'{}' is not a valid digit in base {}.", current(), radix);
        }

        if (is_valid_literal_to_parse) {
            string_builder.push_back(current());
        }

        advance();
    }

    return is_valid_literal_to_parse;
}

void Lexer::Impl::read_token_from_digit_start(SyntaxToken& token, bool transform_keywords) {
    const char* token_start_ptr = current_ptr;

    string_builder.clear();
    bool is_valid_literal_to_parse = read_numeric_component_into_builder(10, NumericComponentDigit::AnyBase10);

    if (not is_valid_literal_to_parse or (not eof() and IsIdentifierContinue(current()))) {
        string_builder.clear();
        current_ptr = token_start_ptr;
        read_token_from_identifier_start(token, transform_keywords);
        return;
    }

    token.kind = SyntaxToken::Kind::LiteralInteger;
    token.integer_value = llvm::APInt{};
    llvm::StringRef{string_builder}.getAsInteger(10, token.integer_value);
    update_token_location(token);

    if (current() == '#') {
        i32 radix;
        if (token.integer_value.ult(2)) {
            // it can't possibly be a valid radix, since 36 is 6 bits
            Error(token.location, "Numeric literal base must be in the range [2, 36].");
            radix = 2;
        } else if (token.integer_value.ugt(36)) {
            // it can't possibly be a valid radix, since 36 is 6 bits
            Error(token.location, "Numeric literal base must be in the range [2, 36].");
            radix = 36;
        } else {
            radix = i32(token.integer_value.getZExtValue());
        }

        CHOIR_ASSERT(radix >= 2 and radix <= 36);
        token.integer_value = llvm::APInt{};
        string_builder.clear();
        continue_read_numeric_token_from_radix(token, radix);
    } else if (current() == '.') {
        continue_read_numeric_token_from_float_point(token, 10);
    }

    string_builder.clear();
}

void Lexer::Impl::continue_read_numeric_token_from_radix(SyntaxToken& token, i32 radix) {
    CHOIR_ASSERT(current() == '#');
    advance();

    string_builder.clear();
    bool is_valid_literal_to_parse = read_numeric_component_into_builder(radix, NumericComponentDigit::AnyBase36);

    if (current() == '.') {
        continue_read_numeric_token_from_float_point(token, radix);
        return;
    }

    token.kind = SyntaxToken::Kind::LiteralInteger;
    token.integer_value = llvm::APInt{};
    if (is_valid_literal_to_parse) {
        llvm::StringRef{string_builder}.getAsInteger(radix, token.integer_value);
        string_builder.clear();
    }
}

void Lexer::Impl::continue_read_numeric_token_from_float_point(SyntaxToken& token, i32 radix) {
    CHOIR_ASSERT(current() == '.');
    advance();

    // if we're called with an empty string builder, then the first half of the token is considered invalid for parsing.
    // we can continue lexing the token, but we won't attempt to parse it.
    bool is_valid_literal_to_parse = not string_builder.empty();
    if (radix != 10 and radix != 16) {
        is_valid_literal_to_parse = false;
        Error(token.location, "Only base 10 float literals are supported.");

        if (radix <= 10) {
            radix = 10;
        } else {
            radix = 16;
        }
    }

    if (is_valid_literal_to_parse) {
        string_builder.push_back('.');
    }

    CHOIR_ASSERT(radix == 10 or radix == 16);
    is_valid_literal_to_parse &= read_numeric_component_into_builder(radix, radix == 16 ? NumericComponentDigit::AnyBase16 : NumericComponentDigit::AnyBase10);

    if (radix == 10 and (current() == 'e' or current() == 'E')) {
        string_builder.push_back('e');
        advance();
        read_float_exponent();
    } else if (radix == 16) {
        if (current() != 'p' and current() != 'P') {
            ErrorNoLoc("Hexadecimal float literals require an exponent delimited by 'p'.");
            string_builder.push_back('p');
            string_builder.push_back('0');
        } else {
            advance();
            string_builder.push_back('p');
            read_float_exponent();
        }
    }

    token.kind = SyntaxToken::Kind::LiteralFloat;
    token.float_value = llvm::APFloat{0.0f};
    if (is_valid_literal_to_parse) {
        if (radix == 10) {
            auto result = token.float_value.convertFromString(llvm::StringRef{string_builder}, llvm::RoundingMode::NearestTiesToEven);
            if (auto e = result.takeError()) {
                update_token_location(token);
                ICE(token.location, "Failed to parse float");
            }
        } else if (radix == 16) {
            string_builder.insert(string_builder.begin(), {'0', 'x'});
            auto result = token.float_value.convertFromString(llvm::StringRef{string_builder}, llvm::RoundingMode::NearestTiesToEven);
            if (auto e = result.takeError()) {
                update_token_location(token);
                ICE(token.location, "Failed to parse float");
            }
        } else {
            CHOIR_UNREACHABLE();
        }
    }

    string_builder.clear();
}

bool Lexer::Impl::read_float_exponent() {
    bool is_valid_literal_to_parse = true;

    if (current() == '-') {
        advance();
        string_builder.push_back('-');
    }

    if (current() == '_') {
        auto underscore_location = location();
        underscore_location.len--;

        while (current() == '_') {
            underscore_location.len++;
            advance();
        }

        Error(underscore_location, "Float exponent cannot contain '_'.");
    }

    while (not eof() and (IsDigitInRadix(current(), 36) or current() == '_')) {
        if (current() == '_') {
            auto underscore_location = location();
            underscore_location.len--;

            while (current() == '_') {
                underscore_location.len++;
                advance();
            }

            Error(underscore_location, "Numeric literal component cannot contain '_'.");
        }

        if (not IsDecimalDigit(current())) {
            is_valid_literal_to_parse = false;
            ErrorNoLoc("'{}' is not a valid decimal digit.", current());
        }

        if (is_valid_literal_to_parse) {
            string_builder.push_back(current());
        }

        advance();
    }

    return is_valid_literal_to_parse;
}

void Lexer::Impl::read_string(SyntaxToken& token) {
    CHOIR_ASSERT(current() == '"');
    advance();

    Location character_location{};

    char utf8[UNI_MAX_UTF8_BYTES_PER_CODE_POINT]{};
    auto append_codepoint = [&](i32 codepoint_value) {
        char* buf = utf8;
        if (llvm::ConvertCodePointToUTF8(codepoint_value, buf)) {
            for (char* bytes = utf8; bytes < buf; bytes++) {
                string_builder.push_back(*bytes);
            }
        } else {
            character_location.len = location().pos - character_location.pos;
            Error(character_location, "Invalid Unicode codepoint in escape sequence.");
            string_builder.push_back('?');
        }
    };

    while (not eof() and current() != '"') {
        character_location = location();
        if (current() == '\\') {
            i32 escaped_character = read_escape_sequence();
            append_codepoint(escaped_character);
        } else {
            string_builder.push_back(current());
            advance();
        }
    }

    CHOIR_TODO("read_string");
}

void Lexer::Impl::read_rune(SyntaxToken& token) {
    CHOIR_ASSERT(current() == '\'');
    CHOIR_TODO("read_rune");
}

i32 Lexer::Impl::read_escape_sequence() {
    CHOIR_ASSERT(current() == '\\');

    auto escape_location = location();
    advance();

    if (eof()) {
        ErrorNoLoc("End of file in escape sequence.");
        return 0;
    }

    auto read_codepoint = [&](int width) -> int {
        CHOIR_ASSERT(width == 4 or width == 8);

        llvm::SmallString<8> hex_image{};
        for (int i = 1; i < width; i++) {
            if (eof() or not IsDigitInRadix(current(), 16)) {
                if (width == 4) {
                    Error(escape_location, "Escape sequence '\\u' requires exactly four hexadecimal digits.");
                } else {
                    Error(escape_location, "Escape sequence '\\U' requires exactly eight hexadecimal digits.");
                }
                break;
            }

            hex_image.push_back(current());
            advance();
        }

        llvm::APInt hex_value{};
        llvm::StringRef{hex_image}.getAsInteger(16, hex_value);

        return int(hex_value.getZExtValue());
    };

    switch (current()) {
        case 'a': {
            advance();
            return '\a';
        }

        case 'b': {
            advance();
            return '\b';
        }

        case 'f': {
            advance();
            return '\f';
        }

        case 'n': {
            advance();
            return '\n';
        }

        case 'r': {
            advance();
            return '\r';
        }

        case 't': {
            advance();
            return '\t';
        }

        case 'v': {
            advance();
            return '\v';
        }

        case '\\': {
            advance();
            return '\\';
        }

        case '\'': {
            advance();
            return '\'';
        }

        case '"': {
            advance();
            return '"';
        }

        case 'u': {
            advance();
            return read_codepoint(4);
        }

        case 'U': {
            advance();
            return read_codepoint(8);
        }

        case 'x': {
            advance();

            if (eof() or not IsDigitInRadix(current(), 16)) {
                escape_location.len = location().pos - escape_location.pos;
                Error(escape_location, "Escape sequence '\\x' requires at least one hexadecimal digit.");
                return '?';
            }

            llvm::SmallString<2> hex_image{};
            for (int i = 1; i < 2 and IsDigitInRadix(current(), 16); i++) {
                hex_image.push_back(current());
                advance();
            }

            llvm::APInt hex_value{};
            llvm::StringRef{hex_image}.getAsInteger(16, hex_value);

            return char(hex_value.getZExtValue());
        }

        default: {
            if (IsDigitInRadix(current(), 8)) {
                llvm::SmallString<3> octal_image{};
                while (not eof() and IsDigitInRadix(current(), 8) and octal_image.size() < 3) {
                    octal_image.push_back(current());
                    advance();
                }

                llvm::APInt octal_value{};
                llvm::StringRef{octal_image}.getAsInteger(8, octal_value);

                return char(octal_value.getZExtValue());
            } else {
                char the_character = current();
                ErrorNoLoc("Invalid escape sequence.");
                advance();
                return the_character;
            }
        }
    }

    CHOIR_UNREACHABLE();
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
    CHOIR_ASSERT(c >= '0' and c <= '9');
    return c - '0';
}

bool choir::laye::IsDigitInRadix(char c, int radix) {
    if (radix <= 10) {
        return c >= '0' and c <= '0' + (radix - 1);
    }

    return (c >= '0' and c <= '9') or (c >= 'a' and c <= 'a' + (radix - 11)) or (c >= 'A' and c <= 'A' + (radix - 11));
}

int choir::laye::DigitValueInRadix(char c, int radix) {
    if (c <= '9') return DigitValue(c);
    CHOIR_ASSERT((c >= 'a' and c <= 'a' + (radix - 11)) or (c >= 'A' and c <= 'A' + (radix - 11)));
    if (c >= 'a' and c <= 'z') return 11 + (c - 'a');
    return 11 + (c - 'A');
}
