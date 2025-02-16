
#define _CRT_SECURE_NO_WARNINGS
#define _CRT_NONSTDC_NO_DEPRECATE 1

#define CC "cl"
#define LIB "lib"
#define LD "link"

#define CC_MSVC
#define LIB_MSVC
#define LD_MSVC

#define LCONFIG 
#define CFLAGS "/std:clatest", "/Wall -Wno-unused-parameter", "/WX", "/Zi" LCONFIG
#define LDFLAGS "/subsystem:console"

#define EXE_EXT ".exe"
#define LIB_EXT ".lib"
#define LIB_PREFIX ""
