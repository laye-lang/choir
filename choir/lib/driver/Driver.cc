#include <choir/macros.hh>
#include <choir/core.hh>
#include <choir/driver.hh>
#include <choir/front/laye.hh>
#include <filesystem>
#include <format>
#include <future>
#include <llvm/ADT/SmallVector.h>
#include <llvm/ADT/StringMap.h>
#include <llvm/Support/ThreadPool.h>
#include <mutex>
#include <print>
#include <unordered_set>
#include <vector>

using llvm::StringRef;

using namespace choir;

llvm::StringMap<SourceFileKind> file_kinds_by_extension{
    {".laye", SourceFileKind::Laye},
    {".c", SourceFileKind::C},
    {".h", SourceFileKind::C},
    {".cpp", SourceFileKind::CXX},
    {".ixx", SourceFileKind::CXX},
    {".cc", SourceFileKind::CXX},
    {".ccm", SourceFileKind::CXX},
};

// ============================================================================
//  Internal API
// ============================================================================

namespace choir {

enum struct DriverJobKind {
    BuildLaye,
    BuildC,
    BuildCXX,
};

class DriverJob : DiagsProducer<DriverJob> {
    CHOIR_IMMOVABLE(DriverJob);

    DriverJobKind _kind;
    Context& _context;
    DriverOptions _options;

protected:
    DriverJob(DriverJobKind kind, Context& context, DriverOptions options) : _kind(kind), _context(context), _options(options) {}
    virtual ~DriverJob() = default;

    template <typename... Args>
    void Diag(Diagnostic::Level level, Location loc, std::format_string<Args...> fmt, Args&&... args) {
        _context.diags().diag(level, loc, fmt, std::forward<Args>(args)...);
    }

    template <typename... Args>
    void Error(std::format_string<Args...> fmt, Args&&... args) {
        Diag(Diagnostic::Level::Error, Location(), fmt, std::forward<Args>(args)...);
    }

public:
    DriverJobKind kind() const { return _kind; }
    Context& context() const { return _context; }
    DriverOptions options() const { return _options; }

    virtual void run() = 0;
};

class BuildLayeDriverJob : public DriverJob {
    std::unordered_set<File::Path> file_uniquer;
    std::vector<File::Path> source_files{};

public:
    BuildLayeDriverJob(Context& context, DriverOptions options) : DriverJob(DriverJobKind::BuildLaye, context, options) {}
    ~BuildLayeDriverJob() override = default;

    void add_source_file(File::Path file_path);
    void run() override;
};

}; // namespace choir

// ============================================================================
//  Implementation
// ============================================================================

void BuildLayeDriverJob::add_source_file(File::Path file_path) {
    auto canonical_path = std::filesystem::canonical(file_path);
    if (not file_uniquer.insert(canonical_path).second) {
        Error("Duplicate file name in command line arguments: '{}'", canonical_path);
        return;
    }

    source_files.push_back(canonical_path);
}

void BuildLayeDriverJob::run() {
    laye::SyntaxGraph syntax{context(), options().action == DriverAction::Lex};
    for (auto& file_path : source_files) {
        syntax.add_file(file_path);
    }

    auto ordered_modules = syntax.ordered_modules();
    if (options().action == DriverAction::Lex) {
        for (const auto& module : ordered_modules) {
            std::println("Module '{}'", module->source_file().name());
            for (const auto& token : module->tokens()) {
                if (auto loc_info = token.location.seek_line_column(context())) {
                    std::print("  ({}:{}) ", loc_info->line, loc_info->col);
                } else {
                    std::print("  (?:?) ");
                }

                std::println("[{}] '{}' ({})", laye::SyntaxToken::KindToString(token.kind), token.text, token.spelling(context()));

                if (token.kind == laye::SyntaxToken::Kind::LiteralInteger) {
                    llvm::SmallVector<char, 16> buf{};
                    token.integer_value.toStringUnsigned(buf);
                    std::println("    literal value: {}", StringRef{buf.data(), buf.size()});
                } else if (token.kind == laye::SyntaxToken::Kind::LiteralFloat) {
                    llvm::SmallVector<char, 16> buf{};
                    token.float_value.toString(buf);
                    std::println("    literal value: {}", StringRef{buf.data(), buf.size()});
                } else if (token.kind == laye::SyntaxToken::Kind::LiteralRune) {
                    i32 codepoint = i32(token.integer_value.getSExtValue());
                    if (codepoint < 256) {
                        std::println("    literal value: '{}'", char(codepoint));
                    } else {
                        std::println("    literal value: '\\U{:X}'", codepoint);
                    }
                }

                if (not token.leading_trivia.empty() or not token.trailing_trivia.empty()) {
                    CHOIR_TODO("print token trivia");
                }
            }
        }

        return;
    }

    if (options().action == DriverAction::Parse) {
        CHOIR_TODO("print AST");
        return;
    }
}

struct Driver::Impl : DiagsProducer<Driver::Impl> {
    std::mutex mutex{};

    Context context{};
    DriverOptions options;

    BuildLayeDriverJob* build_laye_job{};
    std::vector<DriverJob*> jobs{};

    bool invoked = false;

    Impl(DriverOptions options) : options(options) {
        // ensuyre that we have the streaming diags engine so the driver can report to the terminal
        context.set_diags(StreamingDiagnosticsEngine::Create(context, options.error_limit));
        context.enable_colors(options.colors and not options.verify);

        build_laye_job = new BuildLayeDriverJob{context, options};
        jobs.push_back(build_laye_job);
    }

    template <typename... Args>
    void Diag(Diagnostic::Level level, Location loc, std::format_string<Args...> fmt, Args&&... args) {
        context.diags().diag(level, loc, fmt, std::forward<Args>(args)...);
    }

    template <typename... Args>
    int Error(std::format_string<Args...> fmt, Args&&... args) {
        Diag(Diagnostic::Level::Error, Location(), fmt, std::forward<Args>(args)...);
        return 1;
    }

    int execute_jobs();
    void add_file(File::Path file_path, SourceFileKind file_kind);
};

void Driver::Impl::add_file(File::Path file_path, SourceFileKind file_kind) {
    if (!std::filesystem::exists(file_path)) {
        Error("File '{}' does not exist.", file_path);
        return;
    }

    if (file_kind == SourceFileKind::Default) {
        if (not file_path.has_extension()) {
            Error("Source file '{}' has no file extension. Use the '-x' option to specify file types for files with no extensions.", file_path);
            return;
        }

        auto extension = file_path.extension().string();
        // TODO(local): Technically, the fringe file extension .C (capital C, yes, so not on Windows) is also a C++ file extension primarily used by GNU. We should really accept it, but not right now.
        auto it = file_kinds_by_extension.find(extension);

        if (it == file_kinds_by_extension.end()) {
            Error("Source file '{}' has an unrecognized file extension '{}'. Use the '-x' option to specify file types for files with unrecognized extensions.", file_path, extension);
            return;
        }

        file_kind = it->getValue();
    }

    switch (file_kind) {
        default: {
            CHOIR_UNREACHABLE("An unhandled source file kind was encountered. Oops.");
        } break;

        case SourceFileKind::Default: {
            CHOIR_UNREACHABLE("The default source file kind should have been resolved before reaching this switch.");
        } break;

        case SourceFileKind::Laye: {
            build_laye_job->add_source_file(file_path);
        } break;

        case SourceFileKind::C: {
            CHOIR_TODO("Build a job to send a C language file to Clang");
        } break;

        case SourceFileKind::CXX: {
            CHOIR_TODO("Build a job to send a C++ language file to Clang");
        } break;
    }
}

int Driver::Impl::execute_jobs() {
    CHOIR_ASSERT(not invoked, "Can only call execute_jobs() once ber driver instance!");
    invoked = true;

    if (context.diags().has_error()) return 1;

    build_laye_job->run();
    if (context.diags().has_error()) return 1;

    return 0;
}

// ============================================================================
//  Public API
// ============================================================================

CHOIR_DEFINE_HIDDEN_IMPL(Driver);
Driver::Driver(DriverOptions options) : impl(new Impl{options}) {}

int Driver::execute() {
    std::unique_lock _{impl->mutex};
    return impl->execute_jobs();
}

void Driver::add_file(std::string_view file_path, SourceFileKind file_kind) {
    std::unique_lock _{impl->mutex};
    impl->add_file(file_path, file_kind);
}
