#define _CRT_SECURE_NO_WARNINGS
#define _CRT_NONSTDC_NO_DEPRECATE 1

#define NOB_IMPLEMENTATION
#include "nob.h"

#include <stdint.h>

Nob_String_View get_file_name_without_extension(Nob_String_View name) {
    if (name.count == 0) {
        return name;
    }

    int64_t start = (int64_t)name.count;
    int64_t end = (int64_t)name.count;

    for (; start > 0 && name.data[start - 1] != '/' && name.data[start - 1] != '\\'; start--);
    for (; end > 0 && name.data[end] != '.'; end--);

    if (end <= start) {
        end = (int64_t)name.count;
    }

    name.data = name.data + start;
    name.count = (size_t)(end - start);

    return name;
}

static void help(const char* program_name) {
    fprintf(stderr, "claye build configuration tool\n");
    fprintf(stderr, "usage: %s [config-file] [config-name] [args...]\n", program_name);
    fprintf(stderr, "\n");
    fprintf(stderr, "arguments:\n");
    fprintf(stderr, "  --help              Print this help information.\n");
    fprintf(stderr, "  --in-source         Configures an in-source build.\n");
    fprintf(stderr, "                      By default, build files are generated 'out-of-source' and\n");
    fprintf(stderr, "                      located in 'build/<config-name> rather than within the root\n");
    fprintf(stderr, "                      of the project directory.\n");
}

static void remove_if_exists(const char* file_path) {
    if (nob_file_exists(file_path)) remove(file_path);
}

static bool clean_config_dir(const char* config_root, bool is_out_of_source) {
    bool result = true;
    Nob_File_Paths out_files = {0};

    remove_if_exists(nob_temp_sprintf("%s/claye", config_root));
    remove_if_exists(nob_temp_sprintf("%s/claye.exe", config_root));

    remove_if_exists(nob_temp_sprintf("%s/clayec", config_root));
    remove_if_exists(nob_temp_sprintf("%s/clayec.exe", config_root));

    remove_if_exists(nob_temp_sprintf("%s/config.h", config_root));

    remove_if_exists(nob_temp_sprintf("%s/nob", config_root));
    remove_if_exists(nob_temp_sprintf("%s/nob.exe", config_root));
    remove_if_exists(nob_temp_sprintf("%s/nob.old", config_root));
    remove_if_exists(nob_temp_sprintf("%s/nob.exe.old", config_root));
    remove_if_exists(nob_temp_sprintf("%s/nob.c", config_root));

    if (nob_file_exists(nob_temp_sprintf("%s/out", config_root))) {
        if (!nob_read_entire_dir(nob_temp_sprintf("%s/out", config_root), &out_files)) {
            nob_return_defer(false);
        }
    
        for (size_t i = 2; i < out_files.count; i++) {
            remove(nob_temp_sprintf("%s/out/%s", config_root, out_files.items[i]));
        }
    
        remove(nob_temp_sprintf("%s/out", config_root));
    }

    if (is_out_of_source) {
        remove_if_exists(nob_temp_sprintf("%s/nob.h", config_root));
    }

defer:;
    nob_da_free(out_files);
    return result;
}

static int clean(int argc, char** argv) {
    int result = 0;
    Nob_File_Paths build_configs = {0};

    if (!clean_config_dir(".", false)) {
        nob_return_defer(false);
    }

    if (!nob_read_entire_dir("build", &build_configs)) {
        nob_return_defer(1);
    }

    for (size_t i = 2; i < build_configs.count; i++) {
        const char* config_dir = nob_temp_sprintf("build/%s", build_configs.items[i]);
        if (!clean_config_dir(config_dir, true)) {
            nob_return_defer(false);
        }
        
        remove(config_dir);
    }
    
    remove("build");

    // remove_if_exists("config");
    // remove_if_exists("config.exe");
    remove_if_exists("config.old");
    remove_if_exists("config.exe.old");

defer:;
    nob_da_free(build_configs);
    return result;
}

int main(int argc, char** argv) {
    NOB_GO_REBUILD_URSELF(argc, argv);

    int result = 0;
    Nob_Cmd cmd = {0};

    const char* program_name = nob_shift_args(&argc, &argv);
    
    if (argc == 0) {
        help(program_name);
        nob_return_defer(1);
    }

    if (argc != 0) {
        const char* maybe_cmd = argv[0];
        if (0 == strcmp(maybe_cmd, "clean")) {
            nob_shift_args(&argc, &argv);
            nob_return_defer(clean(argc, argv));
        }
    }

    const char* config_file = NULL;
    const char* config_name_override = NULL;
    const char* nob_template_file = "nob.template.c";
    bool build_out_of_source = true;

    while (argc > 0) {
        const char* arg = nob_shift_args(&argc, &argv);
        if (0 == strcmp(arg, "--help")) {
            help(program_name);
            nob_return_defer(0);
        } else if (0 == strcmp(arg, "--in-source")) {
            build_out_of_source = false;
        } else {
            if (config_file == NULL) {
                config_file = arg;
                if (!nob_file_exists(config_file)) {
                    nob_log(NOB_ERROR, "The specified configuration file '%s' does not exist. Specify a configuration header file from the 'configurations' directory.", config_file);
                    nob_return_defer(1);
                }
            } else if (config_name_override == NULL) {
                config_name_override = arg;
            } else {
                nob_log(NOB_ERROR, "Unrecognized argument '%s'.", arg);
                nob_return_defer(1);
            }

        }
    }

    if (config_file == NULL) {
        nob_log(NOB_ERROR, "Specify a configuration header file from the 'configurations' directory.", config_file);
        help(program_name);
        nob_return_defer(1);
    }

    Nob_String_View config_name;
    if (config_name_override == NULL)
    {
        config_name = get_file_name_without_extension(nob_sv_from_cstr(config_file));
        nob_log(NOB_INFO, "Selected configuration name: %.*s", (int)config_name.count, config_name.data);
    }
    else config_name = (Nob_String_View){.data = config_name_override, .count = strlen(config_name_override)};

    const char* config_build_dir;
    if (build_out_of_source) {
        if (!nob_mkdir_if_not_exists("build")) {
            nob_return_defer(1);
        }

        config_build_dir = nob_temp_sprintf("build/%.*s", (int)config_name.count, config_name.data);

        if (!nob_mkdir_if_not_exists(config_build_dir)) {
            nob_return_defer(1);
        }
        
        nob_copy_file("nob.h", nob_temp_sprintf("%s/nob.h", config_build_dir));
    } else {
        config_build_dir = ".";
    }

    const char* config_header_path = nob_temp_sprintf("%s/config.h", config_build_dir);
    if (nob_file_exists(config_header_path)) remove(config_header_path);
    nob_copy_file(config_file, config_header_path);

    const char* nob_path = nob_temp_sprintf("%s/nob.c", config_build_dir);
    if (nob_file_exists(nob_path)) remove(nob_path);
    nob_copy_file(nob_template_file, nob_path);

defer:;
    nob_cmd_free(cmd);
    return result;
}
