// Includes code from or otherwise inspired by https://github.com/Sirraide/source

module;

#include <algorithm>
#include <bit>
#include <choir/macros.hh>
#include <expected>
#include <filesystem>
#include <format>
#include <llvm/ADT/IntrusiveRefCntPtr.h>
#include <llvm/ADT/StringRef.h>
#include <llvm/Support/Alignment.h>
#include <llvm/Support/Error.h>
#include <llvm/Support/MemoryBuffer.h>
#include <llvm/Support/StringSaver.h>
#include <mutex>
#include <print>
#include <string>
#include <string_view>
#include <source_location>

export module choir;

export namespace choir {
class Context;
class Diagnostic;
class DiagnosticsEngine;
class StreamingDiagnosticsEngine;
template <typename Derived, typename ErrRetTy>
class DiagsProducer;
struct Location;
struct LocInfo;
struct LocInfoShort;
class File;
class Size;
class String;
enum struct Color;
struct Colors;
}; // namespace choir

namespace choir {

using llvm::StringRef;

/// A decoded source location.
struct LocInfo {
    size_t line;
    size_t col;
    const char* line_start;
    const char* line_end;
};

/// A short decoded source location.
struct LocInfoShort {
    size_t line;
    size_t col;
};

/// A source range in a file.
struct Location {
    uint32_t pos{};
    uint16_t len{};
    uint16_t file_id{};

    constexpr Location() = default;
    constexpr Location(uint32_t pos, uint16_t len, uint16_t file_id)
        : pos(pos), len(len), file_id(file_id) {}

    /// Create a new location that spans two locations.
    constexpr Location(Location a, Location b) {
        if (a.file_id != b.file_id) return;
        if (not a.is_valid() or not b.is_valid()) return;
        pos = std::min<uint32_t>(a.pos, b.pos);
        len = uint16_t(std::max<uint32_t>(a.pos + a.len, b.pos + b.len) - pos);
    }

    /// Shift a source location to the left.
    [[nodiscard]] constexpr auto operator<<(ptrdiff_t amount) const -> Location {
        Location l = *this;
        if (not is_valid()) return l;
        l.pos = std::min(pos, uint32_t(pos - uint32_t(amount)));
        return l;
    }

    /// Shift a source location to the right.
    [[nodiscard]] constexpr auto operator>>(ptrdiff_t amount) const -> Location {
        Location l = *this;
        l.pos = std::max(pos, uint32_t(pos + uint32_t(amount)));
        return l;
    }

    /// Extend a source location to the left.
    [[nodiscard]] constexpr auto operator<<=(ptrdiff_t amount) const -> Location {
        Location l = *this << amount;
        l.len = std::max(l.len, uint16_t(l.len + amount));
        return l;
    }

    /// Extend a source location to the right.
    [[nodiscard]] constexpr auto operator>>=(ptrdiff_t amount) const -> Location {
        Location l = *this;
        l.len = std::max(l.len, uint16_t(l.len + amount));
        return l;
    }

    /// Contract a source location to the left.
    [[nodiscard]] constexpr auto contract_left(ptrdiff_t amount) const -> Location {
        if (amount > len) return {};
        Location l = *this;
        l.len = uint16_t(l.len - amount);
        return l;
    }

    /// Contract a source location to the right.
    [[nodiscard]] constexpr auto contract_right(ptrdiff_t amount) const -> Location {
        if (amount > len) return {};
        Location l = *this;
        l.pos = uint32_t(l.pos + uint32_t(amount));
        l.len = uint16_t(l.len - amount);
        return l;
    }

    /// Encode a location as a 64-bit number.
    [[nodiscard]] constexpr uint64_t encode() const { return std::bit_cast<uint64_t>(*this); }

    [[nodiscard]] constexpr bool is_valid() const { return len != 0; }

    /// Seek to a source location.
    [[nodiscard]] auto seek(const Context& ctx) const -> std::optional<LocInfo>;

    /// Seek to a source location, but only return the line and column.
    [[nodiscard]] auto seek_line_column(const Context& ctx) const -> std::optional<LocInfoShort>;

    /// Get the text pointed to by this source location.
    ///
    /// This returns a StringRef instead of a String because the returned
    /// range is almost certainly not null-terminated.
    [[nodiscard]] auto text(const Context& ctx) const -> StringRef;

    /// Decode a source location from a 64-bit number.
    static constexpr auto Decode(uint64_t loc) -> Location {
        return std::bit_cast<Location>(loc);
    }

private:
    [[nodiscard]] bool seekable(const Context& ctx) const;
};

/// ANSI Terminal colours.
enum struct Color {
    Bold,
    Reset,
    Red,
    Green,
    Yellow,
    Blue,
    Magenta,
    Cyan,
    White,
    None,
};

/// RAII helper to toggle colours when printing.
///
/// Example:
/// \code{.cpp}
///     using enum Colour;
///     Colours C{true};
///     out += C(Red);
///     out += std::format("{}foo{}", C(Green), C(Reset));
/// \endcode
struct Colors {
    bool use_colors;
    constexpr Colors(bool use_colors)
        : use_colors{use_colors} {}

    constexpr auto operator()(Color c) const -> std::string_view {
        if (not use_colors) return "";
        switch (c) {
            case Color::Reset: return "\033[m";
            case Color::None: return "";
            case Color::Red: return "\033[31m";
            case Color::Green: return "\033[32m";
            case Color::Yellow: return "\033[33m";
            case Color::Blue: return "\033[34m";
            case Color::Magenta: return "\033[35m";
            case Color::Cyan: return "\033[36m";
            case Color::White: return "\033[37m";
            case Color::Bold: return "\033[1m";
        }
        return "";
    }
};

/// All members of 'Context' are thread-safe.
class Context {
    CHOIR_DECLARE_HIDDEN_IMPL(Context);

public:
    /// Create a new context with default options.
    explicit Context();

    /// Get diagnostics engine.
    [[nodiscard]] auto diags() const -> DiagnosticsEngine&;

    /// Enable or disable coloured output.
    void enable_colours(bool enable);

    /// Get a file by index. Returns nullptr if the index is out of bounds.
    [[nodiscard]] auto file(size_t idx) const -> const File*;

    /// Get a file from disk.
    ///
    /// This will load the file the first time it is requested.
    [[nodiscard]] auto get_file(const std::filesystem::path& path) -> const File&;

    /// Set the diagnostics engine.
    void set_diags(llvm::IntrusiveRefCntPtr<DiagnosticsEngine> diags);

    /// Whether to enable coloured output.
    [[nodiscard]] bool use_colours() const;
};

/// A diagnostic.
///
/// This holds the data associated with a diagnostic, i.e. the source
/// location, level, and message.
class Diagnostic {
public:
    /// Diagnostic severity.
    enum struct Level : uint8_t {
        Note,    ///< Informational note.
        Warning, ///< Warning, but no hard error.
        Error,   ///< Hard error. Program is ill-formed.
        ICE,     ///< Internal compiler error. Usually used for things we don’t support yet.
    };

    Level level;
    Location where;
    std::string msg;

    /// Create a diagnostic.
    Diagnostic(Level lvl, Location where, std::string msg)
        : level(lvl),
          where(where),
          msg(std::move(msg)) {}

    /// Create a diagnostic with a format string and arguments.
    template <typename... Args>
    Diagnostic(
        Level lvl,
        Location where,
        std::format_string<Args...> fmt,
        Args&&... args
    ) : Diagnostic{lvl, where, std::format(fmt, std::forward<Args>(args)...)} {}

    /// Get the colour of a diagnostic.
    static constexpr auto Colour(Colors C, Level kind) -> std::string_view {
        using Kind = Level;
        using enum Color;
        switch (kind) {
            case Kind::ICE: return C(Magenta);
            case Kind::Warning: return C(Yellow);
            case Kind::Note: return C(Green);
            case Kind::Error: return C(Red);
        }
        return C(None);
    }

    /// Get the name of a diagnostic.
    static constexpr auto Name(Level kind) -> std::string_view {
        using Kind = Level;
        switch (kind) {
            case Kind::ICE: return "Internal Compiler Error";
            case Kind::Error: return "Error";
            case Kind::Warning: return "Warning";
            case Kind::Note: return "Note";
        }
        return "<Invalid Diagnostic Level>";
    }
};

/// This class handles dispatching diagnostics. Objects of this
/// type are thread-safe.
class DiagnosticsEngine : public llvm::RefCountedBase<DiagnosticsEngine> {
    CHOIR_IMMOVABLE(DiagnosticsEngine);
    std::mutex mtx;

protected:
    /// The context that owns this engine.
    const Context& ctx;
    std::atomic<bool> error_flag = false;

public:
    using Ptr = llvm::IntrusiveRefCntPtr<DiagnosticsEngine>;
    virtual ~DiagnosticsEngine() = default;
    explicit DiagnosticsEngine(const Context& ctx) : ctx(ctx) {}

    /// Issue a diagnostic.
    template <typename... Args>
    void diag(
        Diagnostic::Level lvl,
        Location where,
        std::format_string<Args...> fmt,
        Args&&... args
    ) {
        report(Diagnostic{lvl, where, std::format(fmt, std::forward<Args>(args)...)});
    }

    /// Check whether any diagnostics have been issued.
    [[nodiscard]] bool has_error() const { return error_flag.load(std::memory_order_relaxed); }

    /// Issue a diagnostic.
    void report(Diagnostic&& diag) {
        if (diag.level == Diagnostic::Level::Error or diag.level == Diagnostic::Level::ICE)
            error_flag.store(true, std::memory_order_relaxed);

        // Use a lock to avoid interleaving diagnostics.
        std::unique_lock _{mtx};
        report_impl(std::move(diag));
    }

protected:
    /// Override this to implement the actual reporting.
    virtual void report_impl(Diagnostic&& diag) = 0;
};

/// Diagnostics engine that outputs to a stream.
class StreamingDiagnosticsEngine final : public DiagnosticsEngine {
    llvm::raw_ostream& stream;

    /// Used to limit how many errors we print before giving up.
    uint32_t error_limit;
    uint32_t printed = 0;

    StreamingDiagnosticsEngine(const Context& ctx, uint32_t error_limit, llvm::raw_ostream& output_stream)
        : DiagnosticsEngine(ctx), stream(output_stream), error_limit(error_limit) {}

public:
    /// Create a new diagnostic engine.
    [[nodiscard]] static auto Create(
        const Context& ctx,
        uint32_t error_limit = 0,
        llvm::raw_ostream& output_stream = llvm::errs()
    ) -> Ptr {
        return llvm::IntrusiveRefCntPtr(new StreamingDiagnosticsEngine(ctx, error_limit, output_stream));
    }

private:
    void report_impl(Diagnostic&&) override;
};

/// Mixin to provide helper functions to issue diagnostics.
template <typename Derived, typename ErrRetTy = void>
class DiagsProducer {
public:
    template <typename... Args>
    auto Error(Location where, std::format_string<Args...> fmt, Args&&... args) -> ErrRetTy {
        static_cast<Derived*>(this)->Diag(Diagnostic::Level::Error, where, fmt, std::forward<Args>(args)...);
        return ErrRetTy();
    }

    template <typename... Args>
    auto ICE(Location where, std::format_string<Args...> fmt, Args&&... args) -> ErrRetTy {
        static_cast<Derived*>(this)->Diag(Diagnostic::Level::Error, where, fmt, std::forward<Args>(args)...);
        return ErrRetTy();
    }

    template <typename... Args>
    void Note(Location loc, std::format_string<Args...> fmt, Args&&... args) {
        static_cast<Derived*>(this)->Diag(Diagnostic::Level::Note, loc, fmt, std::forward<Args>(args)...);
    }

    template <typename... Args>
    void Warn(Location loc, std::format_string<Args...> fmt, Args&&... args) {
        static_cast<Derived*>(this)->Diag(Diagnostic::Level::Warning, loc, fmt, std::forward<Args>(args)...);
    }
};

/// A file in the context.
class File {
    CHOIR_IMMOVABLE(File);

public:
    /// Path type used by the file system.
    using Path = std::filesystem::path;

private:
    /// Context handle.
    Context& ctx;

    /// The absolute file path.
    Path file_path;

    /// The name of the file as specified on the command line.
    std::string file_name;

    /// The contents of the file.
    std::unique_ptr<llvm::MemoryBuffer> contents;

    /// The id of the file.
    const int32_t id;

public:
    /// Get an iterator to the beginning of the file.
    [[nodiscard]] auto begin() const { return contents->getBufferStart(); }

    /// Get the owning context.
    [[nodiscard]] auto context() const -> Context& { return ctx; }

    /// Get the file data.
    [[nodiscard]] auto data() const -> const char* { return contents->getBufferStart(); }

    /// Get an iterator to the end of the file.
    [[nodiscard]] auto end() const { return contents->getBufferEnd(); }

    /// Get the id of this file.
    [[nodiscard]] auto file_id() const { return id; }

    /// Get the short file name.
    [[nodiscard]] auto name() const -> StringRef { return file_name; }

    /// Get the file path.
    [[nodiscard]] auto path() const -> const Path& { return file_path; }

    /// Get the size of the file.
    [[nodiscard]] auto size() const -> ptrdiff_t { return ptrdiff_t(contents->getBufferSize()); }

    /// Get a temporary file path.
    [[nodiscard]] static auto TempPath(StringRef extension) -> Path;

    /// Write to a file on disk.
    [[nodiscard]] static auto Write(
        const void* data,
        size_t size,
        const Path& file
    ) -> std::expected<void, std::string>;

    /// Write to a file on disk and terminate on error.
    static void WriteOrDie(void* data, size_t size, const Path& file);

private:
    /// The context is the only thing that can create files.
    friend Context;

    /// Construct a file from a name and source.
    explicit File(
        Context& ctx,
        Path path,
        std::string name,
        std::unique_ptr<llvm::MemoryBuffer> contents,
        uint16_t id
    );

    /// Load a file from disk.
    static auto LoadFileData(const Path& path) -> std::unique_ptr<llvm::MemoryBuffer>;
};

/// Used to represent the size of a type.
///
/// This is just a wrapper around an integer, but it requires us
/// to be explicit as to whether we want bits or bytes, which is
/// useful for avoiding mistakes.
class Size {
    size_t raw;

    static_assert(CHAR_BIT == 8);
    constexpr explicit Size(size_t raw) : raw{raw} {}

public:
    constexpr Size() : raw{0} {}
    explicit Size(llvm::Align align) : raw{align.value() * 8} {}

    [[nodiscard]] static constexpr Size Bits(std::unsigned_integral auto bits) { return Size{bits}; }
    [[nodiscard]] static constexpr Size Bytes(std::unsigned_integral auto bytes) { return Size{bytes * 8}; }

    [[nodiscard]] static constexpr Size Bits(std::signed_integral auto bits) {
        CHOIR_ASSERT(bits >= 0, "Size cannot be negative");
        return Size{size_t(bits)};
    }

    [[nodiscard]] static constexpr Size Bytes(std::signed_integral auto bytes) {
        CHOIR_ASSERT(bytes >= 0, "Size cannot be negative");
        return Size{size_t(bytes) * 8};
    }

    /// Return this size aligned to a given alignment.
    [[nodiscard]] Size aligned(llvm::Align align) const {
        return Bytes(alignTo(bytes(), align));
    }

    /// Align this to a given alignment.
    Size& align(llvm::Align align) {
        *this = aligned(align);
        return *this;
    }

    [[nodiscard]] constexpr Size aligned(Size align) const { return Size{llvm::alignTo(bytes(), llvm::Align(align.bytes()))}; }
    [[nodiscard]] constexpr auto bits() const -> size_t { return raw; }
    [[nodiscard]] constexpr auto bytes() const -> size_t { return llvm::alignToPowerOf2(raw, 8) / 8; }

    constexpr Size operator+=(Size rhs) { return Size{raw += rhs.raw}; }
    constexpr Size operator-=(Size rhs) { return Size{raw -= rhs.raw}; }
    constexpr Size operator*=(size_t rhs) { return Size{raw *= rhs}; }

private:
    /// Only provided for Size*Integer since that basically means scaling a size. Multiplying
    /// two sizes w/ one another doesn’t make sense, so that operation is not provided.
    [[nodiscard]] friend constexpr Size operator*(Size lhs, size_t rhs) { return Size{lhs.raw * rhs}; }
    [[nodiscard]] friend constexpr Size operator+(Size lhs, Size rhs) { return Size{lhs.raw + rhs.raw}; }
    [[nodiscard]] friend constexpr bool operator==(Size lhs, Size rhs) = default;
    [[nodiscard]] friend constexpr auto operator<=>(Size lhs, Size rhs) = default;

    /// This needs to check for underflow.
    [[nodiscard]] friend constexpr Size operator-(Size lhs, Size rhs) {
        CHOIR_ASSERT(lhs.raw >= rhs.raw, "Size underflow");
        return Size{lhs.raw - rhs.raw};
    }
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

}; // namespace choir

auto operator+=(std::string& s, choir::String str) -> std::string& {
    s += str.value();
    return s;
}

export namespace choir::utils {

/// Escape non-printable characters in a string.
auto Escape(llvm::StringRef str) -> std::string;

/// Format string that also stores the source location of the caller.
template <typename... Args>
struct FStringWithSrcLocImpl {
    std::format_string<Args...> fmt;
    std::source_location sloc;

    consteval FStringWithSrcLocImpl(
        std::convertible_to<std::string_view> auto fmt,
        std::source_location sloc = std::source_location::current()
    ) : fmt(fmt), sloc(sloc) {}
};

/// Inhibit template argument deduction.
template <typename... Args>
using FStringWithSrcLoc = FStringWithSrcLocImpl<std::type_identity_t<Args>...>;

/// Negate a predicate.
[[nodiscard]] auto Not(auto Predicate) {
    return [Predicate = std::move(Predicate)]<typename... Args>(Args&&... args) {
        return not std::invoke(Predicate, std::forward<Args>(args)...);
    };
}

/// Determine the width of a number when printed.
[[nodiscard]] auto NumberWidth(size_t number, size_t base = 10) -> size_t;

/// Replace all occurrences of `from` with `to` in `str`.
void ReplaceAll(std::string& str, std::string_view from, std::string_view to);

}; // namespace choir::utils

template <>
struct std::formatter<choir::Size> : formatter<size_t> {
    template <typename FormatContext>
    auto format(choir::Size sz, FormatContext& ctx) const {
        return formatter<size_t>::format(sz.bits(), ctx);
    }
};

template <>
struct std::formatter<choir::String> : formatter<std::string_view> {
    template <typename FormatContext>
    auto format(choir::String s, FormatContext& ctx) const {
        return formatter<std::string_view>::format(std::string_view{s.data(), s.size()}, ctx);
    }
};

template <>
struct std::formatter<llvm::StringRef> : formatter<std::string_view> {
    template <typename FormatContext>
    auto format(llvm::StringRef s, FormatContext& ctx) const {
        return formatter<std::string_view>::format(std::string_view{s.data(), s.size()}, ctx);
    }
};

// TODO: Remove once this is part of C++26.
template <>
struct std::formatter<std::filesystem::path> : std::formatter<std::string> {
    template <typename FormatContext>
    auto format(const std::filesystem::path& path, FormatContext& ctx) const {
        return std::formatter<std::string>::format(path.string(), ctx);
    }
};
