#ifndef CHOIR_CORE_HH
#define CHOIR_CORE_HH

#include <algorithm>
#include <bit>
#include <choir/macros.hh>
#include <choir/core/rtti.hh>
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
#include <source_location>
#include <string>

namespace choir {
using u8 = std::uint8_t;
using u16 = std::uint16_t;
using u32 = std::uint32_t;
using u64 = std::uint64_t;
using usz = std::size_t;
using uptr = std::uintptr_t;

using i8 = std::int8_t;
using i16 = std::int16_t;
using i32 = std::int32_t;
using i64 = std::int64_t;
using isz = std::make_signed_t<std::size_t>;
using iptr = std::intptr_t;

using f32 = float;
using f64 = double;

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
template <typename ElementType>
class DirectedGraph;
enum struct Color;
struct Colors;
}; // namespace choir

namespace choir {

using llvm::StringRef;

enum struct Linkage {
    /// Local variable.
    ///
    /// This is just a dummy value that is used for local variables
    /// only. In particular, a top-level declaration that is marked
    /// as local is treated as a variable local to the top-level
    /// function.
    LocalVar,

    /// Not exported. Will be deleted if unused.
    ///
    /// This is used for variables and functions that are defined in
    /// and local to this module. A variable or function marked with
    /// this attribute will be *deleted* if it is not used anywhere
    /// and will not be accessible to outside code.
    Internal,

    /// Like internal, but will not be deleted.
    ///
    /// This is for variables and functions that are not really exported
    /// and behave just like internal variables and functions, except that
    /// their name will be included in the object file’s symbol table.
    Used,

    /// Exported. May be used by other modules.
    ///
    /// This is used for variables and functions that are defined in
    /// this module and exported. Variables and functions marked with
    /// this attribute will not be deleted even if they are not
    /// referenced anywhere.
    Exported,

    /// Imported from another module or from C.
    ///
    /// This is used for variables and functions imported from outside
    /// code, whether via importing an Intercept module or simply declaring
    /// an external symbol. This linkage type means that the object is
    /// not defined in this module and that it will be made accessible at
    /// link time only. However, this module will not export the symbol.
    Imported,

    /// Imported *and* exported.
    ///
    /// This sort of combines exported and imported in that it means that
    /// the symbol is exported from this module, which will make it accessible
    /// to other *Intercept modules* that import this module, but unlike
    /// regular exports, this module does not have a definition of the symbol.
    Reexported,
};

enum struct CallConv {
    /// C calling convention.
    C,
};

constexpr auto IsExportedLinkage(Linkage link) -> bool {
    switch (link) {
        case Linkage::LocalVar:
        case Linkage::Internal:
        case Linkage::Imported:
            return false;

        case Linkage::Used:
        case Linkage::Exported:
        case Linkage::Reexported:
            return true;
    }

    CHOIR_UNREACHABLE();
}

constexpr auto IsImportedLinkage(Linkage link) -> bool {
    switch (link) {
        case Linkage::LocalVar:
        case Linkage::Internal:
        case Linkage::Used:
        case Linkage::Exported:
            return false;

        case Linkage::Imported:
        case Linkage::Reexported:
            return true;
    }

    CHOIR_UNREACHABLE();
}

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
    void enable_colors(bool enable);

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
        ICE,     ///< Internal compiler error. Usually used for things we don�t support yet.
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
    /// two sizes w/ one another doesn�t make sense, so that operation is not provided.
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
/// This is used for strings that are guaranteed to �live long
/// enough� to be passed around without having to worry about who
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
        CHOIR_ASSERT(arr[size - 1] == '\0', "Strings must be null-terminated!");
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

    /// Save it in a string saver; this is how you�re supposed to create these.
    [[nodiscard]] static auto Save(llvm::StringSaver& ss, StringRef s) {
        return CreateUnsafe(ss.save(s));
    }

    [[nodiscard]] static auto Save(llvm::UniqueStringSaver& ss, StringRef s) {
        return CreateUnsafe(ss.save(s));
    }
};

template <typename ElementType>
class DirectedGraph {
    std::vector<ElementType> _nodes{};
    std::vector<llvm::SmallVector<u32, 16>> _edges{};

private:
    auto add_node_internal(ElementType element) -> u32 {
        CHOIR_ASSERT(_nodes.size() == _edges.size());

        for (u32 i = 0, count = u32(_nodes.size()); i < count; i++) {
            if (_nodes[i] == element) return i;
        }

        u32 node_index = u32(_nodes.size());
        _nodes.push_back(element);
        _edges.push_back({});

        return node_index;
    }

public:
    enum struct OrderKind {
        Ok,
        Cycle,
    };

    struct OrderResult {
        OrderKind kind;
        std::vector<ElementType> elements{};
        ElementType cycle_from{};
        ElementType cycle_to{};
    };

    explicit DirectedGraph() {}

private:
    auto resolve_order(std::vector<ElementType>& resolved, std::vector<ElementType>& seen, ElementType element) const -> OrderResult {
        auto nodes_it = std::find(_nodes.begin(), _nodes.end(), element);
        CHOIR_ASSERT(nodes_it != _nodes.end(), "must only resolve nodes which are present in the graph");

        CHOIR_ASSERT(nodes_it >= _nodes.begin());
        usz node_index = usz(nodes_it - _nodes.begin());

        if (auto resolved_it = std::find(resolved.begin(), resolved.end(), element); resolved_it != resolved.end()) {
            return {OrderKind::Ok};
        }

        seen.push_back(element);

        if (auto edges = _edges[node_index]; not edges.empty()) {
            for (u32 v : edges) {
                ElementType dependency = _nodes[v];
                if (auto resolved_it = std::find(resolved.begin(), resolved.end(), dependency); resolved_it != resolved.end()) {
                    continue;
                }

                if (auto seen_it = std::find(seen.begin(), seen.end(), dependency); seen_it != seen.end()) {
                    return {OrderKind::Cycle, {}, element, dependency};
                }

                auto dep_result = resolve_order(resolved, seen, dependency);
                if (dep_result.kind != OrderKind::Ok) {
                    return dep_result;
                }
            }
        }

        resolved.push_back(element);
        seen.erase(std::find(seen.begin(), seen.end(), element));

        return {OrderKind::Ok};
    }

public:
    auto add_node(ElementType element) -> void {
        add_node_internal(element);
    }

    auto add_edge(ElementType from, ElementType to) {
        u32 from_index = add_node_internal(from);
        u32 to_index = add_node_internal(to);

        auto& from_edges = _edges[from_index];
        for (u32 i = 0, count = u32(from_edges.size()); i < count; i++) {
            if (from_edges[i] == to_index) return;
        }

        from_edges.push_back(to_index);
    }

    auto ordered_elements() const -> OrderResult {
        std::vector<ElementType> ordered_elements{};
        std::vector<ElementType> seen{};

        ordered_elements.reserve(_nodes.size());
        seen.reserve(_nodes.size());

        for (ElementType element : _nodes) {
            auto result = resolve_order(ordered_elements, seen, element);
            if (result.kind != OrderKind::Ok) {
                return result;
            }
        }

        return {
            OrderKind::Ok,
            std::move(ordered_elements),
        };
    }
};

}; // namespace choir

auto operator+=(std::string& s, choir::String str) -> std::string&;

namespace choir::utils {

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
[[nodiscard]] auto NumberWidth(usz number, usz base = 10) -> usz;

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

#endif // CHOIR_CORE_HH
