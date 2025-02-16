#include <choir/choir.h>
#include <threads.h>
#include <stdio.h>

struct libc_alloc {
    ch_allocator_vtable vtable;
};

static void* ch_libc_alloc(void* self, int64 size);
static void* ch_libc_realloc(void* self, void* memory, int64 size);
static void ch_libc_dealloc(void* self, void* memory);
static void ch_libc_deinit(void* self);

static struct libc_alloc libc_alloc = {
    .vtable = {
        .alloc = &ch_libc_alloc,
        .realloc = &ch_libc_realloc,
        .dealloc = &ch_libc_dealloc,
        .deinit = &ch_libc_deinit,
    },
};

struct allocs {
    ch_allocator* allocator;
    void** items;
    int64 count, capacity;
};

struct gpa {
    ch_allocator_vtable vtable;
    struct allocs allocs;
};

static void* ch_gpa_alloc(void* self, int64 size);
static void* ch_gpa_realloc(void* self, void* memory, int64 size);
static void ch_gpa_dealloc(void* self, void* memory);
static void ch_gpa_deinit(void* self);

static struct gpa gpa = {
    .vtable = {
        .alloc = &ch_gpa_alloc,
        .realloc = &ch_gpa_realloc,
        .dealloc = &ch_gpa_dealloc,
        .deinit = &ch_gpa_deinit,
    },
    .allocs = {
        .allocator = cast(ch_allocator*) &libc_alloc,
    },
};

CHOIR_API ch_allocator* ch_general_purpose_allocator() {
    return cast(ch_allocator*) &gpa;
}

static void* ch_libc_alloc(void* self, int64 size) {
    return malloc(cast(size_t) size);
}

static void* ch_libc_realloc(void* self, void* memory, int64 size) {
    return realloc(memory, cast(size_t) size);
}

static void ch_libc_dealloc(void* self, void* memory) {
    free(memory);
}

static void ch_libc_deinit(void* self) {
}

static void* ch_gpa_alloc(void* selfv, int64 size) {
    struct gpa* self = selfv;
    void* memory = malloc(cast(size_t) size);
    da_push(&self->allocs, memory);
    return memory;
}

static void* ch_gpa_realloc(void* selfv, void* memory, int64 size) {
    struct gpa* self = selfv;

    int64 memory_index;
    for (memory_index = 0; memory_index < self->allocs.count; memory_index++) {
        if ((cast(char*) memory) == (cast(char*) self->allocs.items[memory_index])) {
            break;
        }
    }

    assert(memory_index >= 0 && memory_index < self->allocs.count && "memory was not allocated with this allocator or has already been freed");
    
    void* new_memory = realloc(memory, cast(size_t) size);
    self->allocs.items[memory_index] = new_memory;
    return new_memory;
}

static void ch_gpa_dealloc(void* selfv, void* memory) {
    struct gpa* self = selfv;

    int64 memory_index;
    for (memory_index = 0; memory_index < self->allocs.count; memory_index++) {
        if ((cast(char*) memory) == (cast(char*) self->allocs.items[memory_index])) {
            break;
        }
    }

    assert(memory_index >= 0 && memory_index < self->allocs.count && "memory was not allocated with this allocator or has already been freed");

    free(memory);

    self->allocs.items[memory_index] = self->allocs.items[self->allocs.count - 1];
    self->allocs.count--;
}

static void ch_gpa_deinit(void* selfv) {
    struct gpa* self = selfv;
    for (int64 i = 0; i < self->allocs.count; i++) {
        free(self->allocs.items[i]);
    }

    da_free(&self->allocs);
}
