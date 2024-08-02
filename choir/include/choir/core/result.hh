#ifndef CHOIR_CORE_RESULT_HH
#define CHOIR_CORE_RESULT_HH

#include <expected>
#include <format>
#include <string>
#include <type_traits>

namespace choir::detail {

template <typename Ty>
requires (not std::is_reference_v<Ty>)
class ReferenceWrapper {
    Ty* ptr;
public:
    ReferenceWrapper(Ty& ref) : ptr(&ref) {}
    operator Ty&() const { return *ptr; }
    auto operator&() const -> Ty* { return ptr; }
    auto operator->() const -> Ty* { return ptr; }

    auto operator=(std::convertible_to<Ty> auto&& val) -> ReferenceWrapper {
        *ptr = std::forward<decltype(val)>(val);
        return *this;
    }
};

template <typename Ty>
concept Reference = std::is_reference_v<Ty>;

template <typename Ty>
concept NotReference = not Reference<Ty>;

template <typename Ty>
struct ResultImpl;

template <Reference Ty>
struct ResultImpl<Ty> {
    using type = std::expected<ReferenceWrapper<std::remove_reference_t<Ty>>, std::string>;
};

template <NotReference Ty>
struct ResultImpl<Ty> {
    using type = std::expected<Ty, std::string>;
};

template <typename Ty>
struct ResultImpl<std::reference_wrapper<Ty>> {
    using type = typename ResultImpl<Ty&>::type;
    static_assert(false, "Use Result<T&> instead of Result<reference_wrapper<T>>");
};

template <typename Ty>
struct ResultImpl<ReferenceWrapper<Ty>> {
    using type = typename ResultImpl<Ty&>::type;
    static_assert(false, "Use Result<T&> instead of Result<ReferenceWrapper<T>>");
};

} // namespace choir::detail

namespace choir {

/// A result type that stores either a value or an error message.
///
/// You can create a new result using the 'Error()' function and
/// unwrap results using 'Try()' (or check them manually if you
/// want to handle the error).
///
/// Result<T&> is valid and is handled correctly.
template <typename T = void>
class [[nodiscard]] Result : public detail::ResultImpl<T>::type {
    using Base = detail::ResultImpl<T>::type;

public:
    using detail::ResultImpl<T>::type::type;

    /// Disallow unchecked operations.
    auto operator*() = delete;
    auto operator->() = delete;

    /// Get the value or throw the error.
    template <typename Self>
    decltype(auto) value(this Self&& self) {
        if (self.has_value()) return std::forward<Self>(self).Base::value();
        CHOIR_FATAL("{}", std::forward<Self>(self).error());
    }

    /// DO NOT USE. This is used to implement Try().
    auto _unsafe_unwrap() noexcept(std::is_nothrow_move_constructible_v<T>)
        -> std::add_rvalue_reference_t<T>
    requires (not std::is_void_v<T> and not std::is_reference_v<T>)
    {
        return std::move(this->value());
    }

    auto _unsafe_unwrap() noexcept
        -> detail::ReferenceWrapper<std::remove_reference_t<T>>
    requires std::is_reference_v<T>
    {
        return this->value();
    }

    void _unsafe_unwrap() noexcept
    requires std::is_void_v<T>
    {}
};

/// Create an error message.
template <typename... Args>
[[nodiscard]] auto ResultError(
    std::format_string<Args...> fmt,
    Args&&... args
) -> std::unexpected<std::string> {
    return std::unexpected(std::format(fmt, std::forward<Args>(args)...));
}

};

#endif // CHOIR_CORE_RESULT_HH
