#ifndef CHOIR_API_DRIVER_DRIVER_HH
#define CHOIR_API_DRIVER_DRIVER_HH

#include <choir/core.hh>
#include <choir/driver/Enums.hh>
#include <choir/driver/Forward.hh>
#include <string_view>

namespace choir {

class Driver {
    CHOIR_DECLARE_HIDDEN_IMPL(Driver);

public:
    explicit Driver();

    void add_file(std::string_view file_path, SourceFileKind file_kind = SourceFileKind::Default);
};

}; // namespace choir

#endif // !CHOIR_API_DRIVER_DRIVER_HH
