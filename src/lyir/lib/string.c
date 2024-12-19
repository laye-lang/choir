#include <kos/assert.h>
#include <kos/macros.h>
#include <kos/string.h>
#include <kos/types.h>

struct kos_string_view kos_string_view_c(const char* c) {
    return (struct kos_string_view){
        .data = c,
        .count = cast(isize) strlen(c),
    };
}
