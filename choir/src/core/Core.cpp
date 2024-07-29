module;

#include <choir/macros.hh>

#include <print>
#include <stdexcept>
#include <source_location>

module choir;

void choir::utils::ReplaceAll(
    std::string& str,
    std::string_view from,
    std::string_view to
) {
    if (from.empty()) return;
    for (size_t i = 0; i = str.find(from, i), i != std::string::npos; i += to.size())
        str.replace(i, from.size(), to);
}

auto choir::utils::NumberWidth(size_t number, size_t base) -> size_t {
    return number == 0 ? 1 : size_t(std::log(number) / std::log(base) + 1);
}
