#include <kos/alloc.h>
#include <kos/assert.h>
#include <kos/macros.h>
#include <kos/types.h>
#include <liblaye.h>
#include <liblyir.h>
#include <stdio.h>

int main(int argc, char** argv) {
    int result = 0;

    struct lyir_state* state = lyir_create(default_allocator);
    struct laye_args args = {0};

    if (!laye_args_parse(state, argc, argv, &args)) {
        kos_return_defer(cast(int) LAYE_DRIVER_ARGS_PARSE_FAILURE);
    }

    enum laye_driver_status driver_status = laye_driver_run(state, &args);
    if (driver_status != LAYE_DRIVER_OK) {
        kos_return_defer(cast(int) driver_status);
    }

defer:;
    lyir_destroy(state);
    return result;
}
