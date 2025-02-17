#include <laye/laye.h>
#include <stdio.h>
#include <string.h>

#ifndef BUILD_VERSION
#    define BUILD_VERSION "<unknown>"
#endif // BUILD_VERSION

static const char* help_text =
    "Laye Module Compiler Version %s\n";

static void print_tokens(ly_token* tokens);

int main(int argc, char** argv) {
    int result = 0;

    ch_allocator default_allocator = ch_general_purpose_allocator();

    ch_context context = {0};
    context.string_store.allocator = default_allocator;

    ch_arena token_arena = {0};
    ch_arena_init(&token_arena, default_allocator, 4096 * sizeof(ly_token));
    ch_allocator token_arena_allocator = ch_arena_allocator(&token_arena);

    ch_source source = {0};
    source.text = "+ - * /";
    source.length = cast(int64) strlen(source.text);

    ly_token* tokens = ly_lex(&source, default_allocator, &context.string_store, LY_LEX_PRESERVE_TRIVIA);

    print_tokens(tokens);

defer:
    ch_allocator_deinit(token_arena_allocator);
    ch_allocator_deinit(default_allocator);
    return result;
}

static void print_trivia(ly_token* trivia) {
    while (trivia != NULL) {
        fprintf(stderr, "  %s\n", ly_token_kind_name_get(trivia->kind));
        trivia = trivia->next;
    }
}

static void print_tokens(ly_token* tokens) {
    while (tokens != NULL) {
        print_trivia(tokens->leading_trivia);
        fprintf(stderr, "%s: %.*s\n", ly_token_kind_name_get(tokens->kind), cast(int) tokens->lexeme_length, tokens->lexeme_begin);
        print_trivia(tokens->trailing_trivia);
        fprintf(stderr, "-----\n");
        tokens = tokens->next;
    }
}
