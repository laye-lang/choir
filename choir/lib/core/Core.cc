#include <choir/macros.hh>
#include <choir/core.hh>
#include <print>
#include <string>
#include <stdexcept>
#include <source_location>

void choir::utils::ReplaceAll(
    std::string& str,
    std::string_view from,
    std::string_view to
) {
    if (from.empty()) return;
    for (usz i = 0; i = str.find(from, i), i != std::string::npos; i += to.size())
        str.replace(i, from.size(), to);
}

auto choir::utils::NumberWidth(usz number, usz base) -> usz {
    return number == 0 ? 1 : usz(std::log(number) / std::log(base) + 1);
}

auto operator+=(std::string& s, choir::String str) -> std::string& {
    s += str.value();
    return s;
}
