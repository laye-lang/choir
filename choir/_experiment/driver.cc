#include "driver.hh"

#include <clang/Tooling/Tooling.h>
#include <llvm/Support/InitLLVM.h>
#include <llvm/Support/FileSystem.h>

int choir_main(int argc, char** argv) {
    llvm::SmallString<_MAX_PATH> current_path{};
    auto path_error = llvm::sys::fs::current_path(current_path);

    clang::FileManager file_manager{{std::string(current_path)}};
    auto test_c = file_manager.getFileRef("test.c");
    if (auto error = test_c.takeError()) {
        Die("fuck");
    }

    auto file_contents_or_error = file_manager.getBufferForFile(test_c.get());
    if (auto error = file_contents_or_error.getError()) {
        Die("fuck 2");
    }

    auto file_contents_buffer = std::move(file_contents_or_error.get());

    auto ast_unit = clang::tooling::buildASTFromCodeWithArgs(file_contents_buffer->getBuffer(), {}, "input.c");
    //ast_unit.get()->getPreprocessor().getMacroDefinition().;
    auto& ast_context = ast_unit->getASTContext();
    auto tu_decl = ast_context.getTranslationUnitDecl();
    for (auto decl : tu_decl->decls()) {
        if (auto function_decl = llvm::dyn_cast<clang::FunctionDecl>(decl)) {
            function_decl->dump();
        }
    }
    
    return 0;
}

int main(int argc, char** argv) {
    llvm::InitLLVM X(argc, argv);
    return choir_main(argc, argv);
}
