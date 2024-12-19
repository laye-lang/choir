#include <kos/alloc.h>
#include <kos/assert.h>
#include <kos/macros.h>
#include <kos/types.h>
#include <kos/string.h>
#include <liblyir.h>

enum lyir_driver_status lyir_driver_run(struct lyir_state* state, struct lyir_args* args) {
    KOS_ASSERT(state != nullptr, "State cannot be null.");
    KOS_ASSERT(args != nullptr, "Args cannot be null.");

    struct kos_string_view source_name = kos_string_view_c("foo.laye");
    struct kos_string_view source_text = kos_string_view_c("int main() {\n    return 0;\n}");
    lyir_source_id source_id = lyir_source_add(state, source_name, source_text);

    struct lyir_span span = lyir_span(source_id, 17, 23);
    
    lyir_diag(state, LYIR_WARNING, LYIR_NO_SPAN, "This is a warning with no span.");
    lyir_diag(state, LYIR_ERROR, span, "This is an error with a span.");

    return LYIR_DRIVER_OK;
}

bool lyir_args_parse(struct lyir_state* state, int argc, char** argv, struct lyir_args* args) {
    KOS_ASSERT(args != nullptr, "Output args cannot be null.");
    return true;
}
