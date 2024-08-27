CHOIR_DOTNET_BINDIR = ./choir/bootstrap/Choir.Driver/bin/Release/net8.0/linux-x64/publish/
CHOIR_DOTNET_EXE = $(CHOIR_DOTNET_BINDIR)Choir.Driver

.PHONY: all
all: choir-dotnet

.PHONY: choir-dotnet
choir-dotnet:
	dotnet publish ./choir/bootstrap/Choir.Driver -r linux-x64
test-dotnet:
	$(CHOIR_DOTNET_EXE) test.laye --sema

CC = choir cc
LD = ld

CFLAGS = -std=c2x -pedantic -pedantic-errors -Wall -Wextra -Wno-unused-parameter -Wno-unused-variable -Wno-unused-function -Wno-gnu-zero-variadic-macro-arguments -Wno-missing-field-initializers -Wno-deprecated-declarations -fdata-sections -ffunction-sections -Werror=return-type -D__USE_POSIX -D_XOPEN_SOURCE=600 -fms-compatibility -fsanitize=address -ggdb
LDFLAGS = -Wl,--gc-sections -Wl,--as-needed -fsanitize=address

choir/bootstrap/stage0.ssa: choir-dotnet
#	$(CHOIR) -S -emit-qbe -o stage0.ssa choir-driver.laye

stage0: choir/bootstrap/stage0.ssa
