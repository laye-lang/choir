/*
@@ TRIPLE is, ideally, a unique identifier for this configuration.
** The triple is included in output temporary files stored in ODIR so that
** multiple build configurations can be used in the same working directory
*/
#define TRIPLE "x86_64-linux"

#define CC "clang"
#define LIB "ar"
#define LD "clang"

#define LCONFIG , "-DLAYE_USE_LINUX"
#define CFLAGS "-Iinclude", "-std=c23", "-Wall", "-Werror", "-Werror=return-type", "-pedantic", "-pedantic-errors", "-ggdb", "-fsanitize=address" LCONFIG
#define LDFLAGS "-ggdb", "-fsanitize=address"

#define EXE_EXT ""
#define LIB_EXT ".a"

#define LIB_PREFIX "lib"
