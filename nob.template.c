#include "config.h"

#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

#define ODIR "out"

#if defined(_WIN32)
#    define LIBCHOIR_STATIC_LIBRARY_FILE "libchoir"
#    define LIBLAYE_STATIC_LIBRARY_FILE  "liblaye"
#else
#    define LIBCHOIR_STATIC_LIBRARY_FILE "choir"
#    define LIBLAYE_STATIC_LIBRARY_FILE  "laye"
#endif

#define LAYE_EXECUTABLE_FILE  "laye"
#define LAYEC_EXECUTABLE_FILE "layec"

#if defined(NOBCONFIG_MISSING)
#    error No nob configuration has been specified. Please copy the relevant config file from the config directory for your platform and toolchain into the appropriate 'nob_config.<PLATFORM>.h' file.
#endif

#define NOB_IMPLEMENTATION
#include "nob.h"

typedef struct {
    const char* source_file;
    const char* object_file;
} source_paths;

static source_paths libchoir_files[] = {
    {"lib/choir/alloc.c", ODIR "/choir-alloc.o"},
    {"lib/choir/arena.c", ODIR "/choir-arena.o"},
    {"lib/choir/gpalloc.c", ODIR "/choir-gpalloc.o"},
    {0},
};

static source_paths liblaye_files[] = {
    {"lib/laye/token.c", ODIR "/laye-token.o"},
    {"lib/laye/lex.c", ODIR "/laye-lex.o"},
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

static bool compile_object(const char* source_path, const char* object_path, const char* source_root) {
    bool result = true;

    Nob_Cmd cmd = {0};
    if (0 == nob_needs_rebuild1(object_path, source_path)) {
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

static void clean(void) {
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

defer:
    return result;
}

int main(int argc, char** argv) {
    NOB_GO_REBUILD_URSELF(argc, argv);

    bool result = 0;

    const char* program_name = nob_shift_args(&argc, &argv);

    if (argc > 0) {
        const char* arg = nob_shift_args(&argc, &argv);
        if (0 == strcmp(arg, "clean")) {
            clean();
            nob_return_defer(0);
        }
    }

    if (!nob_mkdir_if_not_exists(ODIR)) {
        nob_return_defer(1);
    }

    const char* source_root = ".";
    while (!nob_file_exists(nob_temp_sprintf("%s/choir", source_root))) {
        source_root = nob_temp_sprintf("%s/..", source_root);
    }

    source_root = nob_temp_sprintf("%s/choir", source_root);
    nob_log(NOB_INFO, "source_root = %s\n", source_root);

    Nob_File_Paths libchoir_object_paths = {0};
    if (!build_object_files(source_root, libchoir_files, &libchoir_object_paths)) {
        nob_return_defer(1);
    }

    const char* libchoir_file = ODIR "/" LIB_PREFIX LIBCHOIR_STATIC_LIBRARY_FILE LIB_EXT;
    if (!package_library(libchoir_object_paths, libchoir_file)) {
        nob_return_defer(1);
    }

    Nob_File_Paths liblaye_object_paths = {0};
    if (!build_object_files(source_root, liblaye_files, &liblaye_object_paths)) {
        nob_return_defer(1);
    }

    const char* liblaye_file = ODIR "/" LIB_PREFIX LIBLAYE_STATIC_LIBRARY_FILE LIB_EXT;
    if (!package_library(liblaye_object_paths, liblaye_file)) {
        nob_return_defer(1);
    }

    Nob_File_Paths layec_input_paths = {0};
    if (!build_object_files(source_root, layec_files, &layec_input_paths)) {
        nob_return_defer(1);
    }

    nob_da_append(&layec_input_paths, libchoir_file);
    nob_da_append(&layec_input_paths, liblaye_file);
    const char* layecfile = ODIR "/" LAYEC_EXECUTABLE_FILE EXE_EXT;
    if (!link_executable(layec_input_paths, layecfile)) {
        nob_return_defer(1);
    }

    Nob_File_Paths laye_input_paths = {0};
    if (!build_object_files(source_root, laye_files, &laye_input_paths)) {
        nob_return_defer(1);
    }

    nob_da_append(&layec_input_paths, libchoir_file);
    nob_da_append(&layec_input_paths, liblaye_file);
    const char* layefile = ODIR "/" LAYE_EXECUTABLE_FILE EXE_EXT;
    if (!link_executable(laye_input_paths, layefile)) {
        nob_return_defer(1);
    }

    if (!nob_copy_file(layecfile, LAYEC_EXECUTABLE_FILE EXE_EXT)) {
        nob_return_defer(1);
    }

    if (!nob_copy_file(layefile, LAYE_EXECUTABLE_FILE EXE_EXT)) {
        nob_return_defer(1);
    }

defer:;
    return result;
}
