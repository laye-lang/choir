module;

#include <choir/macros.hh>
#include <vector>

module choir.laye;
import choir;
import choir.frontend;

using namespace choir;
using namespace choir::laye;

auto Parser::Parse(const File& source_file) -> SyntaxModule::Ptr {
    Parser parser{source_file};

    CHOIR_TODO("Parser::Parse(\"{}\") -- actually parse the tokens", source_file.path());
    return std::move(parser._module);
}

void SyntaxGraph::add_file(File::Path source_file_path) {
    const auto& source_file = context().get_file(source_file_path);
    auto syntax_module = Parser::Parse(source_file);

    CHOIR_TODO("SyntaxGraph::add_file(\"{}\") -- what to do after parse?", source_file_path);
}
