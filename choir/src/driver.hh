#ifndef CHOIR_DRIVER_HH
#define CHOIR_DRIVER_HH

#include <format>
#include <llvm/ADT/ArrayRef.h>
#include <llvm/ADT/StringRef.h>
#include <llvm/IR/Module.h>

template <typename... Args>
void Die(std::format_string<Args...> fmt, Args&&... args) {
    auto s = std::format(fmt, std::forward<Args>(args)...);
    puts(s.c_str());
    exit(1);
}

int invoke_clang_driver(llvm::ArrayRef<llvm::StringRef> Args);
int emit_object_file(llvm::Module& llvm_module, const std::string& object_file_path);

#endif // !CHOIR_DRIVER_HH
