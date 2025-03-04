#include <choir/choir.h>
#include <inttypes.h>
#include <stdarg.h>
#include <stdio.h>
#include <string.h>

CHOIR_API void ch_diag_flush(ch_context* context) {
    if (context->has_issued_diagnostics) {
        fprintf(stderr, "\n");
    }

    context->has_issued_diagnostics = true;

    // TODO(local): lots of diagnostic formatting to be done soon.
    for (int64 i = 0; i < context->queued_diagnostics.count; i++) {
        ch_diagnostic diag = context->queued_diagnostics.items[i];
        switch (diag.kind) {
            default: break;
            case CH_DIAG_NOTE: fprintf(stderr, "Note: "); break;
            case CH_DIAG_WARN: fprintf(stderr, "Warning: "); break;
            case CH_DIAG_ERROR: fprintf(stderr, "Error: "); break;
            case CH_DIAG_ICE: fprintf(stderr, "Internal Compiler Error: "); break;
        }

        if (diag.location.source != NULL) {
            fprintf(stderr, "%s:[%" PRIi64 ":%" PRIi64 "]: ", diag.location.source->name, diag.location.offset, diag.location.length);
        }

        fprintf(stderr, "%s\n", diag.message);
    }

    context->queued_diagnostics.count = 0;
}

static ch_string format_diag_message(ch_context* context, ch_diagnostic_kind kind, ch_location location, const char* format, va_list v0) {
    ch_string result = {0};
    result.allocator = context->allocator;
    result.capacity = 1024;
    result.items = ch_alloc(result.allocator, result.capacity);
    discard memset(result.items, 0, cast(size_t) result.capacity);

    return result;
}

CHOIR_API void ch_diag(ch_context* context, ch_diagnostic_kind kind, ch_location location, const char* format, ...) {
    if (kind != CH_DIAG_NOTE) {
        ch_diag_flush(context);
    }

    va_list v0, v1;
    va_start(v0, format);
    int message_length = vsnprintf(NULL, 0, format, v0);
    va_end(v0);

    va_start(v1, format);
    char* message = ch_alloc(context->allocator, message_length + 1);
    discard vsnprintf(message, message_length + 1, format, v1);
    va_end(v1);

    ch_diagnostic diag = {
        .kind = kind,
        .location = location,
        .message = message,
    };

    da_push(&context->queued_diagnostics, diag);

    if (kind == CH_DIAG_ICE) {
        ch_diag_flush(context);
        //abort();
        CHOIR_TRAP();
    }
}
