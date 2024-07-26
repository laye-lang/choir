module;

#include <choir/macros.hh>
#include <filesystem>
#include <future>
#include <llvm/ADT/StringMap.h>
#include <llvm/ADT/SmallVector.h>
#include <llvm/Support/ThreadPool.h>
#include <mutex>

module driver;

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

struct Driver::Impl {
    std::mutex mutex;

    DriverOptions _options;

    Impl(DriverOptions options) : _options(options){};

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
            CHOIR_TODO("Add this file to the Laye job please");
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
//  API
// ============================================================================

CHOIR_DEFINE_HIDDEN_IMPL(Driver);
Driver::Driver(DriverOptions options) : impl(new Impl{options}) {}

void Driver::add_file(std::string_view file_path, SourceFileKind file_kind) {
    std::unique_lock _{impl->mutex};
    impl->add_file(file_path, file_kind);
}
