#ifndef KOS_STRING_H
#define KOS_STRING_H

#include <kos/types.h>
#include <kos/alloc.h>

#define KOS_STR_FORMAT "%.*s"
#define KOS_STR_EXPAND(Str) cast(int)(Str).count, (Str).data

struct kos_string {
    struct kos_allocator allocator;
    char* data;
    isize count, capacity;
};

struct kos_string_view {
    const char* data;
    isize count;
};

struct kos_string_view kos_string_view_c(const char* c);
struct kos_string_view kos_string_view_s(struct kos_string s);
struct kos_string_view kos_string_view_sv(struct kos_string_view sv);

#endif // KOS_STRING_H
