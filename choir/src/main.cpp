#include <choir/macros.hh>
#include <clopts.hh>
#include <cstdio>
#include <llvm/ADT/ArrayRef.h>
#include <llvm/ADT/StringSwitch.h>
#include <llvm/Support/LLVMDriver.h>
#include <typeinfo>

import choir.driver;

using namespace choir;

namespace detail {

using namespace command_line_options;
using options = clopts< // clang-format off
    multiple<positional<"file", "The file to compile", ref<std::string, "-x">>>,
    // TODO(local): when aliases work, support a long options
    experimental::short_option<"-x", "Override the language", std::string, false, true>,
    help<>
>; // clang-format on

};

int choir_main(int argc, char** argv, const llvm::ToolContext& tool_context) {
    (void) tool_context;

    auto opts = ::detail::options::parse(argc, argv);

    ::printf("Hello, choir 2!\n");

    DriverOptions driver_options{};
    Driver driver{driver_options};

    // NOTE(local): the following two lines are written out explicitly for two reasons:
    // 1. (the more important reason) Visual Studio IntelliSense is not currently working the same
    //    as the actual compiler, so while the code *compiles*, the IntelliSense states that
    //    opts.get<"file">(); isn't std::span<tuple> and complains.
    //    Since it can't figure it out, we moved it to a variable outside the following loop
    //    so that the structure binding [file_path, file_kind_opt] isn't the source of the error,
    //    which keeps the contents of the for loop clean in editors where this is the case.
    // 2. Less importantly, it helps programmers unfamiliar with the command line options library
    //    being used (read: 99.9% of programmers) understand what we're doing in that loop in the
    //    first place, so it will likely remain this way even if the IntelliSense bug disappears.
    using tuple = std::tuple<std::string, std::optional<std::string>>;
    std::span<tuple> files = opts.get<"file">();

    if (files.size() == 0) {
        return 1;
    }

    for (auto& [file_path, file_kind_opt] : files) {
        auto kind = SourceFileKind::Default;
        if (file_kind_opt) {
            std::string file_kind = *file_kind_opt;
            kind = llvm::StringSwitch<SourceFileKind>(file_kind)
                       .Case("laye", SourceFileKind::Laye)
                       .Case("c", SourceFileKind::C)
                       .Case("c++", SourceFileKind::CXX)
                       .Default(SourceFileKind::Default);
        }

        driver.add_file(file_path, kind);
    }

    int result = driver.execute();
    if (result != 0) {
        return result;
    }

    return 0;
}
