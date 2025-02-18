#include <choir/choir.h>
#include <string.h>

CHOIR_API void ch_context_init(ch_context* context, ch_allocator allocator) {
    memset(context, 0, sizeof *context);
    context->allocator = allocator;
    context->string_store.allocator = allocator;
    context->queued_diagnostics.allocator = allocator;
}

CHOIR_API void ch_context_deinit(ch_context* context) {
    ch_diag_flush(context);

    da_free(&context->string_store);
    da_free(&context->queued_diagnostics);

    memset(context, 0, sizeof *context);
}
