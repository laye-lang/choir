using System.Diagnostics;

using Choir.CommandLine;

namespace Choir.Driver.Options;

public record class BaseLayecDriverOptions<TSelf, TArgParseState>
    : BaseLayeDriverOptions<TSelf, TArgParseState>
    where TSelf : BaseLayecDriverOptions<TSelf, TArgParseState>, new()
    where TArgParseState : BaseLayeCompilerDriverArgParseState, new()
{
    /// <summary>
    /// Every input source file is treated as a Laye source file of the same semantic module.
    /// `layec` will validate that every source file begins with a `module` directive and that the directives specify the same module name.
    /// The only exception to this rule is that executables may not specify a module name, so long as every program source file does the same.
    /// Since `layec` does not know which other modules you are building and linking against, this is not strictly enforced.
    /// You *may* build modules manually which have no module declarations in them using `layec`, though it is discouraged.
    /// A compiler driver or build system may emit an error if you explicitly output libraries which do not have module directives in them.
    /// 
    /// Other files may be passed to `layec` if they are of known binary output formats so that library information can be extracted from them and are stored separately.
    /// </summary>
    public List<FileInfo> ModuleSourceFiles { get; } = [];

    protected override void HandleValue(string value, DiagnosticWriter diag,
        CliArgumentIterator args, TArgParseState state)
    {
        var inputFileInfo = new FileInfo(value);
        if (!inputFileInfo.Exists)
            diag.Error($"No such file or directory '{value}'.");

        var inputFileType = state.CurrentFileType;
        if (inputFileType == InputFileLanguage.Default)
        {
            string inputFileExtension = inputFileInfo.Extension;
            switch (inputFileExtension.ToLower())
            {
                case ".laye": inputFileType = InputFileLanguage.LayeSource; break;
                case ".mod": inputFileType = InputFileLanguage.LayeModule; break;

                default:
                {
                    diag.Error($"File extension '{inputFileExtension}' not recognized; use `--file-kind <kind> to manually specify.");
                    inputFileType = InputFileLanguage.LayeModule;
                } break;
            }
        }

        if (inputFileType == InputFileLanguage.LayeSource)
            ModuleSourceFiles.Add(inputFileInfo);
        else
        {
            Debug.Assert(inputFileType == InputFileLanguage.LayeModule);
            BinaryDependencyFiles.Add(inputFileInfo);
        }
    }

    protected override void HandleArgument(string arg, DiagnosticWriter diag,
        CliArgumentIterator args, TArgParseState state)
    {
        switch (arg)
        {
            default: base.HandleArgument(arg, diag, args, state); break;

            case "--file-kind":
            {
                if (!args.Shift(out string? fileKind))
                    diag.Error($"Argument to '{arg}' is missing; expected 1 value.");
                else
                {
                    switch (fileKind)
                    {
                        default: diag.Error($"File kind '{fileKind}' not recognized."); break;

                        case "laye": state.CurrentFileType = InputFileLanguage.LayeSource; break;
                        case "module": state.CurrentFileType = InputFileLanguage.LayeModule; break;
                    }
                }
            } break;
        }
    }
}
