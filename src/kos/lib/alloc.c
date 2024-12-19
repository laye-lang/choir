#include <assert.h>
#include <kos/alloc.h>
#include <kos/assert.h>
#include <kos/macros.h>
#include <kos/types.h>
#include <stdlib.h>

#define KOS_DEFAULT_ALIGN          (16)
#define KOS_ARENA_FREE_ALLOC_COUNT (16)

struct kos_arena_block {
    struct kos_arena_block* next;
    char* memory;
    isize allocated;
};

struct kos_arena_free_allocations {
    struct kos_arena_free_allocations* next;
    void* allocations[KOS_ARENA_FREE_ALLOC_COUNT];
    isize count;
};

struct kos_arena {
    struct kos_allocator allocator;
    isize block_capacity;
    struct kos_arena_block* blocks;
    struct kos_arena_free_allocations* free_allocations;
};

static void* default_allocator_function(struct kos_allocator self, enum kos_allocator_action action, void* memory, isize size, isize align);
static void* temp_allocator_function(struct kos_allocator self, enum kos_allocator_action action, void* memory, isize size, isize align);
static void* arena_allocator_function(struct kos_allocator self, enum kos_allocator_action action, void* memory, isize size, isize align);

struct kos_allocator default_allocator = {nullptr, default_allocator_function};
struct kos_allocator temp_allocator = {nullptr, temp_allocator_function};

static char temp_alloc_data[KOS_TEMP_CAPACITY];
static isize temp_allocated;

i64 kos_align_padding(i64 value, i64 align) {
    KOS_ASSERT(align > 0, "Can only align to a positive alignment.");
    return (align - (value % align)) % align;
}

i64 kos_align_to(i64 value, i64 align) {
    return value + kos_align_padding(value, align);
}

static void* default_allocator_function(struct kos_allocator self, enum kos_allocator_action action, void* memory, isize size, isize align) {
    switch (action) {
        default: {
            KOS_UNREACHABLE("Unhandled or invalid allocator action.");
            return nullptr;
        }

        case KOS_ALLOC: {
            return malloc(cast(usize) size);
        }

        case KOS_ALLOC_ALIGNED: {
            return malloc(cast(usize) size);
        }

        case KOS_REALLOC: {
            return realloc(memory, cast(usize) size);
        }

        case KOS_DEALLOC: {
            free(memory);
            return nullptr;
        }
    }
}

static void* temp_allocator_function(struct kos_allocator self, enum kos_allocator_action action, void* memory, isize size, isize align) {
    switch (action) {
        default: {
            KOS_UNREACHABLE("Unhandled or invalid allocator action.");
            return nullptr;
        }

        case KOS_ALLOC: {
            return kos_temp_alloc(size);
        }

        case KOS_ALLOC_ALIGNED: {
            KOS_TODO("Implement aligned alloc for the temp allocator");
            return memory;
        }

        case KOS_REALLOC: {
            KOS_TODO("Implement realloc for the temp allocator");
            return nullptr;
        }

        case KOS_DEALLOC: {
            // a no-op
            return nullptr;
        }
    }
}

static void* arena_allocator_function(struct kos_allocator self, enum kos_allocator_action action, void* memory, isize size, isize align) {
    struct kos_arena* arena = self.userdata;
    switch (action) {
        default: {
            KOS_UNREACHABLE("Unhandled or invalid allocator action.");
            return nullptr;
        }

        case KOS_ALLOC: {
            return kos_arena_alloc(arena, size);
        }

        case KOS_ALLOC_ALIGNED: {
            KOS_TODO("Implement aligned alloc for the arena allocator");
            return nullptr;
        }

        case KOS_REALLOC: {
            KOS_TODO("Implement realloc for the arena allocator");
            return nullptr;
        }

        case KOS_DEALLOC: {
            // a no-op
            return nullptr;
        }
    }
}

void* kos_alloc(struct kos_allocator allocator, isize size) {
    return allocator.allocator_function(allocator, KOS_ALLOC, nullptr, size, 0);
}

void* kos_alloc_aligned(struct kos_allocator allocator, isize size, isize align) {
    return allocator.allocator_function(allocator, KOS_ALLOC_ALIGNED, nullptr, size, align);
}

void* kos_realloc(struct kos_allocator allocator, void* memory, isize size) {
    return allocator.allocator_function(allocator, KOS_REALLOC, memory, size, 0);
}

void kos_dealloc(struct kos_allocator allocator, void* memory) {
    discard allocator.allocator_function(allocator, KOS_DEALLOC, memory, 0, 0);
}

void* kos_temp_alloc(isize size) {
    size = kos_align_to(size, cast(i64) KOS_DEFAULT_ALIGN);
    temp_allocated = kos_align_to(temp_allocated, KOS_DEFAULT_ALIGN);

    void* memory = temp_alloc_data + temp_allocated;
    memset(memory, 0, cast(isize) size);

    temp_allocated += size;
    return memory;
}

isize kos_temp_mark(void) {
    return temp_allocated;
}

void kos_temp_rewind(isize mark) {
    KOS_ASSERT(mark >= 0 && mark < temp_allocated, "Invalid temp allocator mark position.");
    memset(temp_alloc_data + temp_allocated, 0, cast(usize)(temp_allocated - mark));
    temp_allocated = mark;
}

void kos_temp_reset(void) {
    temp_allocated = 0;
    memset(temp_alloc_data, 0, KOS_TEMP_CAPACITY);
}

struct kos_allocator kos_arena_allocator(struct kos_arena* arena) {
    return (struct kos_allocator){
        .userdata = arena,
        .allocator_function = arena_allocator_function,
    };
}

struct kos_arena* kos_arena_create(struct kos_allocator allocator, isize block_capacity) {
    struct kos_arena* arena = kos_alloc(allocator, sizeof *arena);
    arena->allocator = allocator;
    arena->block_capacity = block_capacity;
    arena->blocks = nullptr;
    arena->free_allocations = nullptr;
    return arena;
}

void kos_arena_destroy(struct kos_arena* arena) {
    KOS_ASSERT(arena != nullptr, "Arena cannot be null.");
    struct kos_allocator allocator = arena->allocator;

    kos_arena_reset(arena);
    kos_dealloc(allocator, arena);
}

void kos_arena_reset(struct kos_arena* arena) {
    KOS_ASSERT(arena != nullptr, "Arena cannot be null.");
    struct kos_allocator allocator = arena->allocator;

    for (struct kos_arena_block* block = arena->blocks; block != nullptr;) {
        struct kos_arena_block* current = block;
        block = block->next;

        kos_dealloc(allocator, current->memory);
        kos_dealloc(allocator, current);
    }

    for (struct kos_arena_free_allocations* free_allocs = arena->free_allocations; free_allocs != nullptr;) {
        struct kos_arena_free_allocations* current = free_allocs;
        free_allocs = free_allocs->next;

        for (isize i = 0; i < KOS_ARENA_FREE_ALLOC_COUNT; i++) {
            kos_dealloc(allocator, current->allocations[i]);
        }

        kos_dealloc(allocator, current);
    }

    arena->blocks = nullptr;
    arena->free_allocations = nullptr;
}

static void kos_arena_add_block(struct kos_arena* arena) {
    KOS_ASSERT(arena != nullptr, "Arena cannot be null.");
    struct kos_arena_block* block = kos_alloc(arena->allocator, sizeof *block);
    block->next = arena->blocks;
    block->allocated = 0;
    block->memory = kos_alloc(arena->allocator, cast(usize) arena->block_capacity);
    arena->blocks = block;
}

void* kos_arena_alloc(struct kos_arena* arena, isize size) {
    KOS_ASSERT(arena != nullptr, "Arena cannot be null.");

    size = kos_align_to(size, cast(i64) KOS_DEFAULT_ALIGN);
    if (size > arena->block_capacity) {
        KOS_TODO("Implement free allocations in arenas.");
    }

    if (arena->blocks == nullptr) {
        kos_arena_add_block(arena);
    }

    for (struct kos_arena_block* block = arena->blocks; block != nullptr; block = block->next) {
        KOS_ASSERT(block->allocated % KOS_DEFAULT_ALIGN == 0, "Allocated memory within an arena block is unaligned.");
        if (block->allocated + size < arena->block_capacity) {
            void* memory = block->memory + block->allocated;
            block->allocated += size;
            return memory;
        }
    }

    kos_arena_add_block(arena);

    struct kos_arena_block* new_block = arena->blocks;
    void* memory = new_block->memory + new_block->allocated;
    new_block->allocated += size;
    return memory;
}
