#ifndef CHOIR_DRIVER_HH
#define CHOIR_DRIVER_HH

#include <choir/macros.hh>
#include <choir/core.hh>
#include <string_view>

namespace choir {

enum struct DriverAction {
    Compile,
    Sema,
    Parse,
    Lex,
};

enum struct SourceFileKind {
    Default,
    Laye,
    C,
    CXX,
};

struct DriverOptions {
    DriverAction action;
    bool colors;
    u32 error_limit;
    bool verify;
};

class Driver {
    CHOIR_DECLARE_HIDDEN_IMPL(Driver);

public:
    Driver(DriverOptions options);

    int execute();
    void add_file(std::string_view file_path, SourceFileKind file_kind = SourceFileKind::Default);
};

}; // namespace choir

#endif // CHOIR_DRIVER_HH
