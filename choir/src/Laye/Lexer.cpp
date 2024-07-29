module;

#include <choir/macros.hh>

module choir.laye;
import choir;
import choir.frontend;

using namespace choir;
using namespace choir::laye;

auto Lexer::ReadTokens(const File& source_file) -> std::vector<SyntaxToken> {
    std::vector<SyntaxToken> tokens{};

    CHOIR_TODO("Lexer::ReadTokens(\"{}\") -- actually lex the file", source_file.path());
    return std::move(tokens);
}
