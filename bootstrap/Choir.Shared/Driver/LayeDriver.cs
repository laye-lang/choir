using System.Runtime.InteropServices;

using Choir.CommandLine;
using Choir.Driver.Options;

namespace Choir.Driver;

public sealed class LayeDriver
{
    private const string DriverVersion = @"{0} version 0.1.0";

    private const string DriverOptions = @"Compiles a list of Laye source files of the same module into a module object file

Usage: {0} [options] file...

Options:
    --help                   Display this information
    --version                Display compiler version information
    --verbose                Emit additional information about the compilation to stderr
    --color <arg>            Specify how compiler output should be colored
                             one of: 'auto', 'always', 'never'

    -i                       Read source text from stdin rather than a list of source files
    --file-kind <kind>       Specify the kind of subsequent input files, or 'default' to infer it from the extension
                             one of: 'default', 'laye', 'module'
    -o <path>                Override the output module object file path
                             To emit output to stdout, specify a path of '-'
                             default: '<module-name>.mod'
    --emit-llvm              Emit LLVM IR instead of Assembler when compiling with `--compile`.

    --no-corelib             Do not link against the the default Laye core libraries
                             This also implies '--no-stdlib'

    -L <lib-dir>             Adds <dir> to the library search list.
                             Directories are searched in the order they are provided, and values
                             provided through the CLI are searched after built-in and environment-
                             specified search paths.

    --lex                    Only read tokens from the source files, then exit
    --parse                  Only lex and parse the source files, then exit
    --sema                   Only lex, parse and analyse the source files, then exit
    --codegen                Only lex, parse, analyse and generate code for the source files, then exit
    --compile                Only lex, parse, analyse, generate and emit code for the source files, then exit

    --tokens                 Print token information to stderr when used alongside `--lex`
    --ast                    Print ASTs to stderr when used alongside `--parse` or `--sema`
    --no-lower               Do not lower the AST during semantic analysis when used alongside `--sema`
    --ir                     Print IR to stderr when used alongside `--codegen`";

    public static int RunWithArgs(StreamingDiagnosticWriter diag, string[] args, string programName = "layec")
    {
        var options = LayeDriverOptions.Parse(diag, new CliArgumentIterator(args));
        if (diag.HasIssuedErrors)
        {
            diag.Flush();
            return 1;
        }

        if (options.ShowVersion)
        {
            Console.WriteLine(string.Format(DriverVersion, programName));
            return 0;
        }

        if (options.ShowHelp)
        {
            Console.WriteLine(string.Format(DriverOptions, programName));
            return 0;
        }

        var driver = Create(diag, options);
        int exitCode = driver.Execute();

        diag.Flush();
        return exitCode;
    }

    public static LayeDriver Create(DiagnosticWriter diag, LayeDriverOptions options, string programName = "laye")
    {
        return new LayeDriver(programName, diag, options);
    }

    public string ProgramName { get; }
    public LayeDriverOptions Options { get; }
    public ChoirContext Context { get; }

    private LayeDriver(string programName, DiagnosticWriter diag, LayeDriverOptions options)
    {
        ProgramName = programName;
        Options = options;

        var abi = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ChoirAbi.WindowxX64 : ChoirAbi.SysV;
        Context = new(diag, ChoirTarget.X86_64, abi, options.OutputColoring)
        {
            EmitVerboseLogs = options.ShowVerboseOutput,
            OmitSourceTextInModuleBinary = options.OmitSourceTextInModuleBinary,
        };
    }

    public int Execute()
    {
        throw new NotImplementedException();
    }
}
