#include <laye/laye.h>
#include <stdio.h>

#ifndef BUILD_VERSION
#    define BUILD_VERSION "<unknown>"
#endif // BUILD_VERSION

static const char* help_text =
    "Laye Module Compiler Version %s\n";

int main(int argc, char** argv) {
    int result = 0;

    ch_allocator gpa = ch_general_purpose_allocator();

    const char* program_name = argv[0];
    fprintf(stderr, help_text, BUILD_VERSION);

defer:
    ch_allocator_deinit(gpa);
    return result;
}
