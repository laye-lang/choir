module;

#include <choir/macros.hh>
#include <string_view>

export module choir.driver;

export namespace choir {

enum struct SourceFileKind {
    Default,
    Laye,
    C,
    CXX,
};

struct DriverOptions {
    bool colors;
    uint32_t error_limit;
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
