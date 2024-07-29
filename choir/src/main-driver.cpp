#include <llvm/ADT/ArrayRef.h>
#include <llvm/Support/LLVMDriver.h>

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#endif

int choir_main(int argc, char** argv, const llvm::ToolContext &tool_context);

int main(int argc, char** argv) {
#ifdef _WIN32
    _set_abort_behavior(0, _WRITE_ABORT_MSG | _CALL_REPORTFAULT);
#endif
    return choir_main(argc, argv, {argv[0], nullptr, false});
}
