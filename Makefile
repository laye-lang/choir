# %OS% is defined on Windows
ifdef OS
	RM = del /Q
	RMDIR = rd /S /Q
	
	EXE_SUFFIX = .exe
	OBJ_SUFFIX = .obj
	LIB_SUFFIX = .lib
	DYLIB_SUFFIX = .dll

	MkDir = if not exist "$1" ( mkdir "$1" )
	LibAr = llvm-lib /out:$1

	RUNTIME = win-x64
	LAYEC_EXE = ./bootstrap/layec/bin/Debug/net8.0/$(RUNTIME)/layec.exe
else
	ifeq ($(shell uname), Linux)
		RM = rm -f
		RMDIR = rm -rf
		
		EXE_SUFFIX =
		OBJ_SUFFIX = .o
		LIB_SUFFIX = .a
		DYLIB_SUFFIX = .so

		MkDir = mkdir -p $1
		LibAr = ar rcs $1
		
		RUNTIME = linux-x64
		LAYEC_EXE = ./bootstrap/layec/bin/Debug/net8.0/$(RUNTIME)/layec
	endif
endif

.PHONY: all
all: layec libs

.PHONY: libs
libs: librt0 libcore libc

.PHONY: layec
layec: $(LAYEC_EXE)
$(LAYEC_EXE):
	dotnet build ./bootstrap/layec --runtime $(RUNTIME)

librt0_sources = $(wildcard ./lib/laye/rt0/*.laye)
.PHONY: librt0
librt0: ./lib/laye/rt0.mod
./lib/laye/rt0.mod: $(LAYEC_EXE) $(librt0_sources)
	$(LAYEC_EXE) -o $@ --no-corelib $(librt0_sources)

libcore_sources = $(wildcard ./lib/laye/core/*.laye)
.PHONY: libcore
libcore: ./lib/laye/core.mod
./lib/laye/core.mod: $(LAYEC_EXE) $(libcore_sources)
	$(LAYEC_EXE) -o $@ --no-corelib $(libcore_sources)

libffi_sources = $(wildcard ./lib/laye/ffi/*.laye)
.PHONY: libffi
libffi: ./lib/laye/ffi.mod
./lib/laye/ffi.mod: $(LAYEC_EXE) $(libffi_sources)
	$(LAYEC_EXE) -o $@ --no-corelib $(libffi_sources)

libc_sources = $(wildcard ./lib/laye/libc/*.laye)
.PHONY: libc
libc: ./lib/laye/libc.mod
./lib/laye/libc.mod: $(LAYEC_EXE) $(libc_sources)
	$(LAYEC_EXE) -o $@ --no-corelib $(libc_sources)

.PHONY: test
test:
	dotnet run --project bootstrap/Choir.TestRunner
