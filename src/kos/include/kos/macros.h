#ifndef KOS_MACROS_H
#define KOS_MACROS_H

#define cast(T) (T)
#define discard (void)
#define nullptr NULL

#define kos_return_defer(RetVal) do { result = (RetVal); goto defer; } while (0)

#ifndef NDEBUG
#    define KOS_DEBUG 1
#else
#    define KOS_DEBUG 0
#endif

#endif // KOS_MACROS_H
