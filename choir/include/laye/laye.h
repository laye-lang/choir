#ifndef LAYE_H_
#define LAYE_H_

#if defined(__cplusplus)
extern "C" {
#endif // defined(__cplusplus)

#include <choir/choir.h>
#include <choir/config.h>
#include <laye/macros.h>

/// @brief Describes the kind of a Laye source text token.
/// @ref ly_token
typedef enum ly_token_kind {
#define LY_TOKEN(Name)   LY_TK_##Name,
#define LY_TOKEN_MISSING LY_TK_MISSING = 256,
#include <laye/tokens.inc>
} ly_token_kind;

/// @brief Returns the name of the enum constant associated with this Laye token kind.
CHOIR_API const char* ly_token_kind_name_get(ly_token_kind kind);

/// @brief
typedef struct ly_token {
    struct ly_token* next;
    /// @brief The beginning of this token's text, its "lexeme", in the Laye source.
    /// @details This is a pointer within the loaded source text of the file, wherever that may have come from.
    /// This means that the lifetime of source text is expected to be as long as the lifetime of tokens.
    /// Usually, we should not need source information (or tokens) after a full semantic analysis pass has completed, since by the time we're generating code for the program all user errors should be gone (and we should be past any place an LSP should care about).
    /// With that said, I have found it useful to emit diagnostics with source location information even past semantic analysis, since that can help pinpoint what context triggers an ICE diagnostic.
    /// Maybe source text, and by extension tokens and their lexemes, should live for the entire duration of the program.
    const char* lexeme_begin;
    /// @brief The length of this token's text, its "lexeme".
    /// @details Hypothetically, this length *could* be specified as an int64_t, but we may already see issues with tooling if a single token is 2GB long.
    /// Technically my only "worry" is calling `printf("%.*s", ...)` on lexeme data, since the count of that format needs to evaluate to an `int`.
    int lexeme_length;
    /// @brief The distinct kind of this token.
    /// @details The kind of a token determines what (sometimes broad) purpose it serves to the syntactic and semantic meaning of the source text.
    /// Some kinds, such as specific operators and keywords, are very consistent visually while others, like identifiers or literals, are more of a class of similar-looking inputs.
    ly_token_kind kind;
    /// @brief Where this token came from in the source text.
    ch_location location;

    struct ly_token* leading_trivia;
    struct ly_token* trailing_trivia;

    union {
        const char* string_value;
        int64 integer_value;
    };
} ly_token;

typedef enum ly_syntax_kind {
    LY_SN_NONE = 0,
} ly_syntax_kind;

typedef struct ly_syntax {
    /// @brief The distinct kind of this syntax node.
    ly_syntax_kind kind;
    ch_location location;
} ly_syntax;

typedef struct ly_ast_header {
    int dummy;
} ly_ast_header;

typedef struct ly_ast {
    int dummy;
} ly_ast;

typedef struct ly_module {
    ch_sources sources;
} ly_module;

typedef enum ly_lex_flag {
    LY_LEX_NONE = 0,
    LY_LEX_PRESERVE_TRIVIA = 1 << 0,
} ly_lex_flag;

CHOIR_API ly_token* ly_lex(ch_context* context, ch_source* source, ch_allocator token_allocator, ly_lex_flag flags);

#if defined(__cplusplus)
}
#endif // defined(__cplusplus)

#endif // LAYE_H_
