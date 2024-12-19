#include <kos/assert.h>
#include <kos/macros.h>
#include <kos/types.h>
#include <liblyir.h>

struct lyir_span lyir_span(lyir_source_id source_id, isize begin, isize end) {
    return (struct lyir_span) {
        .source_id = source_id,
        .begin = begin,
        .end = end,
    };
}
struct lyir_location_info_short lyir_seek_short(struct lyir_state* state, struct lyir_span span) {
    return (struct lyir_location_info_short) {
        .line = 1,
        .column = 1,
    };
}

struct lyir_location_info lyir_seek(struct lyir_state* state, struct lyir_span span) {
    return (struct lyir_location_info) {
        .line = 1,
        .column = 1,
        .line_begin = span.begin,
        .line_end = span.end,
        .line_text = (struct kos_string_view){0},
    };
}