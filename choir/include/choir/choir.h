#ifndef CHOIR_H_
#define CHOIR_H_

#if defined(__cplusplus)
extern "C" {
#endif // defined(__cplusplus)

#include <assert.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>

/*
@@ CHOIR_USE_C11 controlls the use of non-C11 features.
** Define it if you want Choir to avoid the use of C23 features,
** or Windows-specific features on Windows.
*/
/* #define CHOIR_USE_C11 */

#if !defined(CHOIR_USE_C11) && defined(_WIN32)
#    define CHOIR_USE_WINDOWS
#endif

#if defined(CHOIR_USE_WINDOWS)
#    define CHOIR_USE_DLL
#    if defined(_MSC_VER)
#        define CHOIR_USE_C11
#    endif
#endif

#if defined(__linux__)
#    define CHOIR_USE_LINUX
#endif

#if defined(CHOIR_USE_LINUX)
#    define CHOIR_USE_POSIX
#    define CHOIR_USE_DLOPEN
#endif

#if defined(CHOIR_BUILD_AS_DLL)
#    if defined(CHOIR_LIB)
#        define CHOIR_API __declspec(dllexport)
#    else
#        define CHOIR_API __declspec(dllimport)
#    endif
#else /* CHOIR_BUILD_AS_DLL */
#    define CHOIR_API extern
#endif

#define cast(T) (T)
#define discard (void)

#if defined(__clang__)
#    define CHOIR_TRAP() \
        do { [[clang::nomerge]] __builtin_trap(); } while (0)
#else
#    define CHOIR_TRAP() \
        do { __builtin_trap(); } while (0)
#endif

#define return_defer(value) \
    do {                    \
        result = (value);   \
        goto defer;         \
    } while (0)

// Initial capacity of a dynamic array
#ifndef DA_INIT_CAP
#    define DA_INIT_CAP 256
#endif

// Push an item to a dynamic array
#define da_push(da, item)                                                                                  \
    do {                                                                                                   \
        if ((da)->count >= (da)->capacity) {                                                               \
            (da)->capacity = (da)->capacity == 0 ? DA_INIT_CAP : (da)->capacity * 2;                       \
            (da)->items = ch_realloc((da)->allocator, (da)->items, (da)->capacity * sizeof(*(da)->items)); \
            assert((da)->items != NULL && "Buy more RAM lol");                                             \
        }                                                                                                  \
        (da)->items[(da)->count++] = (item);                                                               \
    } while (0)

#define da_free(da) ch_dealloc((da)->allocator, (da)->items)

// Push several items to a dynamic array
#define da_push_many(da, new_items, new_items_count)                                                       \
    do {                                                                                                   \
        if ((da)->count + (new_items_count) > (da)->capacity) {                                            \
            if ((da)->capacity == 0) {                                                                     \
                (da)->capacity = DA_INIT_CAP;                                                              \
            }                                                                                              \
            while ((da)->count + (new_items_count) > (da)->capacity) {                                     \
                (da)->capacity *= 2;                                                                       \
            }                                                                                              \
            (da)->items = ch_realloc((da)->allocator, (da)->items, (da)->capacity * sizeof(*(da)->items)); \
            assert((da)->items != NULL && "Buy more RAM lol");                                             \
        }                                                                                                  \
        memcpy((da)->items + (da)->count, (new_items), (new_items_count) * sizeof(*(da)->items));          \
        (da)->count += (new_items_count);                                                                  \
    } while (0)

#define ch_assert(Context, Condition, Location, Message)                                             \
    do {                                                                                             \
        if (!(Condition)) ch_diag((Context), CH_DIAG_ICE, (Location), "Assertion failed: " Message); \
    } while (0)

#define ch_assertf(Context, Condition, Location, Message, ...)                                                    \
    do {                                                                                                          \
        if (!(Condition)) ch_diag((Context), CH_DIAG_ICE, (Location), "Assertion failed: " Message, __VA_ARGS__); \
    } while (0)

#define CH_NOLOC ((ch_location){0})

typedef int8_t int8;
typedef int16_t int16;
typedef int32_t int32;
typedef int64_t int64;

typedef uint8_t uint8;
typedef uint16_t uint16;
typedef uint32_t uint32;
typedef uint64_t uint64;

typedef float float32;
typedef double float64;

typedef size_t usize;
typedef ptrdiff_t isize;

typedef int64 ch_size;
typedef int64 ch_align;

typedef void* (*ch_allocator_alloc_fn)(void* self, int64 size);
typedef void* (*ch_allocator_realloc_fn)(void* self, void* memory, int64 size);
typedef void (*ch_allocator_dealloc_fn)(void* self, void* memory);
typedef void (*ch_allocator_deinit_fn)(void* self);

typedef struct ch_allocator_vtable {
    ch_allocator_alloc_fn alloc;
    ch_allocator_realloc_fn realloc;
    ch_allocator_dealloc_fn dealloc;
    ch_allocator_deinit_fn deinit;
} ch_allocator_vtable;

typedef struct ch_allocator {
    ch_allocator_vtable vtable;
    void* userdata;
} ch_allocator;

CHOIR_API void* ch_alloc(ch_allocator allocator, int64 size);
CHOIR_API void* ch_realloc(ch_allocator allocator, void* memory, int64 size);
CHOIR_API void ch_dealloc(ch_allocator allocator, void* memory);
CHOIR_API void ch_allocator_deinit(ch_allocator allocator);

CHOIR_API ch_allocator ch_general_purpose_allocator(void);

typedef struct ch_arena_block {
    void* memory;
    int64 consumed;
} ch_arena_block;

typedef struct ch_arena_blocks {
    ch_allocator allocator;
    ch_arena_block* items;
    int64 count, capacity;
} ch_arena_blocks;

typedef struct ch_arena {
    ch_allocator allocator;
    ch_arena_blocks blocks;
    int64 block_size;
} ch_arena;

CHOIR_API void ch_arena_init(ch_arena* arena, ch_allocator allocator, int64 block_size);
CHOIR_API void* ch_arena_alloc(ch_arena* arena, int64 size);
CHOIR_API void ch_arena_deinit(ch_arena* arena);
CHOIR_API ch_allocator ch_arena_allocator(ch_arena* arena);

typedef struct ch_string {
    ch_allocator allocator;
    char* items;
    int64 count, capacity;
} ch_string;

typedef struct ch_target {
    ch_size size_of_pointer;
    ch_align align_of_pointer;
} ch_target;

typedef struct ch_source {
    const char* name;
    const char* text;
    int64 length;
} ch_source;

typedef struct ch_sources {
    ch_allocator allocator;
    ch_source* items;
    int64_t count, capacity;
} ch_sources;

typedef struct ch_location {
    ch_source* source;
    int64 offset;
    int64 length;
} ch_location;

typedef struct ch_string_store {
    ch_allocator allocator;
    const char** items;
    int64_t count, capacity;
} ch_string_store;

typedef enum ch_diagnostic_kind {
    CH_DIAG_NOTE,
    CH_DIAG_WARN,
    CH_DIAG_ERROR,
    CH_DIAG_ICE,
} ch_diagnostic_kind;

typedef struct ch_diagnostic {
    ch_diagnostic_kind kind;
    ch_location location;
    const char* message;
} ch_diagnostic;

typedef struct ch_diagnostics {
    ch_allocator allocator;
    ch_diagnostic* items;
    int64 count, capacity;
} ch_diagnostics;

typedef struct ch_context {
    ch_allocator allocator;
    ch_target* target;
    ch_string_store string_store;

    bool has_issued_diagnostics;
    ch_diagnostics queued_diagnostics;
} ch_context;

typedef enum ch_exit_code {
    CH_EXIT_OK = 0,
} ch_exit_code;

typedef struct ch_args {
    int dummy;
} ch_args;

CHOIR_API void ch_context_init(ch_context* context, ch_allocator allocator);
CHOIR_API void ch_context_deinit(ch_context* context);

CHOIR_API void ch_diag_flush(ch_context* context);
CHOIR_API void ch_diag(ch_context* context, ch_diagnostic_kind kind, ch_location location, const char* format, ...);

CHOIR_API int choir_main(int argc, char** argv);
CHOIR_API ch_exit_code choir_driver(ch_args args);

#if defined(__cplusplus)
}
#endif // defined(__cplusplus)

#endif // CHOIR_H_
