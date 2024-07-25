#include "driver.hh"

#include <format>
#include <llvm/IR/IRBuilder.h>
#include <llvm/IR/LegacyPassManager.h>
#include <llvm/IR/LLVMContext.h>
#include <llvm/IR/Module.h>
#include <llvm/MC/MCContext.h>
#include <llvm/MC/TargetRegistry.h>
#include <llvm/Passes/PassBuilder.h>
#include <llvm/Support/InitLLVM.h>
#include <llvm/Support/LLVMDriver.h>
#include <llvm/Support/Program.h>
#include <llvm/Support/raw_ostream.h>
#include <llvm/Support/TargetSelect.h>
#include <llvm/Target/TargetMachine.h>
#include <llvm/TargetParser/Host.h>

int example_main(int argc, char** argv) {
    llvm::SmallVector<const char*, 256> Args(argv, argv + argc);
    if (argc > 2 && std::string_view(argv[1]) == "cc") {
        llvm::SmallVector<llvm::StringRef, 256> clang_args{};
        for (auto arg : llvm::ArrayRef(Args).drop_front(2)) {
            clang_args.push_back(arg);
        }

        return invoke_clang_driver(clang_args);
    }

    llvm::InitializeNativeTarget();
    llvm::InitializeNativeTargetAsmPrinter();
    llvm::LLVMContext context;
    llvm::Module mymodule("mymodule", context);
    llvm::IRBuilder<> irbuilder(context);
    auto function = mymodule.getOrInsertFunction("main", llvm::FunctionType::get(irbuilder.getVoidTy(), false));
    auto entry_block = llvm::BasicBlock::Create(context, "entry", llvm::cast<llvm::Function>(function.getCallee()));
    irbuilder.SetInsertPoint(entry_block);
    irbuilder.CreateCall(
        mymodule.getOrInsertFunction("puts", llvm::FunctionType::get(irbuilder.getInt32Ty(), {irbuilder.getPtrTy()}, false)),
        irbuilder.CreateGlobalStringPtr("Hello, hunter!")
    );
    irbuilder.CreateRetVoid();
    mymodule.dump();

    std::string object_file_path = "a.out";
    std::string exe_file_path = "main.exe";

    emit_object_file(mymodule, object_file_path);

    llvm::SmallVector<llvm::StringRef, 4> Args2{};
    Args2.push_back(CHOIR_CLANG_EXE_PATH);
    Args2.push_back("-o");
    Args2.push_back(exe_file_path);
    Args2.push_back(object_file_path);

    return invoke_clang_driver(Args2);
}
