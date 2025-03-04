#include "config.h"

#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

#define ODIR "out"

#if defined(_WIN32)
#    define LIBCHOIR_STATIC_LIBRARY_FILE "libchoir"
#    define LIBPINK_STATIC_LIBRARY_FILE  "libpink"
#    define LIBLAYE_STATIC_LIBRARY_FILE  "liblaye"
#    define LIBPENG_STATIC_LIBRARY_FILE  "libpeng"
#    define LIBSCORE_STATIC_LIBRARY_FILE "libscore"
#else
#    define LIBCHOIR_STATIC_LIBRARY_FILE "choir"
#    define LIBPINK_STATIC_LIBRARY_FILE  "pink"
#    define LIBLAYE_STATIC_LIBRARY_FILE  "laye"
#    define LIBPENG_STATIC_LIBRARY_FILE  "peng"
#    define LIBSCORE_STATIC_LIBRARY_FILE "score"
#endif

#define LAYE_EXECUTABLE_FILE  "laye"
#define LAYEC_EXECUTABLE_FILE "layec"
#define PINK_EXECUTABLE_FILE  "pink"
#define PENG_EXECUTABLE_FILE  "peng"
#define SCORE_EXECUTABLE_FILE "score"

#if defined(NOBCONFIG_MISSING)
#    error No nob configuration has been specified. Please copy the relevant config file from the config directory for your platform and toolchain into the appropriate 'nob_config.<PLATFORM>.h' file.
#endif

#define NOB_IMPLEMENTATION
#include "nob.h"

typedef struct {
    const char* source_file;
    const char* object_file;
} source_paths;

static bool compile_object(const char* source_path, const char* object_path, const char* source_root);
static bool package_library(Nob_File_Paths object_files, const char* library_path);
static bool link_executable(Nob_File_Paths input_paths, const char* executable_path);
static bool build_object_files(const char* source_root, source_paths* sources, Nob_File_Paths* object_paths);

const char* identify_source_root();

static int build(int argc, char** argv);
static int clean(int argc, char** argv);
static int fuzz(int argc, char** argv);
static int test(int argc, char** argv);

static bool build_libchoir(const char* source_root, const char** libchoir_file);
static bool build_libpink(const char* source_root, const char** libpink_file);
static bool build_liblaye(const char* source_root, const char** liblaye_file);
static bool build_libpeng(const char* source_root, const char** libpeng_file);
static bool build_libscore(const char* source_root, const char** libscore_file);

static bool build_pink(const char* source_root);
static bool build_laye(const char* source_root);
static bool build_layec(const char* source_root);
static bool build_peng(const char* source_root);
static bool build_score(const char* source_root);

static source_paths libchoir_files[] = {
    {"lib/choir/alloc.c", ODIR "/choir-alloc.o"},
    {"lib/choir/arena.c", ODIR "/choir-arena.o"},
    {"lib/choir/context.c", ODIR "/choir-context.o"},
    {"lib/choir/diag.c", ODIR "/choir-diag.o"},
    {"lib/choir/gpalloc.c", ODIR "/choir-gpalloc.o"},
    {0},
};

static source_paths libpink_files[] = {
    {"lib/cc/std.c", ODIR "/cc-std.o"},
    {"lib/cc/token.c", ODIR "/cc-token.o"},
    {"lib/cc/lex.c", ODIR "/cc-lex.o"},
    {"lib/cc/parse.c", ODIR "/cc-parse.o"},
    {"lib/cc/sema.c", ODIR "/cc-sema.o"},
    {0},
};

static source_paths liblaye_files[] = {
    {"lib/laye/token.c", ODIR "/laye-token.o"},
    {"lib/laye/lex.c", ODIR "/laye-lex.o"},
    {"lib/laye/ast.c", ODIR "/laye-ast.o"},
    {"lib/laye/parse.c", ODIR "/laye-parse.o"},
    {"lib/laye/sema.c", ODIR "/laye-sema.o"},
    {0},
};

static source_paths libpeng_files[] = {
    {"lib/peng/token.c", ODIR "/peng-token.o"},
    {"lib/peng/lex.c", ODIR "/peng-lex.o"},
    {0},
};

static source_paths libscore_files[] = {
    {"lib/score/token.c", ODIR "/score-token.o"},
    {"lib/score/lex.c", ODIR "/score-lex.o"},
    {"lib/score/ast.c", ODIR "/score-ast.o"},
    {"lib/score/parse.c", ODIR "/score-parse.o"},
    {"lib/score/sema.c", ODIR "/score-sema.o"},
    {0},
};

static source_paths pink_files[] = {
    {"src/pink.c", ODIR "/pink.o"},
    {0},
};

static source_paths layec_files[] = {
    {"src/layec.c", ODIR "/layec.o"},
    {0},
};

static source_paths laye_files[] = {
    {"src/laye.c", ODIR "/laye.o"},
    {0},
};

static source_paths peng_files[] = {
    {"src/peng.c", ODIR "/peng.o"},
    {0},
};

static source_paths score_files[] = {
    {"src/score.c", ODIR "/score.o"},
    {0},
};

static const char* all_headers[] = {
    "include/choir/choir.h",
    "include/choir/config.h",
    "include/choir/macros.h",

    "include/cc/cc.h",
    "include/cc/std.inc",
    "include/cc/tokens.inc",

    "include/laye/laye.h",
    "include/laye/macros.h",
    "include/laye/tokens.inc",

    NULL,
};

int main(int argc, char** argv) {
    NOB_GO_REBUILD_URSELF(argc, argv);

    int result = 0;

    const char* program_name = nob_shift_args(&argc, &argv);

    if (argc > 0) {
        const char* command = argv[0];
        if (0 == strcmp(command, "clean")) {
            (void)nob_shift_args(&argc, &argv);
            nob_return_defer(clean(argc, argv));
        } else if (0 == strcmp(command, "fuzz")) {
            (void)nob_shift_args(&argc, &argv);
            nob_return_defer(fuzz(argc, argv));
        } else if (0 == strcmp(command, "test")) {
            (void)nob_shift_args(&argc, &argv);
            nob_return_defer(test(argc, argv));
        }
    }

    nob_return_defer(build(argc, argv));

defer:;
    return result;
}

static bool build_laye(const char* source_root) {
    bool result = true;

    const char* libchoir_file = NULL;
    if (!build_libchoir(source_root, &libchoir_file)) {
        nob_return_defer(false);
    }

    const char* libpink_file = NULL;
    if (!build_libpink(source_root, &libpink_file)) {
        nob_return_defer(false);
    }

    const char* liblaye_file = NULL;
    if (!build_liblaye(source_root, &liblaye_file)) {
        nob_return_defer(false);
    }

    Nob_File_Paths laye_input_paths = {0};
    if (!build_object_files(source_root, laye_files, &laye_input_paths)) {
        nob_return_defer(false);
    }

    nob_da_append(&laye_input_paths, libchoir_file);
    nob_da_append(&laye_input_paths, libpink_file);
    nob_da_append(&laye_input_paths, liblaye_file);
    const char* layefile = ODIR "/" LAYE_EXECUTABLE_FILE EXE_EXT;
    if (!link_executable(laye_input_paths, layefile)) {
        nob_return_defer(false);
    }

    if (0 == nob_needs_rebuild1(LAYE_EXECUTABLE_FILE EXE_EXT, layefile) && !nob_copy_file(layefile, LAYE_EXECUTABLE_FILE EXE_EXT)) {
        nob_return_defer(false);
    }

defer:;
    return result;
}

static bool build_layec(const char* source_root) {
    bool result = true;

    const char* libchoir_file = NULL;
    if (!build_libchoir(source_root, &libchoir_file)) {
        nob_return_defer(false);
    }

    const char* libpink_file = NULL;
    if (!build_libpink(source_root, &libpink_file)) {
        nob_return_defer(false);
    }

    const char* liblaye_file = NULL;
    if (!build_liblaye(source_root, &liblaye_file)) {
        nob_return_defer(false);
    }

    Nob_File_Paths layec_input_paths = {0};
    if (!build_object_files(source_root, layec_files, &layec_input_paths)) {
        nob_return_defer(false);
    }

    nob_da_append(&layec_input_paths, libchoir_file);
    nob_da_append(&layec_input_paths, libpink_file);
    nob_da_append(&layec_input_paths, liblaye_file);
    const char* layecfile = ODIR "/" LAYEC_EXECUTABLE_FILE EXE_EXT;
    if (!link_executable(layec_input_paths, layecfile)) {
        nob_return_defer(false);
    }

    if (0 == nob_needs_rebuild1(LAYEC_EXECUTABLE_FILE EXE_EXT, layecfile) && !nob_copy_file(layecfile, LAYEC_EXECUTABLE_FILE EXE_EXT)) {
        nob_return_defer(false);
    }

defer:;
    return result;
}

static bool build_peng(const char* source_root) {
    bool result = true;

    const char* libchoir_file = NULL;
    if (!build_libchoir(source_root, &libchoir_file)) {
        nob_return_defer(false);
    }

    const char* libpeng_file = NULL;
    if (!build_libpeng(source_root, &libpeng_file)) {
        nob_return_defer(false);
    }

    Nob_File_Paths peng_input_paths = {0};
    if (!build_object_files(source_root, peng_files, &peng_input_paths)) {
        nob_return_defer(false);
    }

    nob_da_append(&peng_input_paths, libchoir_file);
    nob_da_append(&peng_input_paths, libpeng_file);
    const char* pengfile = ODIR "/" PENG_EXECUTABLE_FILE EXE_EXT;
    if (!link_executable(peng_input_paths, pengfile)) {
        nob_return_defer(false);
    }

    if (0 == nob_needs_rebuild1(PENG_EXECUTABLE_FILE EXE_EXT, pengfile) && !nob_copy_file(pengfile, PENG_EXECUTABLE_FILE EXE_EXT)) {
        nob_return_defer(false);
    }

defer:;
    return result;
}

static bool build_pink(const char* source_root) {
    bool result = true;

    const char* libchoir_file = NULL;
    if (!build_libchoir(source_root, &libchoir_file)) {
        nob_return_defer(false);
    }

    const char* libpink_file = NULL;
    if (!build_libpink(source_root, &libpink_file)) {
        nob_return_defer(false);
    }

    Nob_File_Paths pink_input_paths = {0};
    if (!build_object_files(source_root, pink_files, &pink_input_paths)) {
        nob_return_defer(false);
    }

    nob_da_append(&pink_input_paths, libchoir_file);
    nob_da_append(&pink_input_paths, libpink_file);
    const char* pinkfile = ODIR "/" PINK_EXECUTABLE_FILE EXE_EXT;
    if (!link_executable(pink_input_paths, pinkfile)) {
        nob_return_defer(false);
    }

    if (0 == nob_needs_rebuild1(PINK_EXECUTABLE_FILE EXE_EXT, pinkfile) && !nob_copy_file(pinkfile, PINK_EXECUTABLE_FILE EXE_EXT)) {
        nob_return_defer(false);
    }

defer:;
    return result;
}

static bool build_score(const char* source_root) {
    bool result = true;

    const char* libchoir_file = NULL;
    if (!build_libchoir(source_root, &libchoir_file)) {
        nob_return_defer(false);
    }

    const char* libscore_file = NULL;
    if (!build_libscore(source_root, &libscore_file)) {
        nob_return_defer(false);
    }

    Nob_File_Paths score_input_paths = {0};
    if (!build_object_files(source_root, score_files, &score_input_paths)) {
        nob_return_defer(false);
    }

    nob_da_append(&score_input_paths, libchoir_file);
    nob_da_append(&score_input_paths, libscore_file);
    const char* scorefile = ODIR "/" SCORE_EXECUTABLE_FILE EXE_EXT;
    if (!link_executable(score_input_paths, scorefile)) {
        nob_return_defer(false);
    }

    if (0 == nob_needs_rebuild1(SCORE_EXECUTABLE_FILE EXE_EXT, scorefile) && !nob_copy_file(scorefile, SCORE_EXECUTABLE_FILE EXE_EXT)) {
        nob_return_defer(false);
    }

defer:;
    return result;
}

static int build(int argc, char** argv) {
    int result = 0;

    nob_log(NOB_INFO, "Building...");

    if (!nob_mkdir_if_not_exists(ODIR)) {
        nob_return_defer(1);
    }

    const char* source_root = identify_source_root();
    nob_log(NOB_INFO, "source_root = %s", source_root);

    bool success = true;
    success &= build_pink(source_root);
    success &= build_layec(source_root);
    success &= build_laye(source_root);
    success &= build_peng(source_root);
    success &= build_score(source_root);

    if (!success) {
        nob_return_defer(1);
    }

defer:;
    return result;
}

static int clean(int argc, char** argv) {
    int result = 0;

    nob_log(NOB_INFO, "Cleaning...");

    if (nob_file_exists("./laye")) remove("./laye");
    if (nob_file_exists("./laye.exe")) remove("./laye.exe");

    if (nob_file_exists("./layec")) remove("./layec");
    if (nob_file_exists("./layec.exe")) remove("./layec.exe");

    Nob_File_Paths outs = {0};
    nob_read_entire_dir(ODIR, &outs);
    for (size_t i = 2; i < outs.count; i++) {
        remove(nob_temp_sprintf(ODIR "/%s", outs.items[i]));
    }

    remove(ODIR);

    nob_log(NOB_INFO, "Cleaned!");

defer:;
    return result;
}

static int fuzz(int argc, char** argv) {
    int result = 0;

    nob_log(NOB_INFO, "Fuzzing...");

defer:;
    return result;
}

static int test(int argc, char** argv) {
    int result = 0;

    nob_log(NOB_INFO, "Testing...");

defer:;
    return result;
}

const char* identify_source_root() {
    const char* source_root = ".";
    while (!nob_file_exists(nob_temp_sprintf("%s/choir", source_root))) {
        source_root = nob_temp_sprintf("%s/..", source_root);
    }

    source_root = nob_temp_sprintf("%s/choir", source_root);

    return source_root;
}

static bool build_libchoir(const char* source_root, const char** libchoir_file) {
    bool result = true;

    Nob_File_Paths libchoir_object_paths = {0};
    if (!build_object_files(source_root, libchoir_files, &libchoir_object_paths)) {
        nob_return_defer(false);
    }

    *libchoir_file = ODIR "/" LIB_PREFIX LIBCHOIR_STATIC_LIBRARY_FILE LIB_EXT;
    if (!package_library(libchoir_object_paths, *libchoir_file)) {
        nob_return_defer(false);
    }

defer:;
    return result;
}

static bool build_libpink(const char* source_root, const char** libpink_file) {
    bool result = true;

    Nob_File_Paths libpink_object_paths = {0};
    if (!build_object_files(source_root, libpink_files, &libpink_object_paths)) {
        nob_return_defer(1);
    }

    *libpink_file = ODIR "/" LIB_PREFIX LIBPINK_STATIC_LIBRARY_FILE LIB_EXT;
    if (!package_library(libpink_object_paths, *libpink_file)) {
        nob_return_defer(1);
    }

defer:;
    return result;
}

static bool build_liblaye(const char* source_root, const char** liblaye_file) {
    bool result = true;

    Nob_File_Paths liblaye_object_paths = {0};
    if (!build_object_files(source_root, liblaye_files, &liblaye_object_paths)) {
        nob_return_defer(1);
    }

    *liblaye_file = ODIR "/" LIB_PREFIX LIBLAYE_STATIC_LIBRARY_FILE LIB_EXT;
    if (!package_library(liblaye_object_paths, *liblaye_file)) {
        nob_return_defer(1);
    }

defer:;
    return result;
}

static bool build_libpeng(const char* source_root, const char** libpeng_file) {
    bool result = true;

    Nob_File_Paths libpeng_object_paths = {0};
    if (!build_object_files(source_root, libpeng_files, &libpeng_object_paths)) {
        nob_return_defer(1);
    }

    *libpeng_file = ODIR "/" LIB_PREFIX LIBPENG_STATIC_LIBRARY_FILE LIB_EXT;
    if (!package_library(libpeng_object_paths, *libpeng_file)) {
        nob_return_defer(1);
    }

defer:;
    return result;
}

static bool build_libscore(const char* source_root, const char** libscore_file) {
    bool result = true;

    Nob_File_Paths libscore_object_paths = {0};
    if (!build_object_files(source_root, libscore_files, &libscore_object_paths)) {
        nob_return_defer(1);
    }

    *libscore_file = ODIR "/" LIB_PREFIX LIBSCORE_STATIC_LIBRARY_FILE LIB_EXT;
    if (!package_library(libscore_object_paths, *libscore_file)) {
        nob_return_defer(1);
    }

defer:;
    return result;
}

static bool compile_object(const char* source_path, const char* object_path, const char* source_root) {
    bool result = true;

    Nob_File_Paths header_paths = {0};
    for (size_t i = 0; all_headers[i] != NULL; i++) {
        const char* header_path = nob_temp_sprintf("%s/%s", source_root, all_headers[i]);
        nob_da_append(&header_paths, header_path);
    }

    Nob_Cmd cmd = {0};
    if (0 == nob_needs_rebuild1(object_path, source_path) && 0 == nob_needs_rebuild(object_path, header_paths.items, header_paths.count)) {
        nob_return_defer(true);
    }

    nob_cmd_append(&cmd, CC);
#if defined(CC_MSVC)
    nob_cmd_append(&cmd, "/nologo");
    nob_cmd_append(&cmd, "/c", source_path);
    nob_cmd_append(&cmd, nob_temp_sprintf("/Fo%s", object_path));
    nob_cmd_append(&cmd, nob_temp_sprintf("/I%s/include", source_root));
#else // !CC_MSVC
    nob_cmd_append(&cmd, "-c", source_path);
    nob_cmd_append(&cmd, "-o", object_path);
    nob_cmd_append(&cmd, nob_temp_sprintf("-I%s/include", source_root));
#endif
    nob_cmd_append(&cmd, "" CFLAGS "");

    if (!nob_cmd_run_sync(cmd)) {
        nob_return_defer(false);
    }

defer:;
    nob_da_free(header_paths);
    nob_cmd_free(cmd);
    return result;
}

static bool package_library(Nob_File_Paths object_files, const char* library_path) {
    bool result = true;

    Nob_Cmd cmd = {0};
    if (0 == nob_needs_rebuild(library_path, object_files.items, object_files.count)) {
        nob_return_defer(true);
    }

    nob_cmd_append(&cmd, LIB);
#if defined(LIB_MSVC)
    nob_cmd_append(&cmd, "/nologo");
    nob_cmd_append(&cmd, nob_temp_sprintf("/out:%s", library_path));
#else // !LIB_MSVC
    nob_cmd_append(&cmd, "rcs", library_path);
#endif
    nob_da_append_many(&cmd, object_files.items, object_files.count);

    if (!nob_cmd_run_sync(cmd)) {
        nob_return_defer(false);
    }

defer:;
    nob_cmd_free(cmd);
    return result;
}

static bool link_executable(Nob_File_Paths input_paths, const char* executable_path) {
    bool result = true;

    Nob_Cmd cmd = {0};
    if (0 == nob_needs_rebuild(executable_path, input_paths.items, input_paths.count)) {
        nob_return_defer(true);
    }

    nob_cmd_append(&cmd, LD);
#if defined(LD_MSVC)
    nob_cmd_append(&cmd, "/nologo", "/subsystem:console");
    nob_cmd_append(&cmd, nob_temp_sprintf("/out:%s", executable_path));
    nob_cmd_append(&cmd, nob_temp_sprintf("/pdb:%s.pdb", executable_path));
#else // !LD_MSVC
    nob_cmd_append(&cmd, "-o", executable_path);
#endif
    nob_cmd_append(&cmd, "" LDFLAGS "");
    nob_da_append_many(&cmd, input_paths.items, input_paths.count);

    if (!nob_cmd_run_sync(cmd)) {
        nob_return_defer(false);
    }

defer:;
    nob_cmd_free(cmd);
    return result;
}

static bool build_object_files(const char* source_root, source_paths* sources, Nob_File_Paths* object_paths) {
    bool result = true;

    for (int64_t i = 0; sources[i].source_file != 0; i++) {
        const char* source_file = nob_temp_sprintf("%s/%s", source_root, sources[i].source_file);
        if (!compile_object(source_file, sources[i].object_file, source_root)) {
            nob_return_defer(false);
        }

        nob_da_append(object_paths, sources[i].object_file);
    }

defer:;
    return result;
}
