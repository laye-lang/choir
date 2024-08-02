#include <choir/front/laye.hh>
#include <choir/macros.hh>
#include <filesystem>
#include <mutex>
#include <vector>

using namespace choir;
using namespace choir::laye;

using Tk = SyntaxToken::Kind;

// ============================================================================
//  Internal API
// ============================================================================

// ============================================================================
//  Implementation
// ============================================================================

struct Parser::Impl : DiagsProducer<Parser::Impl> {
    friend DiagsProducer;
    
    enum struct TokenClass {
        StmtDelimiter,
        DeclStart,
        DeclEnd,
        StmtStart,
        StmtEnd,
        ExprStart,
        ExprEnd,
    };
    
    std::mutex mutex{};

    Context& context;
    SyntaxModule::Ptr module_ptr;

    isz token_index = 0;
    std::span<SyntaxToken> tokens;

    Impl(const File& source_file)
        : context(source_file.context()), module_ptr{std::make_unique<SyntaxModule>(source_file)} {
        Lexer::ReadTokens(*module_ptr);

        tokens = module_ptr->tokens();
        CHOIR_ASSERT(tokens.size() >= 1 and tokens.back().kind == Tk::EndOfFile);
    }

    template <typename... Args>
    void Diag(Diagnostic::Level level, Location loc, std::format_string<Args...> fmt, Args&&... args) {
        context.diags().diag(level, loc, fmt, std::forward<Args>(args)...);
    }
    
    auto module() const -> SyntaxModule* { return module_ptr.get(); }

    auto eof() const -> bool { return token()->kind == Tk::EndOfFile; }
    auto token() const -> SyntaxToken* { return &tokens[token_index]; }
    auto token_peek(isz ahead = 1) const -> SyntaxToken* {
        isz peek_index = token_index + ahead;
        if (peek_index >= tokens.size()) peek_index = tokens.size() - 1;
        CHOIR_ASSERT(peek_index >= 0 and peek_index < tokens.size());
        return &tokens[peek_index];
    }

    [[nodiscard]]
    bool check_at(auto... kinds) const { return ((token()->kind == kinds) or ...); }
    [[nodiscard]]
    bool check_ident(StringRef ident_text) const { return token()->kind == Tk::Identifier and token()->text == ident_text; }
    [[nodiscard]]
    auto check_peek(isz ahead, auto... kinds) const { return ((token_peek(ahead)->kind == kinds) or ...); }
    [[nodiscard]]
    auto check_peek_ident(isz ahead, StringRef ident_text) const { return token_peek(ahead)->kind == Tk::Identifier and token_peek(ahead)->text == ident_text; }

    bool token_is_stmt_delimiter() const {
        switch (token()->kind) {
            default: return false;
            case Tk::SemiColon:
                return true;
        }
    }

    bool token_is_decl_start() const {
        switch (token()->kind) {
            default: return false;
            case Tk::Foreign:
            case Tk::Export:
            case Tk::Callconv:
            case Tk::Inline:
            case Tk::Pure:
            case Tk::Discardable:

            case Tk::Template:
            case Tk::Import:
            case Tk::Struct:
            case Tk::Enum:
            case Tk::Alias:
            case Tk::Test:

            case Tk::Strict:
                return true;
                
            case Tk::Identifier:
                return check_ident("strict") and check_peek(1, Tk::Alias);
        }
    }

    bool token_is_decl_end() const {
        switch (token()->kind) {
            default: return false;
            case Tk::SemiColon:
            case Tk::CloseBrace:
                return true;
        }
    }

    bool token_is_stmt_start() const {
        switch (token()->kind) {
            default: return false;
            case Tk::OpenBrace:

            case Tk::If:
            case Tk::For:
            case Tk::While:
            case Tk::Do:
            case Tk::Switch:

            case Tk::Assert:
            case Tk::Xyzzy:
            case Tk::Defer:
            case Tk::Discard:
            case Tk::Delete:
            case Tk::Return:
            case Tk::Continue:
            case Tk::Break:
            case Tk::Unreachable:
            case Tk::Fallthrough:
                return true;
        }
    }

    auto token_is_stmt_end() const {
        switch (token()->kind) {
            default: return false;
            case Tk::SemiColon:
            case Tk::CloseBrace:
                return true;
        }
    }

    auto token_is_expr_start() const {
        switch (token()->kind) {
            default: return false;
            case Tk::Identifier:
            case Tk::LiteralString:
            case Tk::LiteralRune:
            case Tk::LiteralFloat:
            case Tk::LiteralInteger:
            case Tk::True:
            case Tk::False:
            case Tk::Nil:

            case Tk::Not:
            case Tk::New:
            case Tk::Cast:
            case Tk::Try:
            case Tk::Catch:
            
            case Tk::Sizeof:
            case Tk::Alignof:
            case Tk::Offsetof:

            case Tk::ColonColon:
                return true;
        }
    }

    auto token_is_expr_end() const {
        switch (token()->kind) {
            default: return false;
            case Tk::SemiColon:
            case Tk::CloseParen:
            case Tk::CloseBracket:
            case Tk::CloseBrace:
                return true;
        }
    }

    bool token_is_of_class_single(TokenClass c) const {
        switch (c) {
            case TokenClass::StmtDelimiter: return token_is_stmt_delimiter();
            case TokenClass::DeclStart: return token_is_decl_start();
            case TokenClass::DeclEnd: return token_is_decl_end();
            case TokenClass::StmtStart: return token_is_stmt_start();
            case TokenClass::StmtEnd: return token_is_stmt_end();
            case TokenClass::ExprStart: return token_is_expr_start();
            case TokenClass::ExprEnd: return token_is_expr_end();
        }
    }

    bool token_is_of_class(auto... c) const {
        return (token_is_of_class_single(c) or ...);
    }

    auto advance() -> void {
        if (token_index >= tokens.size() - 1) {
            token_index = tokens.size() - 1;
        } else {
            token_index++;
        }
        CHOIR_ASSERT(token_index >= 0 and token_index < tokens.size());
    }

    auto advance_until(auto... kinds) {
        std::vector<SyntaxToken*> result_tokens{};
        while (not check_at(kinds...)) {
            result_tokens.push_back(token());
            advance();
        }
        return result_tokens;
    }

    auto advance_until_class(auto... classes) {
        std::vector<SyntaxToken*> result_tokens{};
        while (not token_is_of_class(classes...)) {
            result_tokens.push_back(token());
            advance();
        }
        return result_tokens;
    }
    
    [[nodiscard]]
    auto expect_ident() -> SyntaxToken* {
        if (not check_at(Tk::Identifier)) {
            auto location = token()->location;
            // NOTE(local): should this token have length 0? 1?
            Error(location, "Expected an identifier.");
            return module()->invalid_token();
        }

        auto token_result = token();
        advance();

        return token_result;
    }

    [[nodiscard]]
    auto expect_semi(bool emit_error = true) -> SyntaxToken* {
        if (not check_at(Tk::SemiColon)) {
            auto location = token()->location;
            // NOTE(local): should this token have length 0? 1?
            if (emit_error) Error(location, "Expected a semi-colon.");
            return module()->invalid_token();
        }

        auto token_result = token();
        advance();

        return token_result;
    }

    auto consume(auto... kinds) -> bool {
        if (check_at(kinds...)) {
            advance();
            return true;
        }
        return false;
    }

    auto parse_statement() -> SyntaxNode*;
    auto parse_declaration() -> SyntaxNode*;
    auto parse_expression() -> SyntaxNode*;

    auto parse_import_declaration() -> SyntaxNode*;
};

auto Parser::Impl::parse_statement() -> SyntaxNode* {
    CHOIR_TODO("Parser::Impl::parse_statement");
}

auto Parser::Impl::parse_declaration() -> SyntaxNode* {
    if (check_at(Tk::Import)) {
        return parse_import_declaration();
    }

    CHOIR_TODO("Parser::Impl::parse_declaration");
}

auto Parser::Impl::parse_expression() -> SyntaxNode* {
    CHOIR_TODO("Parser::Impl::parse_expression");
}

auto Parser::Impl::parse_import_declaration() -> SyntaxNode* {
    CHOIR_ASSERT(check_at(Tk::Import));

    auto token_import = token();
    consume(Tk::Import);

    if (not check_at(Tk::LiteralString, Tk::Identifier)) {
        Error(token()->location, "Expected a string literal or an identifier in an import declaration");
        auto consumed_tokens = advance_until_class(TokenClass::StmtDelimiter, TokenClass::DeclStart, TokenClass::DeclEnd, TokenClass::StmtStart);
        auto token_semi = expect_semi(false);
        return new (module()) SyntaxImportInvalidWithTokens{token_import, token_semi, std::move(consumed_tokens)};
    }

    if (check_at(Tk::LiteralString)) {
        auto token_path = token();
        consume(Tk::LiteralString);

        if (check_ident("as")) {
            auto token_as = token();
            token_as->kind = SyntaxToken::Kind::As;
            advance();

            auto token_alias_ident = expect_ident();
            auto token_semi = expect_semi();

            return new (module()) SyntaxImportPathSimpleAliased{token_import, token_path, token_as, token_alias_ident, token_semi};
        }

        auto token_semi = expect_semi();
        return new (module()) SyntaxImportPathSimple{token_import, token_path, token_semi};
    }

    CHOIR_ASSERT(check_at(Tk::Identifier));

    if (check_peek(1, Tk::ColonColon)) {
        // should be the case of
        //   - import foo::bar ...;
        CHOIR_TODO("import foo::bar ...;");
    } else if (check_peek_ident(1, "as")) {
        // either the case of:
        //   1. import foo as bar;
        //   2. import foo as bar from ...;
        CHOIR_TODO("import foo as bar ...;");
    } else if (check_peek_ident(1, "from")) {
        // should be the case of
        //   - import foo from ...;
        CHOIR_TODO("import foo from ...;");
    } else {
        // should be the case of
        //   - import foo;

        auto token_name = token();
        CHOIR_ASSERT(token_name->kind == Tk::Identifier);
        advance();

        if (check_ident("as")) {
            auto token_as = token();
            token_as->kind = SyntaxToken::Kind::As;
            advance();

            auto token_alias_ident = expect_ident();
            auto token_semi = expect_semi();

            return new (module()) SyntaxImportNamedSimpleAliased{token_import, token_name, token_as, token_alias_ident, token_semi};
        }

        auto token_semi = expect_semi();
        return new (module()) SyntaxImportNamedSimple{token_import, token_name, token_semi};
    }

    CHOIR_UNREACHABLE();
}

// ============================================================================
//  Public API
// ============================================================================

CHOIR_DEFINE_HIDDEN_IMPL(Parser);
Parser::Parser(const File& source_file) : impl(new Impl{source_file}) {}

auto Parser::context() const -> Context& {
    std::unique_lock _{impl->mutex};
    return impl->context;
}

auto Parser::module() const -> SyntaxModule* {
    std::unique_lock _{impl->mutex};
    return impl->module();
}

auto Parser::tokens() const -> std::span<const SyntaxToken> {
    std::unique_lock _{impl->mutex};
    return impl->tokens;
}

auto Parser::eof() const -> bool {
    std::unique_lock _{impl->mutex};
    return impl->eof();
}

auto Parser::parse_statement() -> SyntaxNode* {
    std::unique_lock _{impl->mutex};
    return impl->parse_statement();
}

auto Parser::parse_declaration() -> SyntaxNode* {
    std::unique_lock _{impl->mutex};
    return impl->parse_declaration();
}

auto Parser::parse_expression() -> SyntaxNode* {
    std::unique_lock _{impl->mutex};
    return impl->parse_expression();
}

auto Parser::Parse(const File& source_file) -> SyntaxModule::Ptr {
    Parser parser{source_file};
    if (parser.eof()) {
        return std::move(parser.impl->module_ptr);
    }

    while (not parser.eof()) {
        auto node = parser.parse_declaration();
        parser.module()->append_top_level_declaration(node);
    }

    return std::move(parser.impl->module_ptr);
}
