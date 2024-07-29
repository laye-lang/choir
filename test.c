#include <stdio.h>

int _foo_impl();

#define foo _foo_impl

int main(int argc, char** argv) {
    printf("Hello, from Choir!\n");
    return 0;
}