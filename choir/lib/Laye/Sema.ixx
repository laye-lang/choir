module;

#include <choir/macros.hh>
#include <llvm/ADT/SmallVector.h>
#include <memory>

export module choir.laye:sema;
import choir;
import choir.frontend;

export namespace choir::laye {
class TranslationUnit;
class SemaNode;
};

namespace choir::laye {

class TranslationUnit {
    CHOIR_IMMOVABLE(TranslationUnit);

public:
    using Ptr = std::unique_ptr<TranslationUnit>;

private:
    /// The source files that are part of this translation unit.
    /// This includes all Laye source files passed from the command line,
    /// as well as those found and included through Laye's import statements.
    llvm::SmallVector<const File*> _files{};

    explicit TranslationUnit();
public:
    static auto Create(String name) -> Ptr;
};

class SemaNode {
    CHOIR_IMMOVABLE(SemaNode);

public:
    enum struct Kind {
        Invalid,
    };

private:
    Kind _kind;

public:
    SemaNode(Kind kind) : _kind(kind) {}
};

}; // namespace choir::laye
