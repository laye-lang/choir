/// This file was generated from 'llvm-c/Core.h' in the LLVM C API.

namespace Choir.LibLLVM;

public enum LLVMAtomicOrdering
{
    AtomicOrderingNotAtomic = 0,
    AtomicOrderingUnordered = 1,
    AtomicOrderingMonotonic = 2,
    AtomicOrderingAcquire = 4,
    AtomicOrderingRelease = 5,
    AtomicOrderingAcquireRelease = 6,
    AtomicOrderingSequentiallyConsistent = 7,
}
