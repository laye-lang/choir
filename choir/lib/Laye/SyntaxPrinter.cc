#include <choir/core.hh>
#include <choir/front/laye.hh>
#include <choir/front/tree_printer.hh>
#include <choir/macros.hh>
#include <format>
#include <llvm/ADT/PointerUnion.h>
#include <llvm/ADT/SmallString.h>
#include <print>

using namespace choir;
using namespace choir::laye;

struct LayeSyntaxPrinter : front::TreePrinterBase<SyntaxTokenOrNodePointer> {
    using enum Color;

    Context& context;

    LayeSyntaxPrinter(Context& context, bool use_color) : TreePrinterBase(use_color), context(context){
        base_color = Color::Green;
    }

    void PrintHeader(const SyntaxToken* token) {
        std::print(
            "{}{} {}<{}> {}[{}{}{}]",
            C(base_color),
            laye::SyntaxToken::KindToString(token->kind),
            C(location_color),
            token->location.pos,
            C(Reset),
            C(base_color),
            token->spelling(context),
            C(Reset)
        );
    }

    void PrintHeader(const SyntaxNode* node) {
        std::print(
            "{}{} {}<{}>",
            C(base_color),
            SyntaxNode::KindToString(node->kind()),
            C(location_color),
            node->location().pos
        );
    }

    void Print(const SyntaxToken* token) {
        using Tk = SyntaxToken::Kind;
        PrintHeader(token);

        switch (token->kind) {
            default: break;

            case Tk::Identifier: {
                std::print(" {}{}", C(name_color), token->text);
            } break;

            case Tk::LiteralString: {
                std::print(" {}\"{}\"", C(value_color), token->text);
            } break;

            case Tk::LiteralRune: {
                i32 codepoint = i32(token->integer_value.getSExtValue());
                if (codepoint < 256) {
                    std::print(" {}\'{}\'", C(value_color), char(codepoint));
                } else {
                    std::print(" {}\'\\U{:X}\'", C(value_color), codepoint);
                }
            } break;

            case Tk::LiteralInteger: {
                llvm::SmallString<16> buf{};
                token->integer_value.toStringUnsigned(buf);
                std::print(" {}{}", C(value_color), StringRef{buf.data(), buf.size()});
            } break;

            case Tk::LiteralFloat: {
                llvm::SmallString<16> buf{};
                token->float_value.toString(buf);
                std::print(" {}{}", C(value_color), StringRef{buf.data(), buf.size()});
            } break;
        }

        std::print("{}\n", C(Reset));
    }

    void Print(const SyntaxNode* node) {
        PrintHeader(node);
        
        using K = SyntaxNode::Kind;
        switch (node->kind()) {
            default: break;

            case K::ImportInvalidWithTokens: {
            } break;

            case K::ImportPathSimple: {
                auto* n = cast<SyntaxImportPathSimple>(node);
                std::print(" {}\"{}\"", C(value_color), n->path_text());
            } break;

            case K::ImportPathSimpleAliased: {
                auto* n = cast<SyntaxImportPathSimpleAliased>(node);
                std::print(" {}\"{}\" {}as {}{}", C(value_color), n->path_text(), C(keyword_color), C(name_color), n->alias_name());
            } break;

            case K::ImportNamedSimple: break;
            case K::ImportNamedSimpleAliased: break;
        }

        std::print("{}\n", C(Reset));
        PrintChildren(node->children());
    }

    void Print(SyntaxTokenOrNodePointer pair) {
        if (auto token = pair.dyn_cast<const SyntaxToken*>()) {
            Print(token);
        } else {
            Print(pair.get<const SyntaxNode*>());
        }
    }

    void print_tokens(const SyntaxModule* module) {
        for (const auto& token : module->tokens()) {
            Print(&token);
        }
    }

    void print_tree(const SyntaxModule* module) {
        for (const auto& top_level_node : module->top_level_declarations()) {
            Print(top_level_node);
        }
    }
};

void SyntaxModule::print_tokens(bool use_color) {
    LayeSyntaxPrinter{context(), use_color}.print_tokens(this);
}

void SyntaxModule::print_tree(bool use_color) {
    LayeSyntaxPrinter{context(), use_color}.print_tree(this);
}
