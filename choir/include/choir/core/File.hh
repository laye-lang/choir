#ifndef CHOIR_API_CORE_FILE_HH
#define CHOIR_API_CORE_FILE_HH

#include <choir/core/Macros.hh>
#include <choir/core/Types.hh>
#include <filesystem>
#include <llvm/Support/MemoryBuffer.h>

namespace choir {
class Context;

class File {
    CHOIR_IMMOVABLE(File);

public:
    using Path = std::filesystem::path;

private:
    /// The Choir context.
    Context& context;

    /// The absolute file path.
    Path file_path;

    /// The name of the file as specified on the command line.
    std::string file_name;

    /// The contents of the file.
    std::unique_ptr<llvm::MemoryBuffer> contents;

    /// The id of the file.
    const int32_t id;
};
}; // namespace choir

#endif // !CHOIR_API_CORE_FILE_HH
