# %OS% is defined on Windows
ifdef OS
	ODIR = win32/o
	RM = del /Q
	RMRF = rmdir /Q /S
	CP = copy /Y
	MkDir = if not exist $1 ( mkdir $1 )
	ExePath = $1.exe
   	FixPath = $(subst /,\,$1)
	CHOIR_DOTNET_RUNTIME = win-x64
else
	ifeq ($(shell uname), Linux)
		ODIR = linux/o
		RM = rm -f
		RMRF = rm -rf
		CP = cp -f
		MkDir = mkdir -p $1
		ExePath = $1
		FixPath = $1
		CHOIR_DOTNET_RUNTIME = linux-x64
	endif
endif

CC = clang
LD = clang

CFLAGS = -std=c2x -pedantic -pedantic-errors -Wall -Wextra -Wno-unused-parameter -Wno-unused-variable -Wno-unused-function -Wno-gnu-zero-variadic-macro-arguments -Wno-missing-field-initializers -Wno-deprecated-declarations -fdata-sections -ffunction-sections -Werror=return-type -D__USE_POSIX -D_XOPEN_SOURCE=600 -fms-compatibility -fsanitize=address -ggdb
LDFLAGS = -Wl,--gc-sections -Wl,--as-needed -fsanitize=address

CHOIR_DOTNET_BIN = $(call FixPath,./choir/bootstrap/bin/Debug/net8.0/$(CHOIR_DOTNET_RUNTIME)/)
CHOIR_DOTNET_DLL = $(CHOIR_DOTNET_BIN)Choir.Driver.dll

all: libchoir choir
.PHONY: all choir1 choir2 choir libchoir1 libchoir2 libchoir choir-dotnet

choir1: choir-dotnet
choir2: choir1
choir: choir2

libchoir1: choir-dotnet
libchoir2: libchoir1
libchoir: libchoir2

choir-dotnet: $(CHOIR_DOTNET_DLL)

$(CHOIR_DOTNET_DLL):
	dotnet build $(call FixPath,./choir/bootstrap/Choir.Driver) -r $(CHOIR_DOTNET_RUNTIME)
