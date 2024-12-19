#include <kos/alloc.h>
#include <kos/assert.h>
#include <kos/macros.h>
#include <kos/types.h>
#include <liblyir.h>
#include <stdio.h>

int main(int argc, char** argv) {
    int result = 0;

    struct lyir_state* state = lyir_create(default_allocator);
    struct lyir_args args = {0};

    if (!lyir_args_parse(state, argc, argv, &args)) {
        kos_return_defer(cast(int) LYIR_DRIVER_ARGS_PARSE_FAILURE);
    }

    enum lyir_driver_status driver_status = lyir_driver_run(state, &args);
    if (driver_status != LYIR_DRIVER_OK) {
        kos_return_defer(cast(int) driver_status);
    }

defer:;
    lyir_destroy(state);
    return result;
}
