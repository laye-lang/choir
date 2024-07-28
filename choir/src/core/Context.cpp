module;

#include <choir/macros.hh>
#include <filesystem>
#include <llvm/ADT/IntrusiveRefCntPtr.h>
#include <llvm/ADT/StringRef.h>
#include <llvm/IR/LLVMContext.h>
#include <llvm/Support/TargetSelect.h>
#include <unordered_map>
#include <mutex>
#include <thread>

module choir;
using namespace choir;

struct Context::Impl {
    llvm::LLVMContext llvm;

    /// Diagnostics engine.
    llvm::IntrusiveRefCntPtr<DiagnosticsEngine> diags_engine;

    /// Mutex used by API functions that may mutate the context.
    std::recursive_mutex context_mutex;

    /// Files loaded by the context.
    std::vector<std::unique_ptr<File>> files;
    std::unordered_map<File::Path, File*> files_by_path; // FIXME: use inode number instead.

    /// Mutex used for printing diagnostics.
    mutable std::recursive_mutex diags_mutex;

    /// Whether there was an error.
    mutable std::atomic<bool> errored = false;

    /// Whether to use coloured output.
    std::atomic<bool> enable_colours = true;
};

CHOIR_DEFINE_HIDDEN_IMPL(Context);
Context::Context() : impl(new Impl) {
    static std::once_flag init_flag;
    std::call_once(init_flag, [] {
        llvm::InitializeNativeTarget();
        llvm::InitializeNativeTargetAsmPrinter();
    });
}

auto Context::diags() const -> DiagnosticsEngine& {
    CHOIR_ASSERT(impl->diags_engine, "Diagnostics engine not set!");
    return *impl->diags_engine;
}

void Context::enable_colours(bool enable) {
    impl->enable_colours.store(enable, std::memory_order_release);
}

auto Context::file(size_t idx) const -> const File* {
    std::unique_lock _{impl->context_mutex};

    if (idx >= impl->files.size()) return nullptr;
    return impl->files[idx].get();
}

auto Context::get_file(const File::Path& path) -> const File& {
    std::unique_lock _{impl->context_mutex};

    auto can = canonical(path);
    if (auto it = impl->files_by_path.find(can); it != impl->files_by_path.end())
        return *it->second;

    static constexpr size_t MaxFiles = std::numeric_limits<uint16_t>::max();
    CHOIR_ASSERT(
        impl->files.size() < MaxFiles,
        "Sorry, that’s too many files for us! (max is {})",
        MaxFiles
    );

    auto mem = File::LoadFileData(can);
    auto f = new File(*this, can, path.string(), std::move(mem), uint16_t(impl->files.size()));
    impl->files.emplace_back(f);
    impl->files_by_path[std::move(can)] = f;
    return *f;
}

void Context::set_diags(llvm::IntrusiveRefCntPtr<DiagnosticsEngine> diags) {
    impl->diags_engine = std::move(diags);
}

bool Context::use_colours() const {
    return impl->enable_colours.load(std::memory_order_acquire);
}
