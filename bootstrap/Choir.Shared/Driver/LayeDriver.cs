using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

using Choir.CommandLine;
using Choir.Driver.Options;
using Choir.Front.Laye;

namespace Choir.Driver;

public sealed class LayeDriver
    : BaseLayeDriver<LayeDriverOptions, BaseLayeCompilerDriverArgParseState>
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
            return 1;

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

        return exitCode;
    }

    public static LayeDriver Create(DiagnosticWriter diag, LayeDriverOptions options, string programName = "laye")
    {
        return new LayeDriver(programName, diag, options);
    }

    private LayeDriver(string programName, DiagnosticWriter diag, LayeDriverOptions options)
        : base(programName, diag, options)
    {
    }

    public override int Execute()
    {
        bool isOutputStdout = Options.OutputFilePath == "-";

        DirectoryInfo[] allLibrarySearchPaths = [.. CollectBuiltInLibrarySearchPaths(), .. Options.LibrarySearchPaths];
        Options.LibrarySearchPaths.Clear();
        Options.LibrarySearchPaths.AddRange(allLibrarySearchPaths.Where(dir => dir.Exists).Select(d => d.Canonical()).Distinct());

        #region Implicit Module Selection

        if (Options.AdditionalSourceFiles.Count == 0 && Options.ModuleDirectories.Count == 0 && Options.BinaryDependencyFiles.Count == 0)
        {
            Context.LogVerbose("Searching for a suitable default module source directory, since no input files were provided.");

            string[] defaultModuleDirectories = ((string[]) [ "src", "source", "lib", "library", "mod", "module", ..(Environment.GetEnvironmentVariable("LAYE_DIR_DEFAULT_MODULE")?.Split(Path.PathSeparator) ?? []) ])
                .Distinct().ToArray();
            foreach (string dirName in defaultModuleDirectories)
            {
                var directoryInfo = new DirectoryInfo(dirName);
                if (!directoryInfo.Exists)
                    continue;

                var entities = directoryInfo.EnumerateFiles();
                if (!entities.Any(f => f.Extension == ".laye"))
                    continue;

                Options.ModuleDirectories.Add(directoryInfo);
                break;
            }

            if (Options.ModuleDirectories.Count == 0)
            {
                Context.Diag.Error("No input files or module directories.");
                Context.Diag.Note($"'{ProgramName}' checks the following directories for '.laye' source files if you don't provide any explicitly and uses the first one it finds:\n  {string.Join(", ", defaultModuleDirectories)}");
                Context.Diag.Note($"Try creating a 'main.laye' file in this directory, then re-running '{ProgramName}'.");
                return 1;
            }
        }

        #endregion

        Context.LogVerbose("Input source files:");
        foreach (var file in Options.AdditionalSourceFiles)
            Context.LogVerbose($"  {file.FullName}");

        Context.LogVerbose("Input module directories:");
        foreach (var dir in Options.ModuleDirectories)
            Context.LogVerbose($"  {dir.FullName}");

        Context.LogVerbose("Input module binaries:");
        foreach (var file in Options.BinaryDependencyFiles)
            Context.LogVerbose($"  {file.FullName}");

        #region Module Resolution

        string[] sourceModuleDirPaths = ((string[])["third-party", "third_party", "vendor", .. (Environment.GetEnvironmentVariable("LAYE_DIR_VENDOR")?.Split(Path.PathSeparator) ?? [])])
                .Distinct().ToArray();

        List<DirectoryInfo> additionalSourceDirs = [.. sourceModuleDirPaths.Select(path => new DirectoryInfo(path))];
        if (Options.ModuleDirectories.Count == 0 && Options.AdditionalSourceFiles.Count != 0)
            additionalSourceDirs.AddRange(new DirectoryInfo(".").EnumerateDirectories());

        SourceModuleInfo[] fileListModules = CreateFileListModules(Options.AdditionalSourceFiles);
        var modules = ResolveModuleDependencyOrder(Options.ModuleDirectories, additionalSourceDirs, fileListModules, Options.BinaryDependencyFiles, Options.LibrarySearchPaths);

        if (Context.HasIssuedError)
            return 1;

        #endregion

        string[] foreignLinkLibraries = modules.SelectMany(m => m.ForeignLibraryNames).ToArray();

        Context.LogVerbose("Resolved modules, sorted:");
        foreach (var module in modules)
            Context.LogVerbose($"  {module}");

        Context.LogVerbose("Foreign link libraries:");
        foreach (string linkLibrary in foreignLinkLibraries)
            Context.LogVerbose($"  {linkLibrary}");

        // do some sanity checking on outputting to stdout
        if (Options.OutputFilePath == "-")
        {
            var presentSourceModulesForCompilation = modules.Where(m => m is SourceModuleInfo).ToArray();
            int compilationModuleCount = presentSourceModulesForCompilation.Length;

            // we don't error for this case if no output files will be generated anyway. Maybe we warn instead?
            if (Options.DriverStage >= DriverStage.Compile && Options.DriverStage != DriverStage.Link && compilationModuleCount != 1)
            {
                Context.Diag.Error($"Cannot output to stdout (the '-o -' option) with the current compiler options.");
                Context.Diag.Note($"Can only output to stdout when either:\n  a) linking an executable, or\n  b) compiling a single source module.");
                
                string presentSourceModulesForCompilationDesc = string.Join("\n", presentSourceModulesForCompilation.Select(m => $"  {m}"));
                Context.Diag.Note($"You requested for {compilationModuleCount} source modules to be compiled:\n{presentSourceModulesForCompilationDesc}");

                if (!Context.EmitVerboseLogs)
                    Context.Diag.Note($"To see the modules being referred to by the compiler, add the '--verbose' command line argument.");

                return 1;
            }
        }

        var compilationArtifacts = new List<FileInfo>();
        var linkerInputs = new List<FileInfo>();
        var moduleInfosToResultFiles = new Dictionary<ModuleInfo, FileInfo>();

        bool isSingleSourceModule = modules.Where(m => m is SourceModuleInfo).Count() == 1;

        #region Construct Compiler Calls

        bool hasErroredWhenCompilingModules = false;

        foreach (var module in modules)
        {
            if (module is BinaryModuleInfo binaryModule)
            {
                linkerInputs.Add(binaryModule.ModuleFile);
                moduleInfosToResultFiles[binaryModule] = binaryModule.ModuleFile;
            }
            else if (module is SourceModuleInfo sourceModule)
            {
                FileInfo? moduleOutputFile = null;

                bool hasOutputFiles = Options.DriverStage >= DriverStage.Compile;
                bool emitModuleToStdout = hasOutputFiles && Options.DriverStage != DriverStage.Link && isOutputStdout && isSingleSourceModule;

                if (hasOutputFiles)
                {
                    if (Options.DriverStage == DriverStage.Link || emitModuleToStdout)
                        moduleOutputFile = new DirectoryInfo(Path.GetTempPath()).ChildFile($"{module.ModuleName}-{Path.GetRandomFileName()}.mod");
                    else moduleOutputFile = new DirectoryInfo(".").ChildFile($"{module.ModuleName}.mod");
                }

                var layecOptions = new LayecHighLevelDriverOptions()
                {
                    // layec can't link, it doesn't make sense to pass that (or let it get rendered)
                    DriverStage = (DriverStage)Math.Min((int)DriverStage.Assemble, (int)Options.DriverStage),
                    OmitSourceTextInModuleBinary = Options.OmitSourceTextInModuleBinary,
                    NoLower = Options.NoLower,
                    ShowVerboseOutput = Options.ShowVerboseOutput,
                    AssemblerFormat = Options.AssemblerFormat,
                    IsDistribution = Options.IsDistribution,
                    OutputColoring = Options.OutputColoring,
                    PrintTokens = Options.PrintTokens,
                    PrintAst = Options.PrintAst,
                    PrintIR = Options.PrintIR,
                };

                if (moduleOutputFile is not null)
                {
                    Context.LogVerbose($"Outputting module '{module.ModuleName}' to '{moduleOutputFile.FullName}'.");
                    layecOptions.OutputFilePath = moduleOutputFile.FullName;
                    linkerInputs.Add(moduleOutputFile);
                    compilationArtifacts.Add(moduleOutputFile);
                    moduleInfosToResultFiles[sourceModule] = moduleOutputFile;
                }
                else if (Options.OutputFilePath == "-")
                    layecOptions.OutputFilePath = "-";

                layecOptions.ModuleSourceFiles.AddRange(sourceModule.ModuleFiles);

                var binaryDependencies = modules
                    .Where(m => sourceModule.DependencyNames.Contains(m.ModuleName))
                    .Select(m => moduleInfosToResultFiles[m]);
                layecOptions.BinaryDependencyFiles.AddRange(binaryDependencies);

                var layecDriver = LayecHighLevelDriver.Create(Context, layecOptions);
                int layecExitCode = layecDriver.Execute();

                if (layecExitCode != 0)
                    hasErroredWhenCompilingModules = true;
                else
                {
                    if (emitModuleToStdout)
                        WriteFileToStdout(moduleOutputFile!.FullName);
                }
            }
            else
            {
                Context.Diag.ICE($"unknown module info type: {module.GetType()}");
                throw new UnreachableException();
            }
        }

        if (hasErroredWhenCompilingModules)
        {
            Context.LogVerbose("Failed to compile one or more modules with the 'layec' module compiler.");
            return 1;
        }

        #endregion

        #region Linking

        if (Options.DriverStage == DriverStage.Link)
        {
            var runtimeModule = FindBinaryModule("rt0", Options.LibrarySearchPaths);
            if (runtimeModule is null)
            {
                Context.Diag.Error($"Could not find Laye runtime library (rt0.mod) on the system; cannot link into an executable.");
                return 1;
            }

            linkerInputs.Add(runtimeModule.ModuleFile);

            FileInfo linker;
            if (Options.Linker is string linkerPath)
                linker = new(linkerPath);
            else
            {
                var maybeLinker = IdentifyPlatformLinker();
                if (maybeLinker is null)
                {
                    Context.Diag.Error("Failed to find a linker on this platform. Check your PATH environment variable or specify one with the `--linker` option.");
                    return 1;
                }

                linker = maybeLinker;
            }

            var linkerSyntax = DetermineArgumentSyntaxFlavor(linker);

            var linkerStartInfo = new ProcessStartInfo()
            {
                FileName = linker.FullName,
            };

            string outputFilePath;
            if (Options.OutputFilePath is not null)
            {
                outputFilePath = Options.OutputFilePath;
                Context.LogVerbose("Using input file path.");
            }
            else
            {
                string exeSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";
                string outSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ".out";
                
                var programModule = modules.Where(m => m.ModuleName == LayeConstants.ProgramModuleName).SingleOrDefault();
                if (programModule is SourceModuleInfo { } programSourceModule)
                {
                    if (programSourceModule.ModuleFiles.Length == 1)
                        outputFilePath = $"{Path.GetFileNameWithoutExtension(programSourceModule.ModuleFiles[0].Name)}{exeSuffix}";
                    else outputFilePath = $"program{exeSuffix}";
                }
                else outputFilePath = $"a{outSuffix}";

                Context.LogVerbose($"Setting assumed output file to '{outputFilePath}'.");
            }

            if (isOutputStdout)
            {
                outputFilePath = Path.GetTempFileName();
                Context.LogVerbose($"Setting linker output to a temp file: '{outputFilePath}'.");
            }

            if (linkerSyntax == ExternalArgumentSyntaxFlavor.MSVC)
            {
                linkerStartInfo.ArgumentList.Add($"/nologo");
                linkerStartInfo.ArgumentList.Add($"/out:{outputFilePath}");
                //linkerStartInfo.ArgumentList.Add($"/defaultlib:libcmt");
                linkerStartInfo.ArgumentList.Add("/subsystem:console");
                linkerStartInfo.ArgumentList.Add("kernel32.lib");
                linkerStartInfo.ArgumentList.Add("legacy_stdio_definitions.lib");
                linkerStartInfo.ArgumentList.Add("msvcrt.lib");
            }
            else
            {
                linkerStartInfo.ArgumentList.Add($"-o{outputFilePath}");
            }

            foreach (var input in linkerInputs)
            {
                linkerStartInfo.ArgumentList.Add($"{input.FullName}");
            }

            foreach (string linkLibrary in foreignLinkLibraries)
            {
                linkerStartInfo.ArgumentList.Add(linkLibrary);
            }

            Context.LogVerbose($"{linker.Name} {string.Join(" ", linkerStartInfo.ArgumentList.Select(a => $"\"{a}\""))}");
            var linkerProcess = Process.Start(linkerStartInfo);
            if (linkerProcess is null)
            {
                Context.Diag.ICE($"Failed to start linker process: '{linker.FullName}'.");
                return 1;
            }

            linkerProcess.WaitForExit();
            int linkerExitCode = linkerProcess.ExitCode;

            if (linkerExitCode != 0)
            {
                Context.Diag.Error($"Linker process failed with exit code {linkerExitCode}.");
                return 1;
            }

            if (isOutputStdout)
            {
                WriteFileToStdout(outputFilePath);
            }
            else
            {
                Context.Assert(File.Exists(outputFilePath), "I feel like we should have caught that the file wasn't generated yet.");
            }
        }

        #endregion

        // Context.Todo("Finish implementing the laye driver.");
        return 0;

        static void WriteFileToStdout(string filePath)
        {
            using var fileStream = File.OpenRead(filePath);
            using var stdout = Console.OpenStandardOutput();

            byte[] buffer = new byte[1024 * 1024];
            int nRead;
            while ((nRead = fileStream.Read(buffer)) != 0)
                stdout.Write(buffer.AsSpan(0, nRead));
        }
    }
}
