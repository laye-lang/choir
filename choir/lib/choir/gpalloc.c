#include <choir/choir.h>
#include <stdio.h>
#include <threads.h>

static void* ch_libc_alloc(void* self, int64 size);
static void* ch_libc_realloc(void* self, void* memory, int64 size);
static void ch_libc_dealloc(void* self, void* memory);
static void ch_libc_deinit(void* self);

static void* ch_gpa_alloc(void* self, int64 size);
static void* ch_gpa_realloc(void* self, void* memory, int64 size);
static void ch_gpa_dealloc(void* self, void* memory);
static void ch_gpa_deinit(void* self);

struct allocs {
    ch_allocator allocator;
    void** items;
    int64 count, capacity;
};

CHOIR_API ch_allocator ch_general_purpose_allocator() {
    struct allocs* allocs = malloc(sizeof *allocs);
    *allocs = (struct allocs){
        .allocator = (ch_allocator){
            .vtable = {
                .alloc = ch_libc_alloc,
                .realloc = ch_libc_realloc,
                .dealloc = ch_libc_dealloc,
                .deinit = ch_libc_deinit,
            },
        }
    };

    return (ch_allocator){
        .vtable = {
            .alloc = ch_gpa_alloc,
            .realloc = ch_gpa_realloc,
            .dealloc = ch_gpa_dealloc,
            .deinit = ch_gpa_deinit,
        },
        .userdata = allocs,
    };
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
    struct allocs* allocs = selfv;
    void* memory = malloc(cast(size_t) size);
    da_push(allocs, memory);
    return memory;
}

static void* ch_gpa_realloc(void* selfv, void* memory, int64 size) {
    if (memory == NULL) return ch_gpa_alloc(selfv, size);

    struct allocs* allocs = selfv;

    int64 memory_index;
    for (memory_index = 0; memory_index < allocs->count; memory_index++) {
        if ((cast(char*) memory) == (cast(char*) allocs->items[memory_index])) {
            break;
        }
    }

    assert(memory_index >= 0 && memory_index < allocs->count && "memory was not allocated with this allocator or has already been freed");

    void* new_memory = realloc(memory, cast(size_t) size);
    allocs->items[memory_index] = new_memory;
    return new_memory;
}

static void ch_gpa_dealloc(void* selfv, void* memory) {
    if (memory == NULL) return;

    struct allocs* allocs = selfv;

    int64 memory_index;
    for (memory_index = 0; memory_index < allocs->count; memory_index++) {
        if ((cast(char*) memory) == (cast(char*) allocs->items[memory_index])) {
            break;
        }
    }

    assert(memory_index >= 0 && memory_index < allocs->count && "memory was not allocated with this allocator or has already been freed");

    free(memory);

    allocs->items[memory_index] = allocs->items[allocs->count - 1];
    allocs->count--;
}

static void ch_gpa_deinit(void* selfv) {
    struct allocs* allocs = selfv;

    for (int64 i = 0; i < allocs->count; i++) {
        free(allocs->items[i]);
    }

    da_free(allocs);
    free(allocs);
}
