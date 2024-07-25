#include "driver.hh"

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

int emit_object_file(llvm::Module& llvm_module, const std::string& object_file_path) {
    std::string error;
    auto triple = llvm::sys::getDefaultTargetTriple();
    auto target = llvm::TargetRegistry::lookupTarget(triple, error);
    if (not error.empty() or not target) Die(
        "Failed to lookup target triple '{}': {}",
        triple,
        error
    );

    /// Get CPU.
    std::string cpu = "generic";

    /// Target options.
    llvm::TargetOptions opts;

    /// Get opt level.
    llvm::CodeGenOptLevel opt = llvm::CodeGenOptLevel::Aggressive;

    /// Create machine.
    auto reloc = llvm::Reloc::PIC_;
    auto machine = target->createTargetMachine(
        triple,
        cpu,          /// Target CPU
        "",           /// Features.
        opts,         /// Options.
        reloc,        /// Relocation model.
        std::nullopt, /// Code model.
        opt,          /// Opt level.
        false         /// JIT?
    );

    // Assert(machine, "Failed to create target machine");
    if (!machine) Die("oops");

    /// Set target triple for the module.
    llvm_module.setTargetTriple(triple);

    /// Set PIC level and DL.
    llvm_module.setPICLevel(llvm::PICLevel::Level::BigPIC);
    llvm_module.setPIELevel(llvm::PIELevel::Level::Large);
    llvm_module.setDataLayout(machine->createDataLayout());

    /// Helper to emit an object/assembly file.
    auto EmitFile = [&](llvm::raw_pwrite_stream& stream) {
        /// No idea how or if the new pass manager can be used for this, so...
        llvm::legacy::PassManager pass;
        if (
            machine->addPassesToEmitFile(
                pass,
                stream,
                nullptr,
                llvm::CodeGenFileType::ObjectFile
            )
        ) Die("LLVM backend rejected object code emission passes");
        pass.run(llvm_module);
        stream.flush();
    };

    {
        std::error_code ec;
        llvm::raw_fd_ostream stream{
            object_file_path,
            ec,
            llvm::sys::fs::OF_None,
        };

        if (ec) Die(
            "Could not open file '{}': {}",
            object_file_path,
            ec.message()
        );

        EmitFile(stream);
    }

    return 0;
}
