#include <choir/choir.h>

CHOIR_API void* ch_alloc(ch_allocator allocator, int64 size) {
    return allocator.vtable.alloc(allocator.userdata, size);
}

CHOIR_API void* ch_realloc(ch_allocator allocator, void* memory, int64 size) {
    return allocator.vtable.realloc(allocator.userdata, memory, size);
}

CHOIR_API void ch_dealloc(ch_allocator allocator, void* memory) {
    allocator.vtable.dealloc(allocator.userdata, memory);
}

CHOIR_API void ch_allocator_deinit(ch_allocator allocator) {
    allocator.vtable.deinit(allocator.userdata);
}
