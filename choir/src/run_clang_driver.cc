#include "driver.hh"

#include <llvm/Support/Program.h>

int invoke_clang_driver(llvm::ArrayRef<llvm::StringRef> Args) {
    return llvm::sys::ExecuteAndWait(CHOIR_CLANG_EXE_PATH, Args);
}
