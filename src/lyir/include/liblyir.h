#ifndef LIBLYIR_H
#define LIBLYIR_H

#include <kos/alloc.h>
#include <kos/string.h>
#include <kos/types.h>
#include <kos/macros.h>
#include <stdarg.h>

// ========== Lyir State ==========

#if KOS_DEBUG
#define lyir_assert(State, Span, Condition, ...) do { if (Condition) { lyir_diag((State), LYIR_ICE, (Span), "Assertion Failure: " __VA_ARGS__); } } while (0)
#define lyir_todo(State, Span, ...) do { lyir_diag((State), LYIR_ICE, (Span), "TODO: " __VA_ARGS__); } while (0)
#define lyir_unreachable(State, Span) do { lyir_diag((State), LYIR_ICE, (Span), "Reached unreachable code."); } while (0)
#else
#define lyir_assert(State, Span, Condition, ...) do { discard (Condition); } while (0)
#define lyir_todo(State, Span, ...) do { } while (0)
#define lyir_unreachable(State, Span) do { } while (0)
#endif

#define lyir_ns_assert(State, Condition, ...) lyir_assert((State), LYIR_NO_SPAN, (Condition), __VA_ARGS__)
#define lyir_ns_todo(State, ...)  lyir_todo((State), LYIR_NO_SPAN, __VA_ARGS__)
#define lyir_ns_unreachable(State)  lyir_unreachable((State), LYIR_NO_SPAN)

struct lyir_state;
typedef isize lyir_source_id;

struct lyir_state* lyir_create(struct kos_allocator allocator);
void lyir_destroy(struct lyir_state* state);

lyir_source_id lyir_source_add(struct lyir_state* state, struct kos_string_view source_name, struct kos_string_view source_text);
struct kos_string_view lyir_source_name_get(struct lyir_state* state, lyir_source_id source_id);
struct kos_string_view lyir_source_text_get(struct lyir_state* state, lyir_source_id source_id);

bool lyir_parse(struct lyir_state* state, lyir_source_id source_id);

// ========== Compiler Driver ==========

enum lyir_driver_status {
    LYIR_DRIVER_OK = 0,
    LYIR_DRIVER_ARGS_PARSE_FAILURE = 1,
};

struct lyir_args {
    const char* program_name;
};

enum lyir_driver_status lyir_driver_run(struct lyir_state* state, struct lyir_args* args);
bool lyir_args_parse(struct lyir_state* state, int argc, char** argv, struct lyir_args* args);

// ========== Source Information ==========

#define LYIR_NO_SPAN ((struct lyir_span){0})

struct lyir_span {
    lyir_source_id source_id;
    isize begin, end;
};

struct lyir_location_info_short {
    i64 line, column;
};

struct lyir_location_info {
    i64 line, column;
    isize line_begin, line_end;
    struct kos_string_view line_text;
};

struct lyir_span lyir_span(lyir_source_id source_id, isize begin, isize end);
struct lyir_location_info_short lyir_seek_short(struct lyir_state* state, struct lyir_span span);
struct lyir_location_info lyir_seek(struct lyir_state* state, struct lyir_span span);

// ========== Diagnostics ==========

enum lyir_diagnostic_kind {
    LYIR_NOTE,
    LYIR_WARNING,
    LYIR_ERROR,
    LYIR_ICE,
};

enum lyir_diagnostic_span_format {
    LYIR_SPAN_LINE_COLUMN,
    LYIR_SPAN_OFFSET_LENGTH,
};

struct lyir_diagnostic;

void lyir_diag_flush(struct lyir_state* state);

void lyir_diag(struct lyir_state* state, enum lyir_diagnostic_kind kind, struct lyir_span span, const char* format, ...);
void lyir_diag_v(struct lyir_state* state, enum lyir_diagnostic_kind kind, struct lyir_span span, const char* format, va_list v);

// ========== IR ==========

enum lyir_type {
    LYIR_TYPE_INVALID,
};

enum lyir_icmp {
    LYIR_ICMP_EQ,
};

enum lyir_fcmp {
    LYIR_FCMP_EQ,
};

// clang-format off
enum lyir_jump {
#define LYIR_JUMPS(X) \
    X(RETW) X(RETL) X(RETS) X(RETD) \
    X(RETSB) X(RETUB) X(RETSH) X(RETUH) \
    X(HALT)

    LYIR_JXXX = 0,
#define X(Name) LYIR_J ## Name,
    LYIR_JUMPS(X)
#undef X
    LYIR_JUMP_COUNT,
};
// clang-format on

enum lyir_opcode {
    LYIR_OXXX = 0,
    LYIR_OP_COUNT,
};

struct lyir_op {
    enum lyir_opcode code;
};

// ========== Builder ==========

struct lyir_builder;

struct lyir_builder* lyir_builder_create(struct lyir_state* state);

#endif // LIBLYIR_H
