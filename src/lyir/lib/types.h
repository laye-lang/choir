#include <kos/alloc.h>
#include <kos/assert.h>
#include <kos/macros.h>
#include <kos/string.h>
#include <kos/types.h>
#include <liblyir.h>

#define DIAG_BUFFER_COUNT 16

struct lyir_diagnostic {
    struct lyir_span span;
    // allocated by state->allocator
    char* message;
    enum lyir_diagnostic_kind kind;
};

struct lyir_state {
    struct kos_allocator allocator;

    enum lyir_diagnostic_span_format diagnostic_span_format;
    struct lyir_diagnostic diag_buffer[DIAG_BUFFER_COUNT];
    isize ndiags;
};
