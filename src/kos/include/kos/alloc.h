#ifndef KOS_ALLOC_H
#define KOS_ALLOC_H

#ifndef KOS_TEMP_CAPACITY
#    define KOS_TEMP_CAPACITY (64 * 1024 * 1024) /* should be enough for everybody */
#endif

#include <kos/types.h>
#include <stdlib.h>
#include <string.h>

enum kos_allocator_action {
    KOS_ALLOC,
    KOS_ALLOC_ALIGNED,
    KOS_REALLOC,
    KOS_DEALLOC,
};

struct kos_allocator {
    void* userdata;
    void* (*allocator_function)(struct kos_allocator self, enum kos_allocator_action action, void* memory, isize size, isize align);
};

struct kos_arena;

extern struct kos_allocator default_allocator;
extern struct kos_allocator temp_allocator;

i64 kos_align_padding(i64 value, i64 align);
i64 kos_align_to(i64 value, i64 align);

void* kos_alloc(struct kos_allocator allocator, isize size);
void* kos_alloc_aligned(struct kos_allocator allocator, isize size, isize align);
void* kos_realloc(struct kos_allocator allocator, void* memory, isize size);
void kos_dealloc(struct kos_allocator allocator, void* memory);

void* kos_temp_alloc(isize size);
isize kos_temp_mark(void);
void kos_temp_rewind(isize mark);
void kos_temp_reset(void);

struct kos_allocator kos_arena_allocator(struct kos_arena* arena);
struct kos_arena* kos_arena_create(struct kos_allocator allocator, isize block_capacity);
void kos_arena_destroy(struct kos_arena* arena);
void kos_arena_reset(struct kos_arena* arena);
void* kos_arena_alloc(struct kos_arena* arena, isize size);

#endif // KOS_ALLOC_H
