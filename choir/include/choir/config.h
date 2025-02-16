#ifndef LCONFIG_H_
#define LCONFIG_H_

/*
@@ CHOIR_USE_C11 controlls the use of non-C11 features.
** Define it if you want Choir to avoid the use of C23 features,
** or Windows-specific features on Windows.
*/
/* #define CHOIR_USE_C11 */

#if !defined(CHOIR_USE_C11) && defined(_WIN32)
#    define CHOIR_USE_WINDOWS
#endif

#if defined(CHOIR_USE_WINDOWS)
#    define CHOIR_USE_DLL
#    if defined(_MSC_VER)
#        define CHOIR_USE_C11
#    endif
#endif

#if defined(__linux__)
#    define CHOIR_USE_LINUX
#endif

#if defined(CHOIR_USE_LINUX)
#    define CHOIR_USE_POSIX
#    define CHOIR_USE_DLOPEN
#endif

#if defined(CHOIR_BUILD_AS_DLL)
#    if defined(CHOIR_LIB)
#        define CHOIR_API __declspec(dllexport)
#    else
#        define CHOIR_API __declspec(dllimport)
#    endif
#else /* CHOIR_BUILD_AS_DLL */
#    define CHOIR_API extern
#endif

#endif /* LCONFIG_H_ */
