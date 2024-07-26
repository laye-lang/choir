#include <cstdio>
#include <llvm/ADT/ArrayRef.h>
#include <llvm/Support/LLVMDriver.h>
#include <choir/driver.hh>

int choir_main(int argc, char** argv, const llvm::ToolContext& tool_context) {
    (void)argc;
    (void)argv;
    (void)tool_context;

    ::printf("Hello, choir!\n");

    choir::Driver driver{};

    return 0;
}
