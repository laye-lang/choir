#include <choir/macros.hh>
#include <clopts.hh>
#include <cstdio>
#include <llvm/ADT/ArrayRef.h>
#include <llvm/Support/LLVMDriver.h>

import driver;

using namespace choir;

namespace detail {

using namespace command_line_options;
using options = clopts< // clang-format off
    multiple<positional<"file", "The file to compile">>,
    help<>
>; // clang-format on

};

int choir_main(int argc, char** argv, const llvm::ToolContext& tool_context) {
    (void) tool_context;

    auto opts = ::detail::options::parse(argc, argv);

    ::printf("Hello, choir 2!\n");

    DriverOptions driver_options{};
    Driver driver{driver_options};

    for (auto& file_path : *opts.get<"file">()) {
        driver.add_file(file_path);
    }

    return 0;
}
