#include <cc/cc.h>
#include <stdio.h>

#ifndef BUILD_VERSION
#    define BUILD_VERSION "<unknown>"
#endif // BUILD_VERSION

static const char* help_text =
    "Pink C Compiler Version %s\n";

int main(int argc, char** argv) {
    const char* program_name = argv[0];
    fprintf(stderr, help_text, BUILD_VERSION);
    return 0;
}
