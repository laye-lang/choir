#include "types.h"

#include <kos/alloc.h>
#include <kos/assert.h>
#include <kos/macros.h>
#include <liblyir.h>

struct lyir_source {
    struct kos_string_view name;
    struct kos_string_view text;
};

struct lyir_state* lyir_create(struct kos_allocator allocator) {
    struct lyir_state* state = kos_alloc(allocator, sizeof *state);
    memset(state, 0, sizeof *state);
    state->allocator = allocator;
    return state;
}

void lyir_destroy(struct lyir_state* state) {
    KOS_ASSERT(state != nullptr, "State cannot be null.");

    lyir_diag_flush(state);

    struct kos_allocator allocator = state->allocator;
    kos_dealloc(allocator, state);
}
