#ifndef CHOIR_H_
#define CHOIR_H_

#if defined(__cplusplus)
extern "C" {
#endif // defined(__cplusplus)

#include <choir/config.h>
#include <choir/macros.h>

#include <assert.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>

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

CHOIR_API ch_allocator ch_general_purpose_allocator();

typedef struct ch_arena_block {
    void* memory;
    int64_t consumed;
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

typedef struct ch_target {
    ch_size size_of_pointer;
    ch_align align_of_pointer;
} ch_target;

typedef struct ch_source {
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

typedef struct ch_context {
    ch_target* target;
    ch_string_store string_store;
} ch_context;

#if defined(__cplusplus)
}
#endif // defined(__cplusplus)

#endif // CHOIR_H_
