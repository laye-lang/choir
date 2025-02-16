#include <choir/choir.h>

CHOIR_API void* ch_alloc(ch_allocator* allocator, int64 size) {
    ch_allocator_vtable* vtable = (ch_allocator_vtable*)allocator;
    return vtable->alloc(allocator, size);
}

CHOIR_API void* ch_realloc(ch_allocator* allocator, void* memory, int64 size) {
    ch_allocator_vtable* vtable = (ch_allocator_vtable*)allocator;
    return vtable->realloc(allocator, memory, size);
}

CHOIR_API void ch_dealloc(ch_allocator* allocator, void* memory) {
    ch_allocator_vtable* vtable = (ch_allocator_vtable*)allocator;
    vtable->dealloc(allocator, memory);
}

CHOIR_API void ch_allocator_deinit(ch_allocator* allocator) {
    ch_allocator_vtable* vtable = (ch_allocator_vtable*)allocator;
    vtable->deinit(allocator);
}
