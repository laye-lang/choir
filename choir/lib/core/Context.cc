#include <choir/core.hh>
#include <llvm/Support/TargetSelect.h>
#include <llvm/IR/LLVMContext.h>

namespace choir {

struct Context::Impl {
    llvm::LLVMContext llvm{};
};

}; // namespace choir

// ============================================================================
//  API
// ============================================================================

namespace choir {

CHOIR_DEFINE_HIDDEN_IMPL(Context);
Context::Context() : impl(new Impl{}) {
    static std::once_flag init_flag;
    std::call_once(init_flag, [] {
        llvm::InitializeNativeTarget();
        llvm::InitializeNativeTargetAsmPrinter();
    });
}

}; // namespace choir
