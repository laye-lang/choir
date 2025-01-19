using System.Diagnostics;

using Choir.CommandLine;

namespace Choir.Driver.Options;

public record class LayeDriverOptions
    : BaseLayeDriverOptions<LayeDriverOptions, BaseLayeCompilerDriverArgParseState>
{
    /// <summary>
    /// Laye module directories to compile.
    /// If no directories are provided, then one of 'src', 'source' or '.' are implied, whichever is found first and also
    /// contains Laye source files.
    /// This means a call to the 'laye' driver with no explicit directories specified will try to be smart based on
    /// common conventions.
    /// </summary>
    public List<DirectoryInfo> ModuleDirectories { get; } = [];

    /// <summary>
    /// Additional library search directories.
    /// Library search directories are searched in the order they are specified.
    /// Any built-in or environment-specified library search paths are prepended to this list in the order they are identified.
    /// 
    /// The `laye` driver will search for both Laye modules and foreign libraries in these directories.
    /// Laye modules will be used in the `layec` driver to perform semantic analysis and code generation.
    /// Both kinds of libraries will be passed to the linker to generate the final linked binary by the `laye` driver.
    /// </summary>
    public List<DirectoryInfo> LibrarySearchPaths { get; } = [];

    /// <summary>
    /// The `--no-corelib` flag.
    /// Disables linking to the Laye core library, requiring the programmer to provide their own implementation.
    /// </summary>
    public bool NoCoreLibrary { get; set; }

    protected override void HandleValue(string value, DiagnosticWriter diag,
        CliArgumentIterator args, BaseLayeCompilerDriverArgParseState state)
    {
        var inputDirectoryInfo = new DirectoryInfo(value);
        if (inputDirectoryInfo.Exists)
        {
            ModuleDirectories.Add(inputDirectoryInfo);
            return;
        }

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
        {
            diag.Error($"Laye source files ('{value}') are not accepted by the Laye build tool/compiler driver.");
            diag.Note("To compile Laye with this tool, pass 0 or more directory paths containing Laye source files;\neach directory represents a Laye module, a collection of its immediate child source files.");
            diag.Note("If no directory paths are given as input, this tool will search for some common directories automatically.");
        }
        else
        {
            Debug.Assert(inputFileType == InputFileLanguage.LayeModule);
            BinaryDependencyFiles.Add(inputFileInfo);
        }
    }

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
