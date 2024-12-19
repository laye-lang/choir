#ifndef LIBLAYE_H
#define LIBLAYE_H

#include <kos/alloc.h>
#include <kos/string.h>
#include <kos/types.h>
#include <liblyir.h>
#include <stdarg.h>

// ========== Compiler Driver ==========

enum laye_driver_status {
    LAYE_DRIVER_OK = 0,
    LAYE_DRIVER_ARGS_PARSE_FAILURE = 1,
};

struct laye_args {
    const char* program_name;
};

enum laye_driver_status laye_driver_run(struct lyir_state* state, struct laye_args* args);
bool laye_args_parse(struct lyir_state* state, int argc, char** argv, struct laye_args* args);

// ========== Semantics ==========

enum laye_value_category {
    LAYE_LVALUE,
    LAYE_RVALUE,
};

enum laye_ast_kind {
    LAYE_AST_INVALID,
};

struct laye_module {
};

struct laye_ast_header {
};

struct laye_ast {
    struct laye_module* module;
    struct laye_ast_header header;
};

struct laye_ast_node {
};

#endif // LIBLAYE_H
