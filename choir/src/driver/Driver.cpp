module;

#include <choir/macros.hh>
#include <filesystem>
#include <future>
#include <llvm/ADT/StringMap.h>
#include <llvm/ADT/SmallVector.h>
#include <llvm/Support/ThreadPool.h>
#include <mutex>
#include <vector>

module choir.driver;

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

class DriverJob {
    CHOIR_IMMOVABLE(DriverJob);

    DriverJobKind _kind;

protected:
    DriverJob(DriverJobKind kind) : _kind(kind) {}

public:
    DriverJobKind kind() const { return _kind; }

    virtual void run() = 0;
};

class BuildLayeDriverJob : public DriverJob {
    std::vector<StringRef> laye_source_file_paths{};

public:
    BuildLayeDriverJob() : DriverJob(DriverJobKind::BuildLaye) {}

    void add_laye_source_file(StringRef laye_source_file_path);
    void run() override;
};

}; // namespace choir

// ============================================================================
//  Implementation
// ============================================================================

void BuildLayeDriverJob::add_laye_source_file(StringRef laye_source_file_path) {
    laye_source_file_paths.push_back(laye_source_file_path);
}

void BuildLayeDriverJob::run() {
}

struct Driver::Impl {
    DriverOptions options;

    BuildLayeDriverJob* build_laye_job{};
    std::vector<DriverJob*> jobs{};
    std::mutex mutex;

    Impl(DriverOptions options) : options(options) {
        build_laye_job = new BuildLayeDriverJob{};
        jobs.push_back(build_laye_job);
    }

    void add_file(StringRef file_path, SourceFileKind file_kind);
};

void Driver::Impl::add_file(StringRef file_path, SourceFileKind file_kind) {
    if (file_kind == SourceFileKind::Default) {
        auto last_dot = file_path.find_last_of(".");
        if (last_dot == StringRef::npos) {
            CHOIR_FATAL("This needs to be a driver diagnostic: The source file kind was not specified on the command line, and it has no extension. Oops.");
        }

        auto extension = file_path.substr(last_dot).lower();
        // TODO(local): Technically, the fringe file extension .C (capital C, yes, so not on Windows) is also a C++ file extension primarily used by GNU. We should really accept it, but not right now.
        auto it = file_kinds_by_extension.find(extension);

        if (it == file_kinds_by_extension.end()) {
            CHOIR_FATAL("This needs to be a driver diagnostic: The source file kind was not specified on the command line, and its extension is not recognized. Oops.");
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
            build_laye_job->add_laye_source_file(file_path);
        } break;

        case SourceFileKind::C: {
            CHOIR_TODO("Build a job to send a C language file to Clang");
        } break;

        case SourceFileKind::CXX: {
            CHOIR_TODO("Build a job to send a C++ language file to Clang");
        } break;
    }
}

// ============================================================================
//  Public API
// ============================================================================

CHOIR_DEFINE_HIDDEN_IMPL(Driver);
Driver::Driver(DriverOptions options) : impl(new Impl{options}) {}

void Driver::execute() {
    std::unique_lock _{impl->mutex};
}

void Driver::add_file(std::string_view file_path, SourceFileKind file_kind) {
    std::unique_lock _{impl->mutex};
    impl->add_file(file_path, file_kind);
}
