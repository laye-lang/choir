#include <laye/laye.h>
#include <stdio.h>
#include <string.h>

#ifndef BUILD_VERSION
#    define BUILD_VERSION "<unknown>"
#endif // BUILD_VERSION

static const char* help_text =
    "Laye Module Compiler Version %s\n";

static void print_tokens(ch_context* context, ly_token* tokens);

int main(int argc, char** argv) {
    int result = 0;

    ch_allocator default_allocator = ch_general_purpose_allocator();

    ch_context context = {0};
    ch_context_init(&context, default_allocator);

    ch_diag(&context, CH_DIAG_NOTE, CH_NOLOC, "this is a test");
    ch_diag(&context, CH_DIAG_WARN, CH_NOLOC, "this is a test");
    ch_diag(&context, CH_DIAG_ERROR, CH_NOLOC, "this is a test");

    ch_arena token_arena = {0};
    ch_arena_init(&token_arena, default_allocator, 4096 * sizeof(ly_token));
    ch_allocator token_arena_allocator = ch_arena_allocator(&token_arena);

    ch_source source = {0};
    source.name = "test_file";
    source.text = "+ - * /";
    source.length = cast(int64) strlen(source.text);

    ly_token* tokens = ly_lex(&context, &source, token_arena_allocator, LY_LEX_PRESERVE_TRIVIA);
    print_tokens(&context, tokens);

defer:
    ch_allocator_deinit(token_arena_allocator);
    ch_context_deinit(&context);
    ch_allocator_deinit(default_allocator);
    return result;
}

static void print_trivia(ch_context* context, ly_token* trivia) {
    while (trivia != NULL) {
        ch_diag(context, CH_DIAG_NOTE, trivia->location, "%s", ly_token_kind_name_get(trivia->kind));
        trivia = trivia->next;
    }
}

static void print_tokens(ch_context* context, ly_token* tokens) {
    while (tokens != NULL) {
        ch_diag(context, CH_DIAG_WARN, tokens->location, "%s", ly_token_kind_name_get(tokens->kind));
        ch_diag(context, CH_DIAG_NOTE, CH_NOLOC, "leading:");
        print_trivia(context, tokens->leading_trivia);
        ch_diag(context, CH_DIAG_NOTE, CH_NOLOC, "trailing:");
        print_trivia(context, tokens->trailing_trivia);
        tokens = tokens->next;
    }
}
