#ifndef CHOIR_FRONT_LAYE_SEMA_HH
#define CHOIR_FRONT_LAYE_SEMA_HH

#include <choir/macros.hh>
#include <choir/core.hh>
#include <llvm/ADT/SmallVector.h>
#include <memory>

namespace choir::laye {
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
    /// This includes all laye source files passed from the command line,
    /// as well as those found and included through laye's import statements.
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

#endif // #define CHOIR_FRONT_LAYE_SEMA_HH
