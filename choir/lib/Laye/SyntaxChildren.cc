#include <choir/core.hh>
#include <choir/front/laye.hh>
#include <vector>

using namespace choir;
using namespace choir::laye;

auto SyntaxNode::children() const -> std::vector<SyntaxTokenOrNodePointer> {
    using K = SyntaxNode::Kind;

    std::vector<SyntaxTokenOrNodePointer> children{};
    switch (kind()) {
        default: break;

        case K::ImportInvalidWithTokens: {
            auto n = cast<SyntaxImportInvalidWithTokens>(this);
            children.push_back(n->token_import());
            for (const auto token : n->consumed_tokens()) {
                children.push_back(token);
            }
            children.push_back(n->token_semi());
        } break;
    }

    return children;
}
