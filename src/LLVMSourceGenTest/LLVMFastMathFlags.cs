/// This file was generated from 'llvm-c/Core.h' in the LLVM C API.

namespace Choir.LibLLVM;

public enum LLVMFastMathFlags
{
    FastMathAllowReassoc = 1,
    FastMathNoNaNs = 2,
    FastMathNoInfs = 4,
    FastMathNoSignedZeros = 8,
    FastMathAllowReciprocal = 16,
    FastMathAllowContract = 32,
    FastMathApproxFunc = 64,
    FastMathNone = 0,
    FastMathAll = 127,
}
