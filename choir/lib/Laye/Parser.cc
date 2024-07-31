#include <choir/macros.hh>
#include <choir/front/laye.hh>
#include <filesystem>
#include <vector>

using namespace choir;
using namespace choir::laye;

auto Parser::Parse(const File& source_file) -> SyntaxModule::Ptr {
    Parser parser{source_file};

    CHOIR_TODO("Parser::Parse(\"{}\") -- actually parse the tokens", source_file.path());
    return std::move(parser._module);
}

void SyntaxGraph::add_file(File::Path source_file_path) {
    CHOIR_ASSERT(source_file_path == canonical(source_file_path), "the input file path was not canonicalized first");
    const auto& source_file = context().get_file(source_file_path);

    std::unique_ptr<SyntaxModule> syntax_module{};
    if (lex_only()) {
        syntax_module = std::make_unique<SyntaxModule>(source_file);
        Lexer::ReadTokens(*syntax_module);
    }
    else syntax_module = Parser::Parse(source_file);

    _module_graph.add_node(syntax_module.get());
    _modules.push_back(std::move(syntax_module));

    // TODO(local): when we add imports to the module, build the graph here
}
