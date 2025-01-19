using Choir.CommandLine;

namespace Choir.Driver.Options;

public record class LayecHighLevelDriverOptions
    : BaseLayecDriverOptions<LayecHighLevelDriverOptions, BaseLayeCompilerDriverArgParseState>
{
    /// <summary>
    /// Additional library search directories.
    /// Library search directories are searched in the order they are specified.
    /// Any built-in or environment-specified library search paths are prepended to this list in the order they are identified.
    /// 
    /// The `layec` driver does not perform linking, so the libraries searched for are always Laye modules for use in semantic analysis and code generation.
    /// it will never try to resolve foreign libraries for final linking.
    /// </summary>
    public List<DirectoryInfo> LibrarySearchPaths { get; } = [];

    /// <summary>
    /// The `--no-corelib` flag.
    /// Disables linking to the Laye core library, requiring the programmer to provide their own implementation.
    /// </summary>
    public bool NoCoreLibrary { get; set; }

    protected override void HandleArgument(string arg, DiagnosticWriter diag,
        CliArgumentIterator args, BaseLayeCompilerDriverArgParseState state)
    {
        switch (arg)
        {
            default: base.HandleArgument(arg, diag, args, state); break;

            case "--no-corelib": NoCoreLibrary = true; break;

            case "-L":
            {
                if (!args.Shift(out string? libDir) || libDir.IsNullOrEmpty())
                    diag.Error($"Argument to '{arg}' is missing; expected 1 (non-empty) value.");
                else LibrarySearchPaths.Add(new DirectoryInfo(libDir));
            } break;
        }
    }
}
