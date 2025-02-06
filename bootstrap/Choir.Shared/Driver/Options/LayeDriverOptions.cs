using System.Diagnostics;

using Choir.CommandLine;

namespace Choir.Driver.Options;

public record class LayeDriverOptions
    : BaseLayeDriverOptions<LayeDriverOptions, BaseLayeCompilerDriverArgParseState>
{
    /// <summary>
    /// <para>
    /// While the `laye` compiler driver expects to operate on module *directories*, it still accepts source files.
    /// `laye` will parse the module headers of all module source files and group them based on their module declaration.
    /// This is the same process which is done for the contents of module directories, except that all files within a specified directory are required to be of the same module.
    /// This restriction is not present on the list of input files.
    /// </para>
    /// <para>
    /// If the module declaration of an input file conflicts with those of files discovered in module directories, an error will be emitted.
    /// </para>
    /// </summary>
    public List<FileInfo> AdditionalSourceFiles { get; } = [];

    /// <summary>
    /// <para>
    /// Laye module directories to compile.
    /// </para>
    /// <para>
    /// When a Laye source file imports a module by name, the compiler drivers will attempt to find a `.mod` file with that name adjacent to the module or in the library search path.
    /// In the case of the `laye` tool specificly, it will also search for directories with that name which contain Laye source files declaring that module and add the module directory to the build process if found.
    /// </para>
    /// <para>
    /// A module directory given this way is also implicitly added to the library search path for the purposes of identifying source modules only.
    /// </para>
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

    /// <summary>
    /// The `--no-rt0` flag.
    /// Disables linking to the Laye runtime/entry library, requiring the programmer to provide their own implementation.
    /// </summary>
    public bool NoRuntimeEntry { get; set; }

    /// <summary>
    /// Specifies the linker to use.
    /// This should be directly executable by the process and accept arguments as expected by the common linkers supported by the driver.
    /// </summary>
    public string? Linker { get; set; }

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
            AdditionalSourceFiles.Add(inputFileInfo);
            // diag.Error($"Laye source files ('{value}') are not accepted by the Laye build tool/compiler driver.");
            // diag.Note("To compile Laye with this tool, pass 0 or more directory paths containing Laye source files;\neach directory represents a Laye module, a collection of its immediate child source files.");
            // diag.Note("If no directory paths are given as input, this tool will search for some common directories automatically.");
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
            case "--no-rt0": NoRuntimeEntry = true; break;

            case "-L":
            {
                if (!args.Shift(out string? libDir) || libDir.IsNullOrEmpty())
                    diag.Error($"Argument to '{arg}' is missing; expected 1 (non-empty) value.");
                else LibrarySearchPaths.Add(new DirectoryInfo(libDir));
            } break;

            case "--linker":
            {
                if (!args.Shift(out string? linker) || linker.IsNullOrEmpty())
                    diag.Error($"Argument to '{arg}' is missing; expected 1 (non-empty) value.");
                else Linker = linker;
            } break;
        }
    }
}
