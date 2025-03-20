using Choir.Diagnostics;

namespace Choir.Driver;

public record class BaseCompilerDriverParseState
{
    public OutputColoring OutputColoring { get; set; } = OutputColoring.Auto;
}

public abstract class BaseCompilerDriverOptions<TSelf, TContext, TParseState>
    where TSelf : BaseCompilerDriverOptions<TSelf, TContext, TParseState>, new()
    where TContext : ChoirContext
    where TParseState : BaseCompilerDriverParseState, new()
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

    protected virtual void HandleValue(string value, DiagnosticEngine diag, CliArgumentIterator args, TParseState state)
    {
        diag.Emit(DiagnosticLevel.Fatal, $"Unhandled positional argument '{value}'. The compiler driver option parsers should always handle these themselves. {GetType().Name} did not.");
    }

    protected virtual void HandleArgument(string arg, DiagnosticEngine diag, CliArgumentIterator args, TParseState state)
    {
        switch (arg)
        {
            default:
            {
                diag.Emit(DiagnosticLevel.Error, $"Unknown argument '{arg}'.");
            } break;

            case "--help": ShowHelp = true; break;
            case "--version": ShowVersion = true; break;
            case "--verbose": ShowVerboseOutput = true; break;

            case "--color":
            {
                if (!args.Shift(out string? color))
                    diag.Emit(DiagnosticLevel.Error, $"Argument to '{arg}' is missing; expected 1 value.");
                else
                {
                    switch (color.ToLower())
                    {
                        default: diag.Emit(DiagnosticLevel.Error, $"Color mode '{color}' not recognized."); break;
                        case "auto": state.OutputColoring = Driver.OutputColoring.Auto; break;
                        case "always": state.OutputColoring = Driver.OutputColoring.Always; break;
                        case "never": state.OutputColoring = Driver.OutputColoring.Never; break;
                    }
                }
            } break;
        }
    }

    public static TSelf Parse(DiagnosticEngine diag, CliArgumentIterator args)
    {
        var options = new TSelf();
        var state = new TParseState();

        while (args.Shift(out string arg))
        {
            if (arg.StartsWith('-'))
                options.HandleArgument(arg, diag, args, state);
            else options.HandleValue(arg, diag, args, state);
        }

        if (state.OutputColoring == Driver.OutputColoring.Auto)
            state.OutputColoring = (Console.IsOutputRedirected || Console.IsErrorRedirected) ? Driver.OutputColoring.Never : Driver.OutputColoring.Always;
        options.OutputColoring = state.OutputColoring == Driver.OutputColoring.Always;

        return options;
    }
}
