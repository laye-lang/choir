#ifndef CHOIR_API_CORE_ASSERT_HH
#define CHOIR_API_CORE_ASSERT_HH

#include <libassert/assert.hpp>

// Some IDEs don’t know about __builtin_expect_with_probability, for some reason.
#if !__has_builtin(__builtin_expect_with_probability)
#    define __builtin_expect_with_probability(x, y, z) __builtin_expect(x, y)
#endif

#define CHOIR_ASSERT(cond, ...)       LIBASSERT_ASSERT(cond __VA_OPT__(, std::format(__VA_ARGS__)))
#define CHOIR_DEBUG_ASSERT(cond, ...) LIBASSERT_DEBUG_ASSERT(cond __VA_OPT__(, std::format(__VA_ARGS__)))
#define CHOIR_FATAL(...)              LIBASSERT_PANIC(__VA_OPT__(std::format(__VA_ARGS__)))
#define CHOIR_UNREACHABLE(...)        LIBASSERT_UNREACHABLE(__VA_OPT__(std::format(__VA_ARGS__)))
#define CHOIR_TODO(...)               CHOIR_UNREACHABLE("TODO: " __VA_OPT__(": " __VA_ARGS__))

#endif // !CHOIR_API_CORE_ASSERT_HH
