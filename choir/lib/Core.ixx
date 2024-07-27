module;

#include <choir/macros.hh>
#include <llvm/ADT/StringRef.h>
#include <llvm/Support/StringSaver.h>

export module choir;

export namespace choir {

using llvm::StringRef;

class File {
};

/// A null-terminated string that is saved somewhere.
///
/// This is used for strings that are guaranteed to ‘live long
/// enough’ to be passed around without having to worry about who
/// owns them. This typically means they are stored in a module
/// or static storage.
///
/// NEVER return a String to outside a single driver invocation!
class String {
    StringRef val;

public:
    constexpr String() = default;

    /// Construct from a string literal.
    template <size_t size>
    consteval String(const char (&arr)[size]) : val{arr} {
        Assert(arr[size - 1] == '\0', "Strings must be null-terminated!");
    }

    /// Construct from a string literal.
    consteval String(llvm::StringLiteral lit) : val{lit} {}

    /// Get an iterator to the beginning of the string.
    [[nodiscard]] auto begin() const { return val.begin(); }

    /// Get the data of the string.
    [[nodiscard]] constexpr auto data() const -> const char* { return val.data(); }

    /// Check if the string is empty.
    [[nodiscard]] constexpr auto empty() const -> bool { return val.empty(); }

    /// Get an iterator to the end of the string.
    [[nodiscard]] auto end() const { return val.end(); }

    /// Check if the string ends with a given suffix.
    [[nodiscard]] auto ends_with(StringRef suffix) const -> bool {
        return val.ends_with(suffix);
    }

    /// Get the size of the string.
    [[nodiscard]] constexpr auto size() const -> size_t { return val.size(); }

    /// Check if the string starts with a given prefix.
    [[nodiscard]] auto starts_with(StringRef prefix) const -> bool {
        return val.starts_with(prefix);
    }

    /// Get the string value as a std::string_view.
    [[nodiscard]] constexpr auto sv() const -> std::string_view { return val; }

    /// Get the string value.
    [[nodiscard]] constexpr auto value() const -> StringRef { return val; }

    /// Get the string value, including the null terminator.
    [[nodiscard]] constexpr auto value_with_null() const -> StringRef {
        return StringRef{val.data(), val.size() + 1};
    }

    /// Get a character at a given index.
    [[nodiscard]] auto operator[](size_t idx) const -> char { return val[idx]; }

    /// Comparison operators.
    [[nodiscard]] friend auto operator==(String a, StringRef b) { return a.val == b; }
    [[nodiscard]] friend auto operator==(String a, String b) { return a.value() == b.value(); }
    [[nodiscard]] friend auto operator==(String a, const char* b) { return a.value() == b; }
    [[nodiscard]] friend auto operator<=>(String a, String b) { return a.sv() <=> b.sv(); }
    [[nodiscard]] friend auto operator<=>(String a, std::string_view b) { return a.val <=> b; }

    /// Get the string.
    [[nodiscard]] constexpr operator StringRef() const { return val; }

    /// Create a 'String' from a 'StringRef'.
    ///
    /// This is an unsafe operation! The caller must ensure that the
    /// underlying value lives as long as the string is going to be
    /// used and that it is null-terminated. This is intended to be
    /// used e.g. by the lexer; always prefer to obtain a 'String'
    /// by other means.
    [[nodiscard]] static constexpr auto CreateUnsafe(StringRef value) {
        String s;
        s.val = value;
        return s;
    }

    /// Save it in a string saver; this is how you’re supposed to create these.
    [[nodiscard]] static auto Save(llvm::StringSaver& ss, StringRef s) {
        return CreateUnsafe(ss.save(s));
    }

    [[nodiscard]] static auto Save(llvm::UniqueStringSaver& ss, StringRef s) {
        return CreateUnsafe(ss.save(s));
    }
};

auto operator+=(std::string& s, choir::String str) -> std::string& {
    s += str.value();
    return s;
}

}; // namespace choir
