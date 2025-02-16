# Choir - A Compiler Library & Collection Written in C

## Building from source

This project is built with the help of [nob](https://github.com/tsoding/nob.h), a single-header C library designed for building projects with nothing but a C compiler.

### Bootstrap a build system

The project root directory contains the following relevant files and directories for configuring a build environment:
1. `nob.h`: The nob library file itself.
2. `config.c`: The build configuration tool source which depends on nob.
3. `cfg/`: A directory containing default supported build configurations as C header files.

To get started, compile the `config.c` source file with no additional flags:

On Windows, use of MSVC is supported:
```batch
> cl /Fo:config.exe config.c
```

But you can also use any C compiler of your choice. We recommend installing Clang, either through Visual Studio or a prebuilt binary package, for a more intuitive development experience on Windows without relying on additional systems such as MinGW.

On Linux, any old C compiler will do the trick. We just use `cc`:
```sh
$ cc -o config config.c
```

**NOTE:** The resulting executable file is self-rebuilding if ever you change it, so long as it recognized your platform correctly.
See the `NOB_REBUILD_URSELF` macro in `nob.h` for what command it will infer to recompile itself.

Once you've built the `config` executable, run it from your terminal.
It should print some simple help information instructing you to specify the name of a configuration file to use.
Pass in a relevant configuration header file path for your system, such as `cfg/x86_64-unknown-linux-clang.h`, to generate a build directory for that configuration.
Build directories are generated at `build/<configuration-name>`, where the configuration name is the file name you specified without extensions or parent directories included.

```sh
$ ./config cfg/x86_64-unknown-linux-clang.h
```

Navigate to the newly created build directory to continue the build process.

```sh
$ cd build/x86_64-unknown-linux-clang
```

### Build the configured build tool

In the build directory is a copy of the `nob.h` header and a newly created `nob.c`, copied from the `nob.template.c` file found at the project root.
Compile the nob build tool in the same way you did the config tool in the previous step, invoking your compiler without additional arguments.

```sh
$ cc -o nob nob.c
```

### Run the build tool

Then run the resulting executable file.
Build artifacts will be placed in the `out` directory, and the final project executables will be copied into this build configuration folder for easier access.

```sh
$ ./nob
```

### Clean up build directories

Both the `config` and `nob` tools support the `clean` command.
Running `nob clean` in its build directory will remove all of the build artifacts.
Running `config clean` at the project root will remove all build directories, and any build artifacts in the project root directory if you specified an in-source build during configuration.

```sh
# in a build configuration directory
$ ./nob clean
# or, at the project root
$ ./config clean
```
