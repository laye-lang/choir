using Choir.CommandLine;

namespace Choir.Driver.Options;

public record class BaseLayeCompilerDriverArgParseState
{
    public InputFileLanguage CurrentFileType { get; set; } = InputFileLanguage.Default;
    public OutputColoring OutputColoring { get; set; } = OutputColoring.Auto;
}

public record class BaseLayeDriverOptions<TSelf, TArgParseState>
    where TSelf : BaseLayeDriverOptions<TSelf, TArgParseState>, new()
    where TArgParseState : BaseLayeCompilerDriverArgParseState, new()
{
    /// <summary>
    /// The `--version` flag.
    /// When specified, the driver prints the program version, then exits.
    /// </summary>
    public bool ShowVersion { get; set; }

    /// <summary>
    /// The `--help` flag.
    /// When specified, the driver prints the help text, then exits.
    /// </summary>
    public bool ShowHelp { get; set; }

    /// <summary>
    /// The `--verbose` flag.
    /// Allows emitting verbose information about the compilation to stderr.
    /// </summary>
    public bool ShowVerboseOutput { get; set; }

    /// <summary>
    /// True if the compiler output should be colored.
    /// Determined by the `--color` flag.
    /// </summary>
    public bool OutputColoring { get; set; }

    /// <summary>
    /// The name of the generated binary file.
    /// 
    /// `laye` can produce executable or object files. It delegates to `layec` to produce object files
    /// and to the system linker to link into executables.
    /// </summary>
    public string? OutputFilePath { get; set; }

    /// <summary>
    /// Additional binary files that the input source code expects to link against in the future.
    /// `layec` will read Laye metadata from these files to perform smenatic analysis against.
    /// </summary>
    public List<FileInfo> BinaryDependencyFiles { get; } = [];

    /// <summary>
    /// The format of assembler output when compiling with the `--compile` flag.
    /// Defaults to traditional assembler code, but can be switched to LLVM IR with the `--emit-llvm` flag.
    /// </summary>
    public AssemblerFormat AssemblerFormat { get; set; } = AssemblerFormat.Assembler;

    /// <summary>
    /// The `--omit-source-text` flag.
    /// Disables the inclusion of source text in binary module files.
    /// </summary>
    public bool OmitSourceTextInModuleBinary { get; set; } = false;

    /// <summary>
    /// The `--distribution` flag.
    /// Enables "distribution" builds, where file paths are simplified since the end user won't have source files for the module on their system.
    /// </summary>
    public bool IsDistribution { get; set; } = false;

    /// <summary>
    /// Through what stage the driver should run.
    /// By default, a call to `layec` will generate an object file for the target system.
    /// All of the <see cref="DriverStage.Lex"/> (--lex), <see cref="DriverStage.Parse"/> (--parse), <see cref="DriverStage.Sema"/>, (--sema), <see cref="DriverStage.Codegen"/> (--codegen), <see cref="DriverStage.Compile"/> (--compile), <see cref="DriverStage.Assemble"/> (-c, --assemble), and <see cref="DriverStage.Link"/> stages are supported.
    /// When a specific driver stage is selected, any alternate output forms it supports may also be set; any alternate output form which does not apply to the set driver stage is ignored.
    /// </summary>
    public DriverStage DriverStage { get; set; } = DriverStage.Link;

    /// <summary>
    /// The `--tokens` compiler flag, determining if tokens should be printed when running only the <see cref="DriverStage.Lex"/> stage.
    /// </summary>
    public bool PrintTokens { get; set; } = false;

    /// <summary>
    /// The `--ast` compiler flag, determining if the AST should be printed when running only the <see cref="DriverStage.Parse"/> or <see cref="DriverStage.Sema"/> stages.
    /// </summary>
    public bool PrintAst { get; set; } = false;

    /// <summary>
    /// The `--ir` compiler flag, determining if the IR should be printed when running only the <see cref="DriverStage.Codegen"/> stage.
    /// </summary>
    public bool PrintIR { get; set; } = false;

    /// <summary>
    /// The `--no-lower` compiler flag, determining if lowering should be skipped only during the <see cref="DriverStage.Sema"/> stage.
    /// </summary>
    public bool NoLower { get; set; } = false;

    protected virtual void HandleValue(string value, DiagnosticWriter diag,
        CliArgumentIterator args, TArgParseState state)
    {
        diag.ICE($"Unhandled positional argument '{value}'. The compiler driver argument parsers should always handle these themselves. {GetType().Name} did not.");
    }

    protected virtual void HandleArgument(string arg, DiagnosticWriter diag,
        CliArgumentIterator args, TArgParseState state)
    {
        switch (arg)
        {
            default:
            {
                diag.Error($"Unknown argument '{arg}'.");
            } break;

            case "--help": ShowHelp = true; break;
            case "--version": ShowVersion = true; break;
            case "--verbose": ShowVerboseOutput = true; break;

            case "--color":
            {
                if (!args.Shift(out string? color))
                    diag.Error($"Argument to '{arg}' is missing; expected 1 value.");
                else
                {
                    switch (color.ToLower())
                    {
                        default: diag.Error($"Color mode '{color}' not recognized."); break;
                        case "auto": state.OutputColoring = Driver.OutputColoring.Auto; break;
                        case "always": state.OutputColoring = Driver.OutputColoring.Always; break;
                        case "never": state.OutputColoring = Driver.OutputColoring.Never; break;
                    }
                }
            } break;

            case "-o":
            {
                if (!args.Shift(out string? outputPath))
                    diag.Error($"Argument to '{arg}' is missing; expected 1 value.");
                else OutputFilePath = outputPath;
            } break;

            case "--emit-llvm": AssemblerFormat = AssemblerFormat.LLVM; break;

            case "--omit-source-text": OmitSourceTextInModuleBinary = true; break;
            case "--distribution": IsDistribution = true; break;

            case "--lex": DriverStage = DriverStage.Lex; break;
            case "--parse": DriverStage = DriverStage.Parse; break;
            case "--sema": DriverStage = DriverStage.Sema; break;
            case "--codegen": DriverStage = DriverStage.Codegen; break;
            case "--compile": DriverStage = DriverStage.Compile; break;
            case "--assemble":
            case "-c": DriverStage = DriverStage.Assemble; break;

            case "--tokens": PrintTokens = true; break;
            case "--ast": PrintAst = true; break;
            case "--no-lower": NoLower = true; break;
            case "--ir": PrintIR = true; break;
        }
    }

    public static TSelf Parse(DiagnosticWriter diag, CliArgumentIterator args)
    {
        var options = new TSelf();
        var state = new TArgParseState();

        while (args.Shift(out string arg))
        {
            if (arg.StartsWith('-'))
                options.HandleArgument(arg, diag, args, state);
            else options.HandleValue(arg, diag, args, state);
        }

        if (state.OutputColoring == Driver.OutputColoring.Auto)
            state.OutputColoring = Console.IsErrorRedirected ? Driver.OutputColoring.Never : Driver.OutputColoring.Always;
        options.OutputColoring = state.OutputColoring == Driver.OutputColoring.Always;

        return options;
    }
}
