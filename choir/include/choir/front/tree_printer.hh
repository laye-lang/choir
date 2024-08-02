#ifndef CHOIR_FRONT_ASTDUMP_HH
#define CHOIR_FRONT_ASTDUMP_HH

#include <choir/core.hh>
#include <llvm/ADT/ArrayRef.h>
#include <llvm/ADT/StringRef.h>
#include <llvm/ADT/SmallString.h>
#include <print>

namespace choir::front {

template <typename NodeType>
class TreePrinterBase {
protected:
    llvm::SmallString<128> leading;
    Colors C;

    Color base_color{Color::White};
    Color name_color{Color::Reset};
    Color attrib_color{Color::Cyan};
    Color location_color{Color::Magenta};
    Color value_color{Color::Yellow};
    Color keyword_color{Color::Blue};

    explicit TreePrinterBase(bool use_color) : C{use_color} {}

    template <typename Node = NodeType>
    void PrintChildren(this auto&& This, std::type_identity_t<llvm::ArrayRef<Node>> children) {
        using enum choir::Color;
        if (children.empty()) return;
        auto& leading = This.leading;
        auto C = This.C;

        // Print all but the last.
        const auto size = leading.size();
        leading += "│ ";
        const auto current = llvm::StringRef{leading}.take_front(size);
        for (auto c : children.drop_back(1)) {
            std::print("{}{}├─", C(This.base_color), current);
            This.Print(c);
        }

        // Print the preheader of the last.
        leading.resize(size);
        std::print("{}{}└─", C(This.base_color), llvm::StringRef{leading});

        // Print the last one.
        leading += "  ";
        This.Print(children.back());

        // And reset the leading text.
        leading.resize(size);
    }
};

};

#endif // CHOIR_FRONT_ASTDUMP_HH
