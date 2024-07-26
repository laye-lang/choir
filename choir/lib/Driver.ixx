module;

#include <choir/macros.hh>
#include <string_view>

export module driver;

export namespace choir {

enum struct SourceFileKind {
    Default,
    Laye,
    C,
    CXX,
};

struct DriverOptions {
};

class Driver {
    CHOIR_DECLARE_HIDDEN_IMPL(Driver);

    DriverOptions _options;

public:
    Driver(DriverOptions options);

    void add_file(std::string_view file_path, SourceFileKind file_kind = SourceFileKind::Default);
};

}; // namespace choir
