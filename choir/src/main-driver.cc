#include <llvm/ADT/ArrayRef.h>
#include <llvm/Support/InitLLVM.h>
#include <llvm/Support/LLVMDriver.h>

int choir_main(int argc, char** argv, const llvm::ToolContext &tool_context);

int main(int argc, char** argv) {
    llvm::InitLLVM X(argc, argv);
    return choir_main(argc, argv, {argv[0], nullptr, false});
}
