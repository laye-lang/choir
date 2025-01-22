using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Choir.Driver.Options;
using Choir.Front.Laye;
using Choir.Front.Laye.Syntax;

namespace Choir.Driver;

public abstract class BaseLayeDriver<TOptions, TArgParseState>
    where TOptions : BaseLayeDriverOptions<TOptions, TArgParseState>, new()
    where TArgParseState : BaseLayeCompilerDriverArgParseState, new()
{
    public string ProgramName { get; }
    public ChoirContext Context { get; }

    public TOptions Options { get; }

    protected BaseLayeDriver(string programName, DiagnosticWriter diag, TOptions options)
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

    protected abstract class ModuleInfo(string moduleName, IReadOnlyList<string> dependencyNames)
    {
        public readonly string ModuleName = moduleName;
        public readonly string[] DependencyNames = [.. dependencyNames];
        public abstract string LocationPath { get; }
        public override string ToString() => $"{ModuleName} [{string.Join(", ", DependencyNames)}] @ '{LocationPath}'";
    }

    protected class SourceModuleInfo(DirectoryInfo? moduleDir, FileInfo[] moduleFiles, string moduleName, IReadOnlyList<string> dependencyNames)
        : ModuleInfo(moduleName, dependencyNames)
    {
        public readonly DirectoryInfo? ModuleDirectory = moduleDir;
        public readonly FileInfo[] ModuleFiles = moduleFiles;
        public override string LocationPath { get; } = moduleDir?.FullName ?? string.Join(Path.PathSeparator, moduleFiles.Select(f => f.FullName));
        public override bool Equals(object? obj) => obj is SourceModuleInfo other && other.ModuleName == ModuleName && other.DependencyNames.SequenceEqual(DependencyNames) && other.ModuleDirectory == ModuleDirectory && other.ModuleDirectory == ModuleDirectory;
        public override int GetHashCode() => HashCode.Combine(ModuleName, DependencyNames, ModuleDirectory, ModuleFiles);
    }

    protected sealed class BinaryModuleInfo(FileInfo moduleFile, string moduleName, IReadOnlyList<string> dependencyNames)
        : ModuleInfo(moduleName, dependencyNames)
    {
        public readonly FileInfo ModuleFile = moduleFile;
        public override string LocationPath { get; } = moduleFile.FullName;
        public override bool Equals(object? obj) => obj is BinaryModuleInfo other && other.ModuleName == ModuleName && other.DependencyNames.SequenceEqual(DependencyNames) && other.ModuleFile == ModuleFile;
        public override int GetHashCode() => HashCode.Combine(ModuleName, DependencyNames, ModuleFile);
    }

    protected BinaryModuleInfo? FindBinaryModule(string libraryName, IReadOnlyList<DirectoryInfo> librarySearchDirs)
    {
        string libraryFileName = $"{libraryName}.mod";
        foreach (var libDir in librarySearchDirs)
        {
            var modFile = libDir.ChildFile(libraryFileName);
            if (!modFile.Exists) continue;

            var header = LayeModule.DeserializeHeaderFromObject(Context, modFile);
            return new BinaryModuleInfo(modFile, header.ModuleName, header.DependencyNames);
        }

        Context.Diag.Error($"Could not find Laye module file '{libraryFileName}'.");
        return null;
    }

    protected SourceModuleInfo[] CreateFileListModules(IReadOnlyList<FileInfo> inputFiles)
    {
        if (inputFiles.Count == 0) return [];

        var sourceFiles = inputFiles.Select(Context.GetSourceFile);
        var sourceHeaders = sourceFiles.Select(f => (File: f, Header: Parser.ParseModuleUnitHeader(f)));

        var groups = sourceHeaders.GroupBy(pair => pair.Header.ModuleName ?? LayeConstants.ProgramModuleName)
            .Select(g => new SourceModuleInfo(null, g.Select(pair => new FileInfo(pair.File.FilePath)).ToArray(), g.Key, g.SelectMany(pair => pair.Header.ImportDeclarations.Select(import => import.ModuleNameText)).ToArray()));

        return [.. groups];
    }

    protected SourceModuleInfo[] CollectAvailableSourceModules(IReadOnlyList<DirectoryInfo> inputDirs)
    {
        if (inputDirs.Count == 0) return [];

        var seen = new HashSet<string>();
        var queue = new Queue<DirectoryInfo>();

        void Enqueue(DirectoryInfo dir)
        {
            if (seen.Contains(dir.FullName)) return;
            seen.Add(dir.FullName);
            queue.Enqueue(dir);
        }

        foreach (var input in inputDirs.Select(d => d.Canonical()).Distinct())
            Enqueue(input);

        var result = new HashSet<SourceModuleInfo>();

        while (queue.TryDequeue(out var dir))
        {
            if (!dir.Exists) continue;

            var childDirs = dir.EnumerateDirectories();
            foreach (var child in childDirs)
                Enqueue(child);

            // this is only a valid module directory if it contains .laye files
            var childLayeFiles = dir.EnumerateFiles()
                .Where(f => f.Extension.Equals(".laye", StringComparison.CurrentCultureIgnoreCase));
            if (!childLayeFiles.Any())
                continue;

            var sourceFiles = childLayeFiles.Select(Context.GetSourceFile);
            var sourceHeaders = sourceFiles.Select(Parser.ParseModuleUnitHeader);

            string sourceModuleName = sourceHeaders.FirstOrDefault()?.ModuleName ?? LayeConstants.ProgramModuleName;

            string[] declaredModuleNames = sourceHeaders
                .Select(h => h.ModuleName ?? LayeConstants.ProgramModuleName)
                .Distinct().ToArray();
            if (declaredModuleNames.Length != 1)
            {
                Context.Diag.Error($"Source module at '{dir.FullName}' declared multiple modules in its source.");
                Context.Diag.Note($"The following module names were declared:\n  {string.Join(", ", declaredModuleNames.Order())}");
                if (sourceHeaders.Any(h => h.ModuleName is null))
                    Context.Diag.Note("Note that the '.program' module name is implicitly declared if no 'module' declaration is present. This special module is intended only for use as an executable entry point module, and cannot be depended on.");
                continue;
            }

            string[] dependencies = sourceHeaders
                .SelectMany(h => h.ImportDeclarations.Select(id => id.ModuleNameText))
                .Distinct().ToArray();
            result.Add(new SourceModuleInfo(dir, [.. childLayeFiles], sourceModuleName, dependencies));
        }

        return [.. result];
    }

    protected ModuleInfo[] ResolveModuleDependencyOrder(IReadOnlyList<DirectoryInfo> inputDirs, IReadOnlyList<DirectoryInfo> additionalSourceDirs,
        IReadOnlyList<SourceModuleInfo> fileListModules, IReadOnlyList<FileInfo> binaryModules, IReadOnlyList<DirectoryInfo> librarySearchDirs)
    {
        var inputBinaryModuleInfos = new List<BinaryModuleInfo>(binaryModules.Count);
        foreach (var binaryModuleFile in binaryModules)
        {
            var header = LayeModule.DeserializeHeaderFromObject(Context, binaryModuleFile);
            var moduleInfo = new BinaryModuleInfo(binaryModuleFile, header.ModuleName, header.DependencyNames);
            inputBinaryModuleInfos.Add(moduleInfo);
        }

        var availableDirectoryModules = CollectAvailableSourceModules([.. inputDirs, .. additionalSourceDirs]);
        SourceModuleInfo[] totalAvailableSourceModuleArray = [.. availableDirectoryModules, .. fileListModules];
        
        var duplicateAvailableSourceModuleArray = totalAvailableSourceModuleArray
            .GroupBy(m => m.ModuleName).Where(g => g.Count() > 1);
        if (duplicateAvailableSourceModuleArray.Any())
        {
            Context.Diag.Error("Duplicate module declarations found.");
            Context.Diag.Note($"The following duplicate modules were found:");
            foreach (var group in duplicateAvailableSourceModuleArray)
                Context.Diag.Note($"'{group.Key}':\n{string.Join("\n", group.Select(m => $"  {m}"))}");

            if (duplicateAvailableSourceModuleArray.Any(g => g.Any(m => m.ModuleDirectory is null)))
                Context.Diag.Note($"Some of the conflicting modules are a result of passing Laye source files verbatim to the '{ProgramName}' tool.\nThis is a convenience feature and could cause problems if misused.");

            if (!Context.EmitVerboseLogs)
                Context.Diag.Note($"To see the modules being referred to by the compiler, add the '--verbose' command line argument.");
        }

        if (Context.HasIssuedError)
            return [];

        var availableSourceModules = totalAvailableSourceModuleArray.ToDictionary(m => m.ModuleName, m => m);

        Context.LogVerbose("Available source modules:");
        foreach (var (_, sm) in availableSourceModules)
            Context.LogVerbose($"  {sm}");

        var resolvedDependencies = new Dictionary<string, ModuleInfo>();

        void TryAddInputDependency(ModuleInfo moduleInfo)
        {
            if (resolvedDependencies.TryGetValue(moduleInfo.ModuleName, out var conflicting))
            {
                Context.Diag.Error($"A module named '{moduleInfo.ModuleName}' was already declared.");
                Context.Diag.Note($"The Laye module at '{moduleInfo.LocationPath}' declares module '{moduleInfo.ModuleName}' that was already declared at '{conflicting.LocationPath}'.");
                if (!Context.EmitVerboseLogs)
                    Context.Diag.Note($"To see the modules being referred to by the compiler, add the '--verbose' command line argument.");

                return;
            }

            resolvedDependencies[moduleInfo.ModuleName] = moduleInfo;
        }

        // start by only including modules which were provided on the command line explicitly
        foreach (var inputDir in inputDirs)
        {
            var sourceModuleInfo = availableDirectoryModules.Where(sm => sm.ModuleDirectory == inputDir).SingleOrDefault();
            if (sourceModuleInfo is null) continue;

            TryAddInputDependency(sourceModuleInfo);
        }

        foreach (var sourceModuleInfo in fileListModules)
            TryAddInputDependency(sourceModuleInfo);

        foreach (var binaryModuleInfo in inputBinaryModuleInfos)
            TryAddInputDependency(binaryModuleInfo);

        if (Context.HasIssuedError)
            return [];

        var unresolvedDependencies = new Queue<string>();
        void EnqueueDependency(string dependencyName)
        {
            if (!resolvedDependencies.ContainsKey(dependencyName))
                unresolvedDependencies.Enqueue(dependencyName);
        }

        foreach (var (name, info) in resolvedDependencies)
        {
            foreach (string depname in info.DependencyNames)
                EnqueueDependency(depname);
        }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        while (unresolvedDependencies.TryDequeue(out string dependencyName))
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        {
            if (availableSourceModules.TryGetValue(dependencyName, out var sourceModule))
            {
                resolvedDependencies[dependencyName] = sourceModule;
                foreach (string depname in sourceModule.DependencyNames)
                    EnqueueDependency(depname);

                continue;
            }

            if (FindBinaryModule(dependencyName, librarySearchDirs) is { } binaryModule)
            {
                resolvedDependencies[dependencyName] = binaryModule;
                foreach (string depname in binaryModule.DependencyNames)
                    EnqueueDependency(depname);

                continue;
            }

            Context.Diag.Error($"Could not resolve dependency on module '{dependencyName}'.");
        }

        if (Context.HasIssuedError)
            return [];

        List<(string From, string To)> dependencyEdges = [];
        foreach (var (name, info) in resolvedDependencies)
        {
            Context.Assert(name == info.ModuleName, $"Invalid mapping from '{name}' to '{info.ModuleName}'.");
            foreach (string dependencyName in info.DependencyNames)
                dependencyEdges.Add((name, dependencyName));
        }

        var sortResult = TopologicalSort.Sort(resolvedDependencies.Keys, dependencyEdges);
        if (sortResult is TopologicalSortCircular<string> circular)
        {
            Context.Diag.Error($"Detected circular dependencies: from module '{circular.From}' to '{circular.To}'.");
            if (!Context.EmitVerboseLogs)
                Context.Diag.Note($"To see the modules being referred to by the compiler, add the '--verbose' command line argument.");
            return [];
        }

        if (sortResult is not TopologicalSortSuccess<string> sorted)
        {
            Context.Diag.ICE("Only circular references and sorted lists are expected after dependency sorting.");
            throw new UnreachableException();
        }

        return [.. sorted.Sorted.Select(name => resolvedDependencies[name])];
    }

    protected ExternalArgumentSyntaxFlavor DetermineArgumentSyntaxFlavor(FileInfo toolFilePath)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo(toolFilePath.FullName, "--help")
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            var process = System.Diagnostics.Process.Start(startInfo);
            if (process is null) return ExternalArgumentSyntaxFlavor.GCC;

            process.WaitForExit();
            return process.ExitCode == 0 ? ExternalArgumentSyntaxFlavor.GCC : ExternalArgumentSyntaxFlavor.MSVC;
        }
        catch
        {
            return ExternalArgumentSyntaxFlavor.GCC;
        }
    }

    protected FileInfo? IdentifyPlatformLinker()
    {
        string envName = "Path";
        string envSplit = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";
        string[] pathEntries = Environment.GetEnvironmentVariable(envName)?.Split(envSplit) ?? [];

        string[] knownLinkers;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            knownLinkers = ["link.exe", "clang.exe", "gcc.exe"];
        else knownLinkers = ["ld", "clang", "gcc"];

        foreach (string linker in knownLinkers)
        {
            foreach (string path in pathEntries)
            {
                FileInfo check = new(Path.Combine(path, linker));
                if (check.Exists)
                    return check;
            }
        }

        return null;
    }

    protected DirectoryInfo[] CollectBuiltInLibrarySearchPaths()
    {
        var builtInSearchPaths = new List<DirectoryInfo>();

        string envName = "Lib";
        string envSplit = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";
        string[] pathEntries = Environment.GetEnvironmentVariable(envName)?.Split(envSplit) ?? [];

        builtInSearchPaths.AddRange(pathEntries.Where(path => !path.IsNullOrEmpty()).Select(path => new DirectoryInfo(path)));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            builtInSearchPaths.Add(new DirectoryInfo("/usr/local/lib/laye"));
            builtInSearchPaths.Add(new DirectoryInfo("/usr/local/lib"));
            //builtInSearchPaths.Add(new DirectoryInfo($"/usr/lib/{Triple}"));
            builtInSearchPaths.Add(new DirectoryInfo("/usr/lib/laye"));
            builtInSearchPaths.Add(new DirectoryInfo("/usr/lib"));
            //builtInSearchPaths.Add(new DirectoryInfo($"/lib/{Triple}"));
        }

        string selfExePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        DirectoryInfo? searchDirectoryRoot = new DirectoryInfo(Path.GetDirectoryName(selfExePath)!);

        while (searchDirectoryRoot is not null)
        {
            builtInSearchPaths.Add(searchDirectoryRoot.ChildDirectory("lib").ChildDirectory("laye"));
            builtInSearchPaths.Add(searchDirectoryRoot.ChildDirectory("lib"));
            searchDirectoryRoot = searchDirectoryRoot.Parent;
        }

        return [.. builtInSearchPaths];
    }

    public abstract int Execute();
}
