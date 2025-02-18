#include <laye/laye.h>
#include <string.h>

struct lexer {
    ch_context* context;
    ch_source* source;
    ch_allocator token_allocator;
    
    bool preserve_trivia : 1;

    int64 position;
};

static ly_token* ly_read_token(struct lexer* l);

CHOIR_API ly_token* ly_lex(ch_context* context, ch_source* source, ch_allocator token_allocator, ly_lex_flag flags) {
    struct lexer lexer = {
        .context = context,
        .source = source,
        .token_allocator = token_allocator,
    };

    lexer.preserve_trivia = 0 != (flags & LY_LEX_PRESERVE_TRIVIA);

    ly_token* result = NULL;
    ly_token* previous = NULL;

    while (true) {
        ly_token* token = ly_read_token(&lexer);
        if (result == NULL) {
            result = token;
        } else {
            assert(previous != NULL && "where is previous");
            previous->next = token;
        }

        previous = token;
        if (token->kind == LY_TK_EOF) {
            break;
        }
    }

    assert(result != NULL && "did not read any tokens; at least EOF should have been read.");
    assert(lexer.position >= lexer.source->length && "did not consume enough characters from the source text.");

    return result;
}

static char lexer_peek(struct lexer* l, int64 ahead) {
    assert(l != NULL && "where is the lexer?");
    assert(l->source != NULL && "where is the source?");
    assert(ahead >= 0 && "cannot peek backwards");

    int64 position = l->position + ahead;
    if (position >= l->source->length) {
        return 0;
    }

    return l->source->text[position];
}

static char lexer_current(struct lexer* l) {
    return lexer_peek(l, 0);
}

static void lexer_advance(struct lexer* l) {
    if (l->position >= l->source->length) return;
    l->position++;
}

static bool ly_try_read_trivium(struct lexer* l, ly_token** out_trivia, bool* consumed_tailing_terminal) {
    ly_token* trivia = NULL;

    int64 start_position = l->position;
    char c = lexer_current(l);

    switch (c) {
        default: break;
        case ' ':
        case '\t': {
            lexer_advance(l);
            while ((c = lexer_current(l)), (c == ' ' || c == '\t')) {
                lexer_advance(l);
            }

            if (l->preserve_trivia) {
                trivia = ch_alloc(l->token_allocator, sizeof *trivia);
                memset(trivia, 0, sizeof *trivia);
                trivia->kind = LY_TK_WHITE_SPACE;
            }
        } break;
    }

    *out_trivia = trivia;
    if (trivia != NULL) {
        trivia->lexeme_begin = l->source->text + start_position;
        trivia->lexeme_length = l->position - start_position;

        trivia->location.source = l->source;
        trivia->location.offset = start_position;
        trivia->location.length = l->position - start_position;
    }

    return l->position > start_position;
}

static void ly_read_trivia(struct lexer* l, ly_token** trivia, bool is_trailing) {
    ly_token* current_trivia = NULL;
    ly_token* previous_trivia = NULL;

    bool consumed_tailing_terminal = false;
    while (ly_try_read_trivium(l, &current_trivia, &consumed_tailing_terminal)) {
        if (l->preserve_trivia) {
            if (*trivia == NULL) {
                *trivia = current_trivia;
            } else {
                previous_trivia->next = current_trivia;
            }

            previous_trivia = current_trivia;
        }

        if (is_trailing && consumed_tailing_terminal) {
            break;
        }
    }
}

static ly_token* ly_read_token(struct lexer* l) {
    ly_token* token = ch_alloc(l->token_allocator, sizeof *token);
    memset(token, 0, sizeof *token);
    ly_read_trivia(l, &token->leading_trivia, false);

    int64 start_position = l->position;
    char c = lexer_current(l);

    switch (c) {
        default: {
            assert(false && "unhandled character in Laye source");
        }

        case 0: {
            token->kind = LY_TK_EOF;
        } break;

        case '+': {
            lexer_advance(l);
            token->kind = LY_TK_PLUS;
        } break;

        case '-': {
            lexer_advance(l);
            token->kind = LY_TK_MINUS;
        } break;

        case '*': {
            lexer_advance(l);
            token->kind = LY_TK_STAR;
        } break;

        case '/': {
            lexer_advance(l);
            token->kind = LY_TK_SLASH;
        } break;
    }

    assert((l->position > start_position || token->kind == LY_TK_EOF) && "token read did not consume any characters");
    
    token->lexeme_begin = l->source->text + start_position;
    token->lexeme_length = l->position - start_position;

    token->location.source = l->source;
    token->location.offset = start_position;
    token->location.length = l->position - start_position;
    
    if (token->kind != LY_TK_EOF) {
        ly_read_trivia(l, &token->trailing_trivia, true);
    }

    return token;
}
