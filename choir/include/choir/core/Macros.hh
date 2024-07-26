#ifndef CHOIR_API_CORE_MACROS_HH
#define CHOIR_API_CORE_MACROS_HH

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
