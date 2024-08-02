#include <choir/core.hh>
#include <choir/front/laye.hh>
#include <choir/macros.hh>
#include <vector>

using namespace choir;
using namespace choir::laye;

// ============================================================================
//  Syntax Module
// ============================================================================

SyntaxModule::~SyntaxModule() {
}

// ============================================================================
//  Syntax Token
// ============================================================================

auto SyntaxToken::KindToString(Kind kind) -> String {
    switch (kind) {
        default: return "Unknown??";
        case Kind::Invalid: return "Invalid";
        case Kind::EndOfFile: return "EndOfFile";
        case Kind::Tilde: return "Tilde";
        case Kind::Bang: return "Bang";
        case Kind::Percent: return "Percent";
        case Kind::Ampersand: return "Ampersand";
        case Kind::Star: return "Star";
        case Kind::OpenParen: return "OpenParen";
        case Kind::CloseParen: return "CloseParen";
        case Kind::Minus: return "Minus";
        case Kind::Equal: return "Equal";
        case Kind::Plus: return "Plus";
        case Kind::OpenBracket: return "OpenBracket";
        case Kind::CloseBracket: return "CloseBracket";
        case Kind::OpenBrace: return "OpenBrace";
        case Kind::CloseBrace: return "CloseBrace";
        case Kind::Pipe: return "Pipe";
        case Kind::SemiColon: return "SemiColon";
        case Kind::Colon: return "Colon";
        case Kind::Comma: return "Comma";
        case Kind::Less: return "Less";
        case Kind::Greater: return "Greater";
        case Kind::Dot: return "Dot";
        case Kind::Slash: return "Slash";
        case Kind::Question: return "Question";
        case Kind::Identifier: return "Identifier";
        case Kind::Global: return "Global";
        case Kind::LiteralInteger: return "LiteralInteger";
        case Kind::LiteralFloat: return "LiteralFloat";
        case Kind::LiteralString: return "LiteralString";
        case Kind::LiteralRune: return "LiteralRune";
        case Kind::SlashColon: return "SlashColon";
        case Kind::PercentColon: return "PercentColon";
        case Kind::QuestionQuestion: return "QuestionQuestion";
        case Kind::QuestionQuestionEqual: return "QuestionQuestionEqual";
        case Kind::PlusPlus: return "PlusPlus";
        case Kind::PlusPercent: return "PlusPercent";
        case Kind::PlusPipe: return "PlusPipe";
        case Kind::MinusMinus: return "MinusMinus";
        case Kind::MinusPercent: return "MinusPercent";
        case Kind::MinusPipe: return "MinusPipe";
        case Kind::Caret: return "Caret";
        case Kind::LessColon: return "LessColon";
        case Kind::LessLess: return "LessLess";
        case Kind::ColonGreater: return "ColonGreater";
        case Kind::GreaterGreater: return "GreaterGreater";
        case Kind::EqualEqual: return "EqualEqual";
        case Kind::BangEqual: return "BangEqual";
        case Kind::PlusEqual: return "PlusEqual";
        case Kind::PlusPercentEqual: return "PlusPercentEqual";
        case Kind::PlusPipeEqual: return "PlusPipeEqual";
        case Kind::MinusEqual: return "MinusEqual";
        case Kind::MinusPercentEqual: return "MinusPercentEqual";
        case Kind::MinusPipeEqual: return "MinusPipeEqual";
        case Kind::SlashEqual: return "SlashEqual";
        case Kind::SlashColonEqual: return "SlashColonEqual";
        case Kind::StarEqual: return "StarEqual";
        case Kind::CaretEqual: return "CaretEqual";
        case Kind::PercentEqual: return "PercentEqual";
        case Kind::PercentColonEqual: return "PercentColonEqual";
        case Kind::LessEqual: return "LessEqual";
        case Kind::LessEqualColon: return "LessEqualColon";
        case Kind::GreaterEqual: return "GreaterEqual";
        case Kind::ColonGreaterEqual: return "ColonGreaterEqual";
        case Kind::AmpersandEqual: return "AmpersandEqual";
        case Kind::PipeEqual: return "PipeEqual";
        case Kind::TildeEqual: return "TildeEqual";
        case Kind::LessLessEqual: return "LessLessEqual";
        case Kind::GreaterGreaterEqual: return "GreaterGreaterEqual";
        case Kind::EqualGreater: return "EqualGreater";
        case Kind::LessMinus: return "LessMinus";
        case Kind::ColonColon: return "ColonColon";
        case Kind::Strict: return "Strict";
        case Kind::From: return "From";
        case Kind::As: return "As";
        case Kind::Var: return "Var";
        case Kind::Void: return "Void";
        case Kind::NoReturn: return "NoReturn";
        case Kind::Bool: return "Bool";
        case Kind::BoolSized: return "BoolSized";
        case Kind::Int: return "Int";
        case Kind::IntSized: return "IntSized";
        case Kind::FloatSized: return "FloatSized";
        case Kind::True: return "True";
        case Kind::False: return "False";
        case Kind::Nil: return "Nil";
        case Kind::If: return "If";
        case Kind::Else: return "Else";
        case Kind::For: return "For";
        case Kind::While: return "While";
        case Kind::Do: return "Do";
        case Kind::Switch: return "Switch";
        case Kind::Case: return "Case";
        case Kind::Default: return "Default";
        case Kind::Return: return "Return";
        case Kind::Break: return "Break";
        case Kind::Continue: return "Continue";
        case Kind::Fallthrough: return "Fallthrough";
        case Kind::Yield: return "Yield";
        case Kind::Unreachable: return "Unreachable";
        case Kind::Defer: return "Defer";
        case Kind::Discard: return "Discard";
        case Kind::Goto: return "Goto";
        case Kind::Xyzzy: return "Xyzzy";
        case Kind::Assert: return "Assert";
        case Kind::Try: return "Try";
        case Kind::Catch: return "Catch";
        case Kind::Struct: return "Struct";
        case Kind::Variant: return "Variant";
        case Kind::Enum: return "Enum";
        case Kind::Template: return "Template";
        case Kind::Alias: return "Alias";
        case Kind::Test: return "Test";
        case Kind::Import: return "Import";
        case Kind::Export: return "Export";
        case Kind::Operator: return "Operator";
        case Kind::Mut: return "Mut";
        case Kind::New: return "New";
        case Kind::Delete: return "Delete";
        case Kind::Cast: return "Cast";
        case Kind::Is: return "Is";
        case Kind::Sizeof: return "Sizeof";
        case Kind::Alignof: return "Alignof";
        case Kind::Offsetof: return "Offsetof";
        case Kind::Not: return "Not";
        case Kind::And: return "And";
        case Kind::Or: return "Or";
        case Kind::Xor: return "Xor";
        case Kind::Varargs: return "Varargs";
        case Kind::Const: return "Const";
        case Kind::Foreign: return "Foreign";
        case Kind::Inline: return "Inline";
        case Kind::Callconv: return "Callconv";
        case Kind::Pure: return "Pure";
        case Kind::Discardable: return "Discardable";
    }
}

// ============================================================================
//  Syntax Nodes
// ============================================================================

void* SyntaxNode::operator new(usz sz, SyntaxModule* module) {
    auto ptr = ::operator new(sz);
    module->_nodes.push_back(static_cast<SyntaxNode*>(ptr));
    return ptr;
}

auto SyntaxNode::KindToString(Kind kind) -> String {
    switch (kind) {
        default: return "Unknown??";
        case Kind::ImportInvalidWithTokens: return "ImportInvalidWithTokens";
        case Kind::ImportPathSimple: return "ImportPathSimple";
        case Kind::ImportPathSimpleAliased: return "ImportPathSimpleAliased";
        case Kind::ImportNamedSimple: return "ImportNamedSimple";
        case Kind::ImportNamedSimpleAliased: return "ImportNamedSimpleAliased";
    }
}

// ============================================================================
//  Syntax Graph
// ============================================================================

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