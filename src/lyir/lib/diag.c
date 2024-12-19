#include "types.h"

#include <inttypes.h>
#include <kos/alloc.h>
#include <kos/assert.h>
#include <kos/macros.h>
#include <kos/string.h>
#include <kos/types.h>
#include <liblyir.h>
#include <stdarg.h>
#include <stdio.h>
#include <string.h>

static void trivial_print_diag(struct lyir_state* state, struct lyir_diagnostic* diag) {
    bool has_span = diag->span.source_id != 0;
    if (has_span) {
        lyir_source_id source_id = diag->span.source_id;

        struct kos_string_view source_name = lyir_source_name_get(state, source_id);
        //struct kos_string_view source_text = lyir_source_text_get(state, source_id);

        switch (state->diagnostic_span_format) {
            default: KOS_UNREACHABLE("Unhandled span format."); break;
            case LYIR_SPAN_LINE_COLUMN: {
                struct lyir_location_info_short locinfo = lyir_seek_short(state, diag->span);
                fprintf(stderr, KOS_STR_FORMAT ":(%"PRIi64":%"PRIi64"): ", KOS_STR_EXPAND(source_name), locinfo.line, locinfo.column);
            } break;

            case LYIR_SPAN_OFFSET_LENGTH: {
                isize length = diag->span.end - diag->span.begin;
                fprintf(stderr, KOS_STR_FORMAT ":[%td:%td]: ", KOS_STR_EXPAND(source_name), diag->span.begin, length);
            } break;
        }
    }

    switch (diag->kind) {
        default: KOS_UNREACHABLE("Unhandled diagnostic kind."); break;
        case LYIR_NOTE: fprintf(stderr, "Note: "); break;
        case LYIR_WARNING: fprintf(stderr, "Warning: "); break;
        case LYIR_ERROR: fprintf(stderr, "Error: "); break;
        case LYIR_ICE: fprintf(stderr, "Internal Compiler Error: "); break;
    }

    fprintf(stderr, "%s\n", diag->message);
}

void lyir_diag_flush(struct lyir_state* state) {
    KOS_ASSERT(state != nullptr, "State cannot be null.");
    if (state->ndiags == 0) {
        return;
    }

    KOS_ASSERT(state->diag_buffer[0].kind != LYIR_NOTE, "First diagnostic cannot be a note.");
    for (isize i = 1, ndiags = state->ndiags; i < ndiags; i++) {
        KOS_ASSERT(state->diag_buffer[i].kind == LYIR_NOTE, "Non-first diagnostics should have only been notes.");
    }

    for (isize i = 0, ndiags = state->ndiags; i < ndiags; i++) {
        trivial_print_diag(state, &state->diag_buffer[i]);
        kos_dealloc(state->allocator, state->diag_buffer[i].message);
    }

    memset(state->diag_buffer, 0, cast(usize) state->ndiags * sizeof(struct lyir_diagnostic));
    state->ndiags = 0;
}

void lyir_diag(struct lyir_state* state, enum lyir_diagnostic_kind kind, struct lyir_span span, const char* format, ...) {
    va_list v;
    va_start(v, format);
    lyir_diag_v(state, kind, span, format, v);
    va_end(v);
}

void lyir_diag_v(struct lyir_state* state, enum lyir_diagnostic_kind kind, struct lyir_span span, const char* format, va_list v) {
    KOS_ASSERT(state != nullptr, "State cannot be null.");
    KOS_ASSERT(format != nullptr, "Format cannot be null.");

    if (kind != LYIR_NOTE) {
        lyir_diag_flush(state);
    } else {
        KOS_ASSERT(state->ndiags > 0, "should only have issued a note after a non-note diagnostic.");
    }

    KOS_ASSERT(state->ndiags < DIAG_BUFFER_COUNT, "too many diagnostics queued. oops.");

    va_list v1;
    va_copy(v1, v);
    int nbuf = vsnprintf(nullptr, 0, format, v1);
    va_end(v1);

    char* message = kos_alloc(state->allocator, cast(isize)(nbuf + 1));
    int written = vsnprintf(message, cast(usize)(nbuf + 1), format, v);

    KOS_ASSERT(written == nbuf, "vsnprintf performed differently");

    state->diag_buffer[state->ndiags++] = (struct lyir_diagnostic){
        .span = span,
        .message = message,
        .kind = kind,
    };
}
