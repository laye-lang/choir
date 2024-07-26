#ifndef CHOIR_API_CORE_MACROS_HH
#define CHOIR_API_CORE_MACROS_HH

#include <format>
#include <libassert/assert.hpp>

#define CHOIR_ASSERT(cond, ...)       LIBASSERT_ASSERT(cond __VA_OPT__(, std::format(__VA_ARGS__)))
#define CHOIR_DEBUG_ASSERT(cond, ...) LIBASSERT_DEBUG_ASSERT(cond __VA_OPT__(, std::format(__VA_ARGS__)))
#define CHOIR_FATAL(...)              LIBASSERT_PANIC(__VA_OPT__(std::format(__VA_ARGS__)))
#define CHOIR_UNREACHABLE(...)        LIBASSERT_UNREACHABLE(__VA_OPT__(std::format(__VA_ARGS__)))
#define CHOIR_TODO(...)               CHOIR_UNREACHABLE("TODO: " __VA_OPT__(": " __VA_ARGS__))

#define CHOIR_IMMOVABLE(cls)             \
    cls(const cls&) = delete;            \
    cls& operator=(const cls&) = delete; \
    cls(cls&&) = delete;                 \
    cls& operator=(cls&&) = delete

#define CHOIR_DECLARE_HIDDEN_IMPL(X) \
public:                              \
    CHOIR_IMMOVABLE(X);              \
    ~X();                            \
                                     \
private:                             \
    struct Impl;                     \
    Impl* const impl;

#define CHOIR_DEFINE_HIDDEN_IMPL(X) \
    X::~X() { delete impl; }

#endif // !CHOIR_API_CORE_MACROS_HH
