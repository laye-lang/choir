#include <choir/core.hh>
#include <choir/driver.hh>
#include <filesystem>
#include <future>
#include <llvm/ADT/IntrusiveRefCntPtr.h>
#include <llvm/ADT/SmallVector.h>
#include <llvm/Support/ThreadPool.h>
#include <mutex>

namespace choir {

struct Driver::Impl {
    std::mutex mutex;
    llvm::SmallVector<File::Path> files;

    Impl();
};

Driver::Impl::Impl() {
}

}; // namespace choir

// ============================================================================
//  API
// ============================================================================

namespace choir {

CHOIR_DEFINE_HIDDEN_IMPL(Driver);
Driver::Driver() : impl(new Impl{}) {}

void Driver::add_file(std::string_view file_path, SourceFileKind file_kind) {
    std::unique_lock _{impl->mutex};
    impl->files.push_back(File::Path(file_path));
}

}; // namespace choir
