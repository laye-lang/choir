#include <choir/choir.h>

CHOIR_API void ch_arena_init(ch_arena* arena, ch_allocator allocator, int64 block_size) {
    arena->allocator = allocator;
    arena->block_size = block_size;
    arena->blocks = (ch_arena_blocks){
        .allocator = allocator,
    };
}

CHOIR_API void* ch_arena_alloc(ch_arena* arena, int64 size) {
    assert(size <= arena->block_size && "can't allocate something that big in this arena dummy");

    ch_arena_block* alloc_block = NULL;
    for (int64 i = 0; i < arena->blocks.count && alloc_block == NULL; i++) {
        if (arena->block_size - arena->blocks.items[i].consumed <= size) {
            alloc_block = &arena->blocks.items[i];
        }
    }
    
    if (alloc_block == NULL) {
        ch_arena_block block = {
            .memory = ch_alloc(arena->allocator, arena->block_size),
        };
        da_push(&arena->blocks, block);
        alloc_block = &arena->blocks.items[arena->blocks.count - 1];
    }

    assert(alloc_block != NULL && "where did it go");
    
    void* memory = (cast(char*) alloc_block->memory) + alloc_block->consumed;
    alloc_block->consumed += size;

    return memory;
}

CHOIR_API void ch_arena_deinit(ch_arena* arena) {
    ch_allocator allocator = arena->allocator;

    for (int64 i = 0; i < arena->blocks.count; i++) {
        ch_dealloc(allocator, arena->blocks.items[i].memory);
    }

    da_free(&arena->blocks);
}

static void* ch_arena_allocator_alloc(void* self, int64 size);
static void* ch_arena_allocator_realloc(void* self, void* memory, int64 size);
static void ch_arena_allocator_dealloc(void* self, void* memory);
static void ch_arena_allocator_deinit(void* self);

CHOIR_API ch_allocator ch_arena_allocator(ch_arena* arena) {
    return (ch_allocator) {
        .vtable = {
            .alloc = ch_arena_allocator_alloc,
            .realloc = ch_arena_allocator_realloc,
            .dealloc = ch_arena_allocator_dealloc,
            .deinit = ch_arena_allocator_deinit,
        },
        .userdata = arena,
    };
}

static void* ch_arena_allocator_alloc(void* self, int64 size) {
    ch_arena* arena = self;
    return ch_arena_alloc(arena, size);
}

static void* ch_arena_allocator_realloc(void* self, void* memory, int64 size) {
    ch_arena* arena = self;
    assert(false && "cannot realloc with an arena (yet?)");
}

static void ch_arena_allocator_dealloc(void* self, void* memory) {
    ch_arena* arena = self;
    discard memory;
}

static void ch_arena_allocator_deinit(void* self) {
    ch_arena* arena = self;
    ch_arena_deinit(arena);
}
