#define _CRT_NONSTDC_NO_WARNINGS
#define _CRT_SECURE_NO_WARNINGS

#ifndef CC
#    if _WIN32
#        if defined(__GNUC__)
#            define CC "gcc"
#        elif defined(__clang__)
#            define CC "clang"
#        elif defined(_MSC_VER)
#            define CC "cl.exe"
#        endif
#    else // !_WIN32
#        define CC "cc"
#    endif // _WIN32
#endif     // CC

#include <assert.h>
#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

#ifdef _MSC_VER
#    define NOB_REBUILD_URSELF(binary_path, source_path) "cl.exe", "/nologo", nob_temp_sprintf("/Fe:%s", (binary_path)), source_path
#endif

#define NOB_IMPLEMENTATION
#include "nob.h"

// ===== Decls =====

static bool string_ends_with(const char* str, const char* ending);

static void cmd_append_common_cflags(Nob_Cmd* cmd);
static void cmd_append_common_ldflags(Nob_Cmd* cmd);

static bool build_source_files_in_dir(const char* source_dir_path, const char* output_dir_path, Nob_File_Paths include_paths, Nob_File_Paths* object_file_paths);
static bool build_choir_library(const char* project_name, Nob_File_Paths include_paths, const char** out_library_path);
static bool build_choir_executable(const char* project_name, const char* exe_name, Nob_File_Paths include_paths, Nob_File_Paths library_file_paths);

static void delete_temporaries();

static bool build_kos(const char** out_libkos_path);
static bool build_lyir(const char** out_liblaye_path);

// ===== Impl =====

static bool string_ends_with(const char* str, const char* ending) {
    size_t str_len = strlen(str);
    size_t ending_len = strlen(ending);
    return str_len >= ending_len && 0 == strncmp(str + str_len - ending_len, ending, ending_len);
}

static void cmd_append_common_cflags(Nob_Cmd* cmd) {
#ifdef _MSC_VER
    // Microsoft doesn't officially support most standard C.
    // While we're officially writing standard-compliant C99,
    // we give up and ask Windows to compile as C11.
    nob_cmd_append(cmd, "/nologo", "/std:c11", "/TC", "/permissive-");
    nob_cmd_append(cmd, "/Wall", "/WX");
    nob_cmd_append(cmd, "/wd4100", "/wd5045", "/wd4820");
    nob_cmd_append(cmd, "/D_CRT_SECURE_NO_WARNINGS");
    nob_cmd_append(cmd, "/Zi", "/fsanitize=address", "/Zc:__STDC__");
#else
    nob_cmd_append(cmd, "-std=c99", "-pedantic", "-pedantic-errors", "-Werror=return-type", "-ggdb", "-fsanitize=address");
#endif
}

static void cmd_append_common_ldflags(Nob_Cmd* cmd) {
#ifdef _MSC_VER
    nob_cmd_append(cmd, "/nologo", "/DEBUG");
#else
    nob_cmd_append(cmd, "-ggdb", "-fsanitize=address");
#endif
}

static void collect_header_files_from_dir(Nob_File_Paths* header_file_paths, const char* dir_path) {
    Nob_File_Paths file_paths = {0};
    if (!nob_read_entire_dir(dir_path, &file_paths)) {
        return;
    }
    
    for (size_t i = 2; i < file_paths.count; i++) {
        const char* file_name = file_paths.items[i];
        if (string_ends_with(file_name, ".h")) {
            nob_da_append(header_file_paths, nob_temp_sprintf("%s/%s", dir_path, file_name));
        }
    }
}

static bool build_source_files_in_dir(const char* source_dir_path, const char* output_dir_path, Nob_File_Paths include_paths, Nob_File_Paths* object_file_paths) {
    assert(source_dir_path != NULL);
    assert(output_dir_path != NULL);

    bool result = true;
    Nob_Cmd cmd = {0};
    Nob_File_Paths source_file_paths = {0};
    Nob_File_Paths header_file_paths = {0};

    if (!nob_read_entire_dir(source_dir_path, &source_file_paths)) {
        nob_return_defer(false);
    }
    
    collect_header_files_from_dir(&header_file_paths, source_dir_path);
    for (size_t i = 2; i < include_paths.count; i++) {
        collect_header_files_from_dir(&header_file_paths, include_paths.items[i]);
    }

    for (size_t i = 2; i < source_file_paths.count; i++) {
        const char* source_file_name = source_file_paths.items[i];
        if (!string_ends_with(source_file_name, ".c")) {
            continue;
        }
        
        const char* source_file_path = nob_temp_sprintf("%s/%s", source_dir_path, source_file_name);
#ifdef _WIN32
        const char* object_file_path = nob_temp_sprintf("%s/%s.obj", output_dir_path, source_file_name);
        const char* pdb_file_path = nob_temp_sprintf("%s/%s.pdb", output_dir_path, source_file_name);
#else
        const char* object_file_path = nob_temp_sprintf("%s/%s.o", output_dir_path, source_file_name);
#endif

        nob_da_append(object_file_paths, object_file_path);
        if (!nob_needs_rebuild1(object_file_path, source_file_path) && !nob_needs_rebuild(object_file_path, header_file_paths.items, header_file_paths.count)) {
            continue;
        }

        nob_cmd_append(&cmd, CC);
        cmd_append_common_cflags(&cmd);

        for (size_t i = 0; i < include_paths.count; i++) {
#ifdef _MSC_VER
            nob_cmd_append(&cmd, "/I", include_paths.items[i]);
#else
            nob_cmd_append(&cmd, "-I", include_paths.items[i]);
#endif
        }

#ifdef _WIN32
        nob_cmd_append(&cmd, "/c", nob_temp_sprintf("/Fo:%s", object_file_path));
        nob_cmd_append(&cmd, nob_temp_sprintf("/Fd:%s", pdb_file_path));
#else
        nob_cmd_append(&cmd, "-c", "-o", object_file_path);
#endif

        nob_cmd_append(&cmd, source_file_path);

        if (!nob_cmd_run_sync(cmd)) {
            nob_return_defer(false);
        }

        cmd.count = 0;
    }

defer:;
    nob_da_free(source_file_paths);
    nob_cmd_free(cmd);
    return result;
}

static bool build_choir_library(const char* project_name, Nob_File_Paths include_paths, const char** out_library_path) {
    assert(project_name != NULL);
    assert(out_library_path != NULL);

    bool result = true;
    Nob_Cmd cmd = {0};
    Nob_File_Paths object_file_paths = {0};

    const char* out_dir_path = nob_temp_sprintf("./src/%s/out/lib", project_name);

    nob_mkdir_if_not_exists(nob_temp_sprintf("./src/%s/out", project_name));
    nob_mkdir_if_not_exists(out_dir_path);

    if (!build_source_files_in_dir(nob_temp_sprintf("./src/%s/lib", project_name), out_dir_path, include_paths, &object_file_paths)) {
        nob_return_defer(false);
    }

    *out_library_path = nob_temp_sprintf("./src/%s/out/lib%s", project_name, project_name);

#ifdef _MSC_VER
    *out_library_path = nob_temp_sprintf("%s.lib", *out_library_path);
    if (!nob_needs_rebuild(*out_library_path, object_file_paths.items, object_file_paths.count)) {
        nob_return_defer(true);
    }

    nob_cmd_append(&cmd, "lib", "/nologo", nob_temp_sprintf("/out:%s", *out_library_path));
#else
    *out_library_path = nob_temp_sprintf("%s.a", *out_library_path);
    if (!nob_needs_rebuild(*out_library_path, object_file_paths.items, object_file_paths.count)) {
        nob_return_defer(true);
    }

    nob_cmd_append(&cmd, "ar", "rcs", *out_library_path);
#endif

    nob_da_append_many(&cmd, object_file_paths.items, object_file_paths.count);

    if (!nob_cmd_run_sync(cmd)) {
        nob_return_defer(false);
    }

defer:;
    nob_da_free(object_file_paths);
    nob_cmd_free(cmd);
    return result;
}

static bool build_choir_executable(const char* project_name, const char* exe_name, Nob_File_Paths include_paths, Nob_File_Paths library_file_paths) {
    assert(project_name != NULL);
    assert(exe_name != NULL);

#ifdef _MSC_VER
    const char* exe_path = nob_temp_sprintf("./src/%s/out/%s.exe", project_name, exe_name);
#else
    const char* exe_path = nob_temp_sprintf("./src/%s/out/%s", project_name, exe_name);
#endif

    bool result = true;
    Nob_Cmd cmd = {0};
    Nob_File_Paths object_file_paths = {0};
    Nob_File_Paths all_link_objects = {0};

    const char* out_dir_path = nob_temp_sprintf("./src/%s/out", project_name);
    nob_mkdir_if_not_exists(out_dir_path);

    if (!build_source_files_in_dir(nob_temp_sprintf("./src/%s", project_name), out_dir_path, include_paths, &object_file_paths)) {
        nob_return_defer(false);
    }

    nob_da_append_many(&all_link_objects, object_file_paths.items, object_file_paths.count);
    nob_da_append_many(&all_link_objects, library_file_paths.items, library_file_paths.count);

    if (!nob_needs_rebuild(exe_path, all_link_objects.items, all_link_objects.count)) {
        nob_return_defer(true);
    }

#ifdef _MSC_VER
    nob_cmd_append(&cmd, "link");
    cmd_append_common_ldflags(&cmd);
    nob_cmd_append(&cmd, nob_temp_sprintf("/out:%s", exe_path));
#else
    nob_cmd_append(&cmd, CC);
    cmd_append_common_ldflags(&cmd);
    nob_cmd_append(&cmd, "-o", exe_path);
#endif

    nob_da_append_many(&cmd, all_link_objects.items, all_link_objects.count);

    if (!nob_cmd_run_sync(cmd)) {
        nob_return_defer(false);
    }

defer:;
    nob_da_free(all_link_objects);
    nob_da_free(object_file_paths);
    nob_cmd_free(cmd);
    return result;
}

static bool build_kos(const char** out_libkos_path) {
    assert(out_libkos_path != NULL);

    bool result = true;
    Nob_File_Paths include_paths = {0};

    nob_da_append(&include_paths, "./src/kos/include");

    if (!build_choir_library("kos", include_paths, out_libkos_path)) {
        nob_return_defer(false);
    }

defer:;
    nob_da_free(include_paths);
    return result;
}

static bool build_lyir(const char** out_liblyir_path) {
    assert(out_liblyir_path != NULL);

    bool result = true;
    Nob_File_Paths include_paths = {0};
    Nob_File_Paths library_file_paths = {0};

    const char* libkos_path;
    if (!build_kos(&libkos_path)) {
        nob_return_defer(false);
    }

    nob_da_append(&include_paths, "./src/kos/include");
    nob_da_append(&include_paths, "./src/lyir/include");

    if (!build_choir_library("lyir", include_paths, out_liblyir_path)) {
        nob_return_defer(false);
    }

    nob_da_append(&library_file_paths, libkos_path);
    nob_da_append(&library_file_paths, *out_liblyir_path);

    if (!build_choir_executable("lyir", "lyirc", include_paths, library_file_paths)) {
        nob_return_defer(false);
    }

defer:;
    nob_da_free(library_file_paths);
    nob_da_free(include_paths);
    return result;
}

static bool build_ccly(const char** out_libccly_path) {
    assert(out_libccly_path != NULL);

    bool result = true;
    Nob_File_Paths include_paths = {0};
    Nob_File_Paths library_file_paths = {0};

    const char* libkos_path;
    if (!build_kos(&libkos_path)) {
        nob_return_defer(false);
    }

    const char* liblyir_path;
    if (!build_lyir(&liblyir_path)) {
        nob_return_defer(false);
    }

    nob_da_append(&include_paths, "./src/kos/include");
    nob_da_append(&include_paths, "./src/lyir/include");
    nob_da_append(&include_paths, "./src/ccly/include");

    if (!build_choir_library("ccly", include_paths, out_libccly_path)) {
        nob_return_defer(false);
    }

    nob_da_append(&library_file_paths, libkos_path);
    nob_da_append(&library_file_paths, liblyir_path);
    nob_da_append(&library_file_paths, *out_libccly_path);

    if (!build_choir_executable("ccly", "ccly", include_paths, library_file_paths)) {
        nob_return_defer(false);
    }

defer:;
    nob_da_free(library_file_paths);
    nob_da_free(include_paths);
    return result;
}

static bool build_laye(const char** out_liblaye_path) {
    assert(out_liblaye_path != NULL);

    bool result = true;
    Nob_File_Paths include_paths = {0};
    Nob_File_Paths library_file_paths = {0};

    const char* libkos_path;
    if (!build_kos(&libkos_path)) {
        nob_return_defer(false);
    }

    const char* liblyir_path;
    if (!build_lyir(&liblyir_path)) {
        nob_return_defer(false);
    }

    nob_da_append(&include_paths, "./src/kos/include");
    nob_da_append(&include_paths, "./src/lyir/include");
    nob_da_append(&include_paths, "./src/laye/include");

    if (!build_choir_library("laye", include_paths, out_liblaye_path)) {
        nob_return_defer(false);
    }

    nob_da_append(&library_file_paths, libkos_path);
    nob_da_append(&library_file_paths, liblyir_path);
    nob_da_append(&library_file_paths, *out_liblaye_path);

    if (!build_choir_executable("laye", "layec", include_paths, library_file_paths)) {
        nob_return_defer(false);
    }

defer:;
    nob_da_free(library_file_paths);
    nob_da_free(include_paths);
    return result;
}

static bool build_choir(const char** out_libchoir_path) {
    assert(out_libchoir_path != NULL);

    bool result = true;
    Nob_File_Paths include_paths = {0};
    Nob_File_Paths library_file_paths = {0};

    const char* libkos_path;
    if (!build_kos(&libkos_path)) {
        nob_return_defer(false);
    }

    const char* liblyir_path;
    if (!build_lyir(&liblyir_path)) {
        nob_return_defer(false);
    }

    const char* libccly_path;
    if (!build_ccly(&libccly_path)) {
        nob_return_defer(false);
    }

    const char* liblaye_path;
    if (!build_laye(&liblaye_path)) {
        nob_return_defer(false);
    }

    nob_da_append(&include_paths, "./src/kos/include");
    nob_da_append(&include_paths, "./src/lyir/include");
    nob_da_append(&include_paths, "./src/ccly/include");
    nob_da_append(&include_paths, "./src/laye/include");
    nob_da_append(&include_paths, "./src/choir/include");

    if (!build_choir_library("choir", include_paths, out_libchoir_path)) {
        nob_return_defer(false);
    }

    nob_da_append(&library_file_paths, libkos_path);
    nob_da_append(&library_file_paths, liblyir_path);
    nob_da_append(&library_file_paths, libccly_path);
    nob_da_append(&library_file_paths, liblaye_path);
    nob_da_append(&library_file_paths, *out_libchoir_path);

    if (!build_choir_executable("choir", "choir", include_paths, library_file_paths)) {
        nob_return_defer(false);
    }

defer:;
    nob_da_free(library_file_paths);
    nob_da_free(include_paths);
    return result;
}

static void delete_temporaries() {
    Nob_File_Paths children = {0};
    if (!nob_read_entire_dir(".", &children)) {
        nob_log(NOB_ERROR, "failed to enumerate files in the project root directory");
        return;
    }

    for (size_t i = 2; i < children.count; i++) {
        const char* path = children.items[i];
        if (string_ends_with(path, ".obj") || string_ends_with(path, ".pdb")) {
            remove(path);
        }
    }

    nob_da_free(children);
}

static bool nob_rmdir_recursive(const char* dir_path, bool commit) {
    size_t checkpoint = nob_temp_save();

    Nob_File_Paths children = {0};
    if (!nob_read_entire_dir(dir_path, &children)) {
        return false;
    }

    for (size_t i = 2; i < children.count; i++) {
        const char* child_name = children.items[i];
        const char* child_path = nob_temp_sprintf("%s/%s", dir_path, child_name);
        
        Nob_File_Type child_file_type = nob_get_file_type(child_path);
        if (child_file_type == NOB_FILE_DIRECTORY) {
            nob_rmdir_recursive(child_path, commit);
        } else {
            if (commit) {
                if (0 != remove(child_path)) {
                    nob_log(NOB_ERROR, "Failed to remove file %s: %s", child_path, strerror(errno));
                    return false;
                }
                
                nob_log(NOB_INFO, "Removed %s", child_path);
            } else {
                nob_log(NOB_INFO, "Would remove: %s", child_path);
            }
        }
    }
    
    if (commit) {
        if (0 != remove(dir_path)) {
            nob_log(NOB_ERROR, "Failed to remove directory %s: %s", dir_path, strerror(errno));
            return false;
        }
        
        nob_log(NOB_INFO, "Removed %s", dir_path);
    } else {
        nob_log(NOB_INFO, "Would remove: %s", dir_path);
    }

    nob_temp_rewind(checkpoint);
    return true;
}

static bool clean_project(const char* project_name) {
    const char* out_dir_path = nob_temp_sprintf("./src/%s/out", project_name);
    if (!nob_file_exists(out_dir_path)) {
        return true;
    }

    return nob_rmdir_recursive(out_dir_path, true);
}

static int clean() {
    bool success = true;
    success &= clean_project("ccly");
    success &= clean_project("kos");
    success &= clean_project("laye");
    success &= clean_project("choir");
    success &= clean_project("lyir");
    return success ? 0 : 1;
}

static bool build_bootstrap() {
    bool result = true;
    Nob_Cmd cmd = {0};

    nob_cmd_append(&cmd, "dotnet", "build", "bootstrap/layec");
#if defined(_WIN32)
    nob_cmd_append(&cmd, "-r", "win-x64");
#else
    nob_cmd_append(&cmd, "-r", "linux-x64");
#endif

    if (!nob_cmd_run_sync_and_reset(&cmd)) {
        nob_return_defer(false);
    }

defer:;
    nob_cmd_free(cmd);
    return result;
}

static int fchk() {
    int result = 0;
    Nob_Cmd cmd = {0};

    bool rebuild = false;

    if (!build_bootstrap()) {
        nob_return_defer(1);
    }

    if (!nob_file_exists("test-out")) {
        nob_cmd_append(&cmd, "cmake", "-S", ".", "-B", "test-out", "-DBUILD_TESTING=ON");
        if (!nob_cmd_run_sync_and_reset(&cmd)) {
            nob_return_defer(false);
        }

        cmd.count = 0;
        nob_cmd_append(&cmd, "cmake", "--build", "test-out");
        if (!nob_cmd_run_sync_and_reset(&cmd)) {
            nob_return_defer(false);
        }
    } else if (rebuild) {
        cmd.count = 0;
        nob_cmd_append(&cmd, "cmake", "--build", "test-out");
        if (!nob_cmd_run_sync_and_reset(&cmd)) {
            nob_return_defer(false);
        }
    }

    nob_cmd_append(&cmd, "ctest", "--test-dir", "test-out", "-j`nproc`", "--progress");
    if (!nob_cmd_run_sync_and_reset(&cmd)) {
        nob_return_defer(false);
    }

defer:;
    nob_cmd_free(cmd);
    return result;
}

int main(int argc, char** argv) {
    int result = 0;

    NOB_GO_REBUILD_URSELF(argc, argv);

    const char* program_name = nob_shift(argv, argc);
    if (argc > 0) {
        const char* command = nob_shift(argv, argc);
        if (0 == strcmp(command, "clean")) {
            return clean();
        } else if (0 == strcmp(command, "fchk")) {
            return fchk();
        } else {
            nob_log(NOB_ERROR, "unknown nob command '%s'", command);
            return 1;
        }
    }

    const char* libchoir_path;
    if (!build_choir(&libchoir_path)) {
        return 1;
    }

defer:;
    delete_temporaries();
    return result;
}
