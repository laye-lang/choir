## ============================================================================
##  Global CMake Variables.
## ============================================================================
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

## ============================================================================
##  Global compiler options.
## ============================================================================
## Turn on diagnostics colours.
if (CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
    add_compile_options(-fdiagnostics-color=always)
elseif (CMAKE_CXX_COMPILER_ID STREQUAL "Clang")
    add_compile_options(-fcolor-diagnostics)
endif()

## Use mold as the default linker, if it exists.
if (CMAKE_CXX_COMPILER_ID STREQUAL "GNU" OR CMAKE_CXX_COMPILER_ID STREQUAL "Clang")
    find_program(MOLD_LINKER "mold")
    if (MOLD_LINKER)
        add_link_options(-fuse-ld=mold -Wl,--color-diagnostics)
    endif()
endif()

## ============================================================================
##  Compiler options.
## ============================================================================
add_library(options INTERFACE)

add_compile_options(-fdeclspec -fms-extensions -Wno-ctad-maybe-unsupported)

## Flags for Clang and GCC.
if (CMAKE_CXX_COMPILER_ID STREQUAL "GNU" OR CMAKE_CXX_COMPILER_ID STREQUAL "Clang")
    target_compile_options(options INTERFACE
        -fno-exceptions
        -fno-rtti
        -fdeclspec

        ## Warnings.
        -Wall -Wextra     # Enable ‘all’ warnings.
        -Wundef           # Invalid #undef or undefined macro in #if.
        -Wcast-align      # Casting that changes alignment.
        -Wconversion      # Implicit conversions.
        -Wsign-conversion # Implicit sign conversions.
        -Wformat=2        # Stricter format checking.

        ## Disabled warnings.
        -Wno-unused-function
        -Wno-unused-local-typedefs

        ## NULL Errors.
        -Werror=nonnull # Passing NULL to nonnull parameter.

        ## Memory Errors.
        -Werror=address              # Suspicious use of addresses.
        -Werror=init-self            # Initialization of a variable with itself.
        -Werror=uninitialized

        ## Return type.
        -Werror=return-type
        #-Wmissing-noreturn

        ## C/C++.
        -Werror=implicit-fallthrough
        -Werror=missing-include-dirs # User-specified include dir does not exist.
        -Werror=pointer-arith        # Disallow void* and function pointer arithmetic.
        -Werror=string-compare       # Nonsensical string comparisons.
        -Werror=switch               # Missing switch cases.
        # -Werror=switch-enum          # Switch on enum (even if there is a default case).
        -Werror=write-strings        # Strings in C should be const char*.

        ## C++.
        -Werror=missing-field-initializers
        -Werror=non-virtual-dtor
        -Werror=pessimizing-move

        ## This is typically nonsense if you’re not an idiot.
        -Wno-ctad-maybe-unsupported

        ## Clang allows structured bindings in conditions as an extension.
        -Wno-binding-in-condition

        ## This is nonsense in C++26.
        -Wno-trigraphs
    )
endif()

## Additional flags for GCC.
if (CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
    target_compile_options(options INTERFACE
        -Wlogical-op      # Duplicate or unintended logical operators.
        -Werror=invalid-memory-model # For atomics.
        -Werror=maybe-uninitialized
        -Werror=missing-requires
        -Werror=return-local-addr
    )
endif()

## Additional flags for Clang.
if (CMAKE_CXX_COMPILER_ID STREQUAL "Clang")
    target_compile_options(options INTERFACE
        -Werror=dangling
        -Werror=return-stack-address
    )
endif()

## Flags for MSVC.
if (CMAKE_CXX_COMPILER_ID STREQUAL "MSVC")
    target_compile_options(options INTERFACE
        /W4 # Enable ‘all’ warnings.

        # Allow unnamed structs/unions.
        /wd4201
    )
endif()

## On Windows, don’t suggest the _s nonsense functions.
if (WIN32)
    target_compile_definitions(options INTERFACE
        _CRT_SECURE_NO_WARNINGS
        _CRT_SECURE_NO_WARNINGS_GLOBALS
        _CRT_NONSTDC_NO_WARNINGS
    )
endif()

## Enable ASAN if requested.
if (ENABLE_ASAN)
    target_compile_options(options INTERFACE -fsanitize=address)
    target_link_options(options INTERFACE -fsanitize=address)
endif()

## Debug/Release flags.
if (NOT MSVC)
#[[
    target_compile_definitions(options INTERFACE
        $<$<CONFIG:DEBUG>:_GLIBCXX_DEBUG>
    )
]]
    target_compile_options(options INTERFACE
        $<$<CONFIG:DEBUG>:-O0 -g3 -glldb -funwind-tables>
        $<$<CONFIG:RELEASE>:-O3 -march=native>
    )
    target_link_options(options INTERFACE
        $<$<CONFIG:DEBUG>:-O0 -g3 -glldb -Wl,-export-dynamic -funwind-tables>
        $<$<CONFIG:RELEASE>:-O3 -march=native>
    )
else()
    target_compile_options(options INTERFACE
        $<$<CONFIG:DEBUG>:/Od>
        $<$<CONFIG:RELEASE>:/O2>
    )
endif()

## ============================================================================
##  Submodules and include dirs.
## ============================================================================
## Find clopts.
include(CheckIncludeFileCXX)
check_include_file_cxx(clopts.hh HAVE_CLOPTS_HH)
if (NOT HAVE_CLOPTS_HH)
    include(FetchContent)
	message(STATUS "Clopts not found, downloading ...")
	FetchContent_Declare(clopts
		GIT_REPOSITORY https://github.com/Sirraide/clopts.git
		GIT_TAG v2.0.2
		SOURCE_DIR "${CMAKE_CURRENT_BINARY_DIR}/libs/clopts"
    )
	FetchContent_MakeAvailable(clopts)
    target_include_directories(options INTERFACE "${CMAKE_CURRENT_BINARY_DIR}/libs/clopts/include")
endif()

## ============================================================================
##  LLVM.
## ============================================================================
# set(SOURCE_LLVM_BUILD_DIR "${PROJECT_SOURCE_DIR}/../llvm-project/build_win32_ninja/")
# set(CHOIR_LLVM_CMAKE_DIR "${CHOIR_LLVM_BUILD_DIR}/lib/cmake")
set(CHOIR_LLVM_BUILD_DIR "${PROJECT_SOURCE_DIR}/../llvm-project/build_win32/Debug/")
set(CHOIR_LLVM_CMAKE_DIR "${CHOIR_LLVM_BUILD_DIR}/../lib/cmake")
set(CHOIR_LLVM_BIN_DIR "${CHOIR_LLVM_BUILD_DIR}/bin")
set(LLVM_DIR "${CHOIR_LLVM_CMAKE_DIR}/llvm")
set(Clang_DIR "${CHOIR_LLVM_CMAKE_DIR}/clang")

## Set these or be prepared for horrible linker errors.
##
## As of now (LLVM version 17.0.0), AddLLVM.cmake adds `-Wl,-rpath-link,${LLVM_LIBRARY_OUTPUT_INTDIR}`
## to the linker command line. The problem with this is that, if LLVM_LIBRARY_OUTPUT_INTDIR is not
## defined, which, by default, it isn’t, you end up with `-Wl,-rpath-link,`, as a result of which the
## compiler driver just throws an `-rpath-link` with no argument in the middle of the linker command
## line, which causes whatever file or option after it to be be swallowed.
set(LLVM_RUNTIME_OUTPUT_INTDIR "${CMAKE_BINARY_DIR}/${CMAKE_CFG_INTDIR}/bin")
set(LLVM_LIBRARY_OUTPUT_INTDIR "${CMAKE_BINARY_DIR}/${CMAKE_CFG_INTDIR}/lib${LLVM_LIBDIR_SUFFIX}")

find_package(LLVM REQUIRED CONFIG)
find_package(Clang REQUIRED CONFIG)

list(APPEND CMAKE_MODULE_PATH "${LLVM_CMAKE_DIR}")
list(APPEND CMAKE_MODULE_PATH "${CLANG_CMAKE_DIR}")

include(AddLLVM)
include(AddClang)
include(HandleLLVMOptions)

if (NOT DEFINED CHOIR_CLANG_EXE_PATH)
    set(CHOIR_CLANG_EXE_PATH "${CHOIR_LLVM_BIN_DIR}/clang")
endif()

## FIXME: This is a hack.
## Use llvm-config to get all LLVM libraries.
## TODO(local): Select only the libraries I want soon
# execute_process(
#     COMMAND "${LLVM_TOOLS_BINARY_DIR}/llvm-config" --libs
#     OUTPUT_VARIABLE CHOIR_LLVM_LIBS
#     OUTPUT_STRIP_TRAILING_WHITESPACE
#     COMMAND_ERROR_IS_FATAL ANY
# )

## Add the LLVM include directories.
include_directories(SYSTEM
    ${LLVM_INCLUDE_DIRS}
    ${CLANG_INCLUDE_DIRS}
)

## Add the LLVM definitions.
add_definitions(
    ${LLVM_DEFINITIONS}
    ${CLANG_DEFINITIONS}
)

#separate_arguments(CHOIR_LLVM_LIBS)
target_link_libraries(options INTERFACE
#    ${CHOIR_LLVM_LIBS}
    clangBasic clangDriver clangFrontend
    LLVMCore LLVMSupport LLVMTarget
    LLVMX86AsmParser LLVMX86CodeGen LLVMX86Desc LLVMX86Info
)

file(GLOB_RECURSE _driver_sources src/*.cc lib/*.cc)

## Add the executable.
add_clang_executable(choir ${_driver_sources})
target_include_directories(choir PUBLIC "${PROJECT_SOURCE_DIR}/choir/include")
target_include_directories(choir PRIVATE "${PROJECT_SOURCE_DIR}/choir/lib")
target_compile_definitions(choir PRIVATE "CHOIR_CLANG_EXE_PATH=\"${CHOIR_CLANG_EXE_PATH}\"")

## Link everything together.
target_link_libraries(choir PRIVATE options)
