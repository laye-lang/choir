## ============================================================================
##  Global CMake Variables.
## ============================================================================
set(CMAKE_CXX_STANDARD 23)
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

## Flags for Clang and GCC.
if (CMAKE_CXX_COMPILER_ID STREQUAL "GNU" OR CMAKE_CXX_COMPILER_ID STREQUAL "Clang")
    add_compile_options(-fdeclspec -fms-extensions -Wno-ctad-maybe-unsupported)

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
include(FetchContent)

add_definitions(-DLIBASSERT_STATIC_DEFINE)
FetchContent_Declare(
    libassert
    GIT_REPOSITORY https://github.com/jeremy-rifkin/libassert.git
    GIT_TAG        v2.1.0
)

FetchContent_Declare(
    clopts
    GIT_REPOSITORY https://github.com/Sirraide/clopts.git
    GIT_TAG        master
    SOURCE_DIR     "${CMAKE_CURRENT_BINARY_DIR}/clopts"
)

FetchContent_MakeAvailable(libassert clopts)

target_include_directories(options INTERFACE "${cpptrace_SOURCE_DIR}/include")
target_include_directories(options INTERFACE "${libassert_SOURCE_DIR}/include")
target_include_directories(options INTERFACE "${clopts_SOURCE_DIR}/include")
target_include_directories(options INTERFACE "${PROJECT_SOURCE_DIR}/choir/include")

target_link_libraries(options INTERFACE libassert::assert)

## ============================================================================
##  LLVM.
## ============================================================================
# set(CHOIR_LLVM_REDIST_DIR "${PROJECT_SOURCE_DIR}/../llvm-project/build_win32/Debug/")
if(NOT DEFINED CHOIR_LLVM_REDIST_DIR)
    message(FATAL_ERROR "Must provide -DCHOIR_LLVM_REDIST_DIR=<Path to LLVM redistributable binaries>")
endif()

# set(CHOIR_LLVM_CMAKE_DIR "${CHOIR_LLVM_REDIST_DIR}/../lib/cmake")
if(NOT DEFINED CHOIR_LLVM_CMAKE_DIR)
    set(CHOIR_LLVM_CMAKE_DIR "${CHOIR_LLVM_REDIST_DIR}/../lib/cmake/")
    if(NOT EXISTS "${CHOIR_LLVM_CMAKE_DIR}" OR NOT IS_DIRECTORY "${CHOIR_LLVM_CMAKE_DIR}")
        message(FATAL_ERROR "Could not find LLVM CMake directory at '${CHOIR_LLVM_CMAKE_DIR}'. Must provide -DCHOIR_LLVM_CMAKE_DIR=<Path to LLVM CMake files>")
    endif()
endif()

set(CHOIR_LLVM_BIN_DIR "${CHOIR_LLVM_REDIST_DIR}/bin")
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
    if (NOT WIN32)
        set(CHOIR_CLANG_EXE_PATH "${CHOIR_LLVM_BIN_DIR}/clang")
    else()
        set(CHOIR_CLANG_EXE_PATH "${CHOIR_LLVM_BIN_DIR}/clang.exe")
    endif()
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
    clangBasic clangDriver clangFrontend clangTooling
    LLVMCore LLVMSupport LLVMTarget
    LLVMX86AsmParser LLVMX86CodeGen LLVMX86Desc LLVMX86Info
)

file(GLOB_RECURSE _sources_driver src/*.cpp)
file(GLOB_RECURSE _modules_driver lib/*.ixx lib/*.cpp)

## Add the executable.
add_clang_executable(choir ${_sources_driver})
target_sources(choir PUBLIC FILE_SET CXX_MODULES FILES ${_modules_driver})
target_compile_definitions(choir PRIVATE "CHOIR_CLANG_EXE_PATH=\"${CHOIR_CLANG_EXE_PATH}\"")

## Link everything together.
target_link_libraries(choir PRIVATE options)

if (NOT WIN32)
    set(CHOIR_CHOIR_EXE_NAME "choir")
else()
    set(CHOIR_CHOIR_EXE_NAME "choir.exe")
endif()

add_custom_target(choir_exe_copy ALL
    COMMAND "${CMAKE_COMMAND}" -E copy "$<TARGET_FILE:choir>" "${PROJECT_SOURCE_DIR}/out/${CHOIR_CHOIR_EXE_NAME}"
    DEPENDS choir
    COMMENT "Copy $<TARGET_FILE:choir> executable to ${PROJECT_SOURCE_DIR}/out/${CHOIR_CHOIR_EXE_NAME}"
)
