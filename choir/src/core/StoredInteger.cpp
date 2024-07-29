module;

#include <choir/macros.hh>
#include <llvm/ADT/APInt.h>
#include <llvm/ADT/StringExtras.h>
#include <print>

module choir;
using namespace choir;

auto IntegerStorage::store_int(llvm::APInt integer) -> StoredInteger {
    StoredInteger si;
    si.bits = integer.getBitWidth();

    if (integer.getBitWidth() <= 64) {
        si.data = integer.getZExtValue();
        return si;
    }

    si.data = saved.size();
    saved.push_back(std::move(integer));
    return si;
}

auto StoredInteger::inline_value() const -> std::optional<int64_t> {
    if (is_inline()) return int64_t(data);
    return std::nullopt;
}

auto StoredInteger::str(const IntegerStorage* storage, bool is_signed) const -> std::string {
    if (is_inline()) return std::to_string(data);
    if (storage) return llvm::toString(storage->saved[size_t(data)], 10, is_signed);
    return "<huge value>";
}

auto StoredInteger::value(const IntegerStorage& storage) const -> llvm::APInt {
    if (is_inline()) return llvm::APInt(uint32_t(bits), data);
    return storage.saved[size_t(data)];
}
