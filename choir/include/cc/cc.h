#ifndef CC_H_
#define CC_H_

#include <choir/choir.h>

typedef enum cc_language {
    CC_LANG_UNKNOWN = 0,

    CC_LANG_C = 1,

    CC_LANG_COUNT,
} cc_language;

typedef enum cc_features {
    CC_FEAT_NONE = 0,

    CC_FEAT_LINE_COMMENT = 1 << 0,
    CC_FEAT_C99 = 1 << 1,
    CC_FEAT_C11 = 1 << 2,
    CC_FEAT_C17 = 1 << 3,
    CC_FEAT_C23 = 1 << 4,
    CC_FEAT_C2Y = 1 << 5,

    CC_FEAT_DIGRAPHS = 1 << 13,
    CC_FEAT_GNUMODE = 1 << 14,
    CC_FEAT_HEXFLOAT = 1 << 15,
    CC_FEAT_MSVCMODE = 1 << 16,
    CC_FEAT_CHOIRMODE = 1 << 17,
} cc_features;

typedef enum cc_standard_kind {
    CC_STD_UNSPECIFIED,
#define CC_STD(Id, ...) CC_STD_##Id,
#include <cc/std.inc>
} cc_standard_kind;

typedef struct cc_standard {
    const char* short_name;
    const char* description;
    cc_features features;
    cc_language language;
} cc_standard;

CHOIR_API bool cc_standard_has_line_comments(const cc_standard* std);
CHOIR_API bool cc_standard_has_digraphs(const cc_standard* std);
CHOIR_API bool cc_standard_has_hex_floats(const cc_standard* std);
CHOIR_API bool cc_standard_is_gnumode(const cc_standard* std);
CHOIR_API bool cc_standard_is_msvcmode(const cc_standard* std);
CHOIR_API bool cc_standard_is_choirmode(const cc_standard* std);
CHOIR_API bool cc_standard_is_c99(const cc_standard* std);
CHOIR_API bool cc_standard_is_c11(const cc_standard* std);
CHOIR_API bool cc_standard_is_c17(const cc_standard* std);
CHOIR_API bool cc_standard_is_c23(const cc_standard* std);
CHOIR_API bool cc_standard_is_c2y(const cc_standard* std);
CHOIR_API bool cc_standard_has_raw_string_literals(const cc_standard* std);

CHOIR_API cc_standard_kind cc_get_language_standard_kind(const char* lang_name);
CHOIR_API cc_standard cc_get_default_standard(ch_context* context, cc_language language);
CHOIR_API cc_standard cc_get_standard_for_kind(ch_context* context, cc_standard_kind kind);
CHOIR_API cc_standard cc_get_standard_for_name(ch_context* context, const char* lang_name);

typedef struct cc_include_dirs {
    ch_allocator allocator;
    const char** items;
    int64 count, capacity;
} cc_include_dirs;

typedef enum cc_token_kind {
#define CC_TOKEN(Name) LY_TK_##Name,
#include <cc/tokens.inc>
} cc_token_kind;

typedef struct cc_token {
    struct cc_token* next;
    /// @brief The beginning of this token's text, its "lexeme", in the C source.
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
    cc_token_kind kind;
    /// @brief Where this token came from in the source text.
    ch_location location;

    struct cc_token* leading_trivia;
    struct cc_token* trailing_trivia;

    union {
        const char* string_value;
        int64 integer_value;
    };
} cc_token;

typedef enum cc_lex_flag {
    CC_LEX_NONE = 0,
    CC_LEX_PRESERVE_TRIVIA = 1 << 0,
} cc_lex_flag;

CHOIR_API cc_token* cc_lex(ch_context* context, ch_source* source, ch_allocator token_allocator, cc_lex_flag flags);
CHOIR_API cc_token* cc_preprocess(ch_context* context, cc_token* tokens, ch_allocator token_allocator, cc_include_dirs include_dirs);

#endif // CC_H_
