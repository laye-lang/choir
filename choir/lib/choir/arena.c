#include <choir/choir.h>

CHOIR_API void ch_arena_init(ch_arena* arena, ch_allocator allocator, int64 block_size) {
    arena->allocator = allocator;
    arena->block_size = block_size;
    arena->blocks = (ch_arena_blocks){0};
}

CHOIR_API void* ch_arena_alloc(ch_arena* arena, int64 size) {
    return NULL;
}

CHOIR_API void ch_arena_deinit(ch_arena* arena) {
    ch_allocator allocator = arena->allocator;
    da_free(&arena->blocks);
}

static void* ch_arena_alloc(void* self, int64 size);
static void* ch_arena_realloc(void* self, void* memory, int64 size);
static void ch_arena_dealloc(void* self, void* memory);
static void ch_arena_deinit(void* self);

CHOIR_API ch_allocator ch_arena_allocator(ch_arena* arena) {
    return (ch_allocator) {
        .vtable = {
            .alloc = ch_arena_alloc,
            .realloc = ch_arena_realloc,
            .dealloc = ch_arena_dealloc,
            .deinit = ch_arena_deinit,
        },
        .userdata = arena,
    };
}

static void* ch_arena_alloc(void* self, int64 size) {
    ch_arena* arena = self;
    return ch_arena_alloc(arena, size);
}

static void* ch_arena_realloc(void* self, void* memory, int64 size) {
    ch_arena* arena = self;
    assert(false && "cannot realloc with an arena (yet?)");
}

static void ch_arena_dealloc(void* self, void* memory) {
    ch_arena* arena = self;
    discard memory;
}

static void ch_arena_deinit(void* self) {
    ch_arena* arena = self;
    ch_arena_deinit(arena);
}
