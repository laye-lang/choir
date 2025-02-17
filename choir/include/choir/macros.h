#ifndef CHOIR_MACROS_H_
#define CHOIR_MACROS_H_

#define cast(T) (T)
#define discard (void)

#define return_defer(value) \
    do {                    \
        result = (value);   \
        goto defer;         \
    } while (0)

// Initial capacity of a dynamic array
#ifndef DA_INIT_CAP
#    define DA_INIT_CAP 256
#endif

// Push an item to a dynamic array
#define da_push(da, item)                                                                                  \
    do {                                                                                                   \
        if ((da)->count >= (da)->capacity) {                                                               \
            (da)->capacity = (da)->capacity == 0 ? DA_INIT_CAP : (da)->capacity * 2;                       \
            (da)->items = ch_realloc((da)->allocator, (da)->items, (da)->capacity * sizeof(*(da)->items)); \
            assert((da)->items != NULL && "Buy more RAM lol");                                             \
        }                                                                                                  \
        (da)->items[(da)->count++] = (item);                                                               \
    } while (0)

#define da_free(da) ch_dealloc((da)->allocator, (da)->items)

// Push several items to a dynamic array
#define da_push_many(da, new_items, new_items_count)                                                       \
    do {                                                                                                   \
        if ((da)->count + (new_items_count) > (da)->capacity) {                                            \
            if ((da)->capacity == 0) {                                                                     \
                (da)->capacity = DA_INIT_CAP;                                                              \
            }                                                                                              \
            while ((da)->count + (new_items_count) > (da)->capacity) {                                     \
                (da)->capacity *= 2;                                                                       \
            }                                                                                              \
            (da)->items = ch_realloc((da)->allocator, (da)->items, (da)->capacity * sizeof(*(da)->items)); \
            assert((da)->items != NULL && "Buy more RAM lol");                                             \
        }                                                                                                  \
        memcpy((da)->items + (da)->count, (new_items), (new_items_count) * sizeof(*(da)->items));          \
        (da)->count += (new_items_count);                                                                  \
    } while (0)

#endif // CHOIR_MACROS_H_
