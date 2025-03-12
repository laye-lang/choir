/// This file was generated from 'llvm-c/Core.h' in the LLVM C API.

namespace Choir.LibLLVM;

public enum LLVMThreadLocalMode
{
    NotThreadLocal = 0,
    GeneralDynamicTLSModel = 1,
    LocalDynamicTLSModel = 2,
    InitialExecTLSModel = 3,
    LocalExecTLSModel = 4,
}
