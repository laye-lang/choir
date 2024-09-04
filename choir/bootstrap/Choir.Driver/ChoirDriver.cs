using System.Diagnostics;
using Choir.CommandLine;
using Choir.Front.Laye;
using Choir.Front.Laye.Codegen;
using Choir.Front.Laye.Sema;
using Choir.Front.Laye.Syntax;
using Choir.IR;
using Choir.Qbe;

namespace Choir;

public enum ChoirDriverStage
{
    Preprocess,
    Lex,
    Parse,
    Sema,
    Codegen,
    Compile,
    Assemble,
    Link,
}

public enum ChoirAssemblerFormat
{
    Choir,
    QBE,
    LLVM,
}

[Flags]
public enum InputFileLanguage
{
    Default = 0,

    Laye = 1 << 0,
    Choir = 1 << 1,
    C = 1 << 2,
    CXX = 1 << 3,
    ObjC = 1 << 4,
    ObjCXX = 1 << 5,
    Assembler = 1 << 6,
    Object = 1 << 7,

    Header = 1 << 24,
    NoPreprocess = 1 << 25,
}

public readonly struct InputFileInfo(InputFileLanguage language, FileInfo fileInfo)
{
    public readonly InputFileLanguage Language = language;
    public readonly FileInfo FileInfo = fileInfo;
}

public abstract class ChoirJob(ChoirDriver driver)
{
    public ChoirDriver Driver { get; } = driver;
    public ChoirContext Context { get; } = driver.Context;

    public abstract int Run();

    public class CompileLayeTranslationUnit(ChoirDriver driver, FileInfo[] layeFiles) : ChoirJob(driver)
    {
        private readonly FileInfo[] _layeFiles = layeFiles;

        public override int Run()
        {
            var layeFiles = _layeFiles
                .Select(Context.GetSourceFile)
                .DistinctBy(sourceFile => sourceFile.FileId)
                .ToArray();
            
            var tu = new TranslationUnit(Context);

            foreach (var file in layeFiles)
            {
                var module = new Module(file);
                tu.AddModule(module);

                Lexer.ReadTokens(module);
                if (Driver.Options.DriverStage != ChoirDriverStage.Lex)
                    Parser.ParseSyntax(module);
            }

            Context.Diag.Flush();

            if (Driver.Options.DriverStage == ChoirDriverStage.Lex)
            {
                if (Driver.Options.PrintTokens)
                {
                    var printer = new SyntaxPrinter(Context, Driver.Options.PrintFileScopes);
                    foreach (var module in tu.Modules)
                    {
                        if (Driver.Options.PrintCliFilesOnly && !Driver.Options.InputFiles.Any(f => module.SourceFile.FileInfo == f.FileInfo))
                            continue;
                        printer.PrintModuleTokens(module);
                    }
                }
                
                return 0;
            }

            if (Driver.Options.DriverStage == ChoirDriverStage.Parse)
            {
                if (Driver.Options.PrintAst)
                {
                    var syntaxPrinter = new SyntaxPrinter(Context, Driver.Options.PrintFileScopes);
                    foreach (var module in tu.Modules)
                    {
                        if (Driver.Options.PrintCliFilesOnly && !Driver.Options.InputFiles.Any(f => module.SourceFile.FileInfo == f.FileInfo))
                            continue;
                        if (module.HasSyntax)
                            syntaxPrinter.PrintModuleSyntax(module);
                        else syntaxPrinter.PrintModuleHeader(module);
                    }
                }

                return 0;
            }

            if (Context.HasIssuedError) return 1;

            Sema.Analyse(tu);
            Context.Diag.Flush();

            if (Driver.Options.DriverStage == ChoirDriverStage.Sema)
            {
                if (Driver.Options.PrintAst)
                {
                    var syntaxPrinter = new SyntaxPrinter(Context, Driver.Options.PrintFileScopes);
                    var semaPrinter = new SemaPrinter(Context, Driver.Options.PrintFileScopes);
                    foreach (var module in tu.Modules)
                    {
                        if (Driver.Options.PrintCliFilesOnly && !Driver.Options.InputFiles.Any(f => module.SourceFile.FileInfo == f.FileInfo))
                            continue;
                        if (module.HasSemaDecls)
                            semaPrinter.PrintModule(module);
                        else if (module.HasSyntax)
                            syntaxPrinter.PrintModuleSyntax(module);
                        else semaPrinter.PrintModuleHeader(module);
                    }
                }

                return 0;
            }

            if (Context.HasIssuedError) return 1;

            LayeCodegen.GenerateIR(tu);
            Context.Diag.Flush();

            if (Driver.Options.DriverStage == ChoirDriverStage.Codegen)
            {
                if (Driver.Options.PrintIR)
                {
                    foreach (var module in tu.Modules)
                    {
                        if (module.ChoirModule is not null)
                            Console.WriteLine(module.ChoirModule.ToIRString());
                        else Console.WriteLine($"<no Choir module for '{module.SourceFile.FileInfo.FullName}'>");
                    }
                }

                return 0;
            }

            if (Context.HasIssuedError) return 1;

            Module[] compilationModules;
            if (Driver.Options.OutputFile == "-")
            {
                Debug.Assert(Driver.Options.InputFiles.Count == 1, "exactly one file should have been specified when requesting output to stdout.");
                Debug.Assert(Driver.Options.DriverStage == ChoirDriverStage.Compile, "when specifying output to stdout, -S (the 'compile only' flag) should have been set.");
                var firstModule = tu.Modules.First();
                Debug.Assert(firstModule.ChoirModule is not null);
                compilationModules = [firstModule];
            }
            else
            {
                Debug.Assert(tu.Modules.All(m => m.ChoirModule is not null));
                compilationModules = tu.Modules.ToArray();
            }

            switch (Driver.Options.AssemblerFormat)
            {
                case ChoirAssemblerFormat.Choir:
                case ChoirAssemblerFormat.QBE:
                    break;
                default:
                {
                    Context.Assert(false, $"Unsupported assembler format: {Driver.Options.AssemblerFormat}");
                    throw new UnreachableException();
                }
            }

            var intermediateFiles = new List<FileInfo>();
            try
            {
                foreach (var module in compilationModules)
                {
                    var cm = module.ChoirModule!;

                    TextWriter outputFileWriter;
                    if (Driver.Options.OutputFile == "-")
                    {
                        // remember: if output file == "-", this is the last stage; we can write directly to it
                        outputFileWriter = Console.Out;
                    }
                    else if (Driver.Options.DriverStage == ChoirDriverStage.Compile)
                    {
                        if (Driver.Options.OutputFile is not null)
                        {
                            outputFileWriter = new StreamWriter(Driver.Options.OutputFile);
                        }
                        else
                        {
                            var aoutFileInfo = new FileInfo("a.out");
                            outputFileWriter = new StreamWriter(aoutFileInfo.FullName);
                        }
                    }
                    else
                    {
                        if (Driver.Options.AssemblerFormat == ChoirAssemblerFormat.Choir)
                        {
                            var choirOutputFileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-" + Path.GetFileNameWithoutExtension(module.SourceFile.FileInfo.Name) + ".choir"));
                            intermediateFiles.Add(choirOutputFileInfo);
                            outputFileWriter = new StreamWriter(choirOutputFileInfo.FullName);
                        }
                        else if (Driver.Options.AssemblerFormat == ChoirAssemblerFormat.QBE)
                        {
                            var qbeOutputFileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-" + Path.GetFileNameWithoutExtension(module.SourceFile.FileInfo.Name) + ".ssa"));
                            intermediateFiles.Add(qbeOutputFileInfo);
                            outputFileWriter = new StreamWriter(qbeOutputFileInfo.FullName);
                        }
                        else throw new UnreachableException();
                    }

                    if (Driver.Options.AssemblerFormat == ChoirAssemblerFormat.Choir)
                        outputFileWriter.Write(cm.ToIRString());
                    else if (Driver.Options.AssemblerFormat == ChoirAssemblerFormat.QBE)
                        outputFileWriter.Write(cm.ToQbeString().ReplaceLineEndings("\n"));
                    else throw new UnreachableException();

                    outputFileWriter.Flush();
                    outputFileWriter.Close();
                }

                if (Driver.Options.DriverStage == ChoirDriverStage.Compile)
                {
                    Debug.Assert(intermediateFiles.Count == 0, "expected to not track output files when the compile only flag is set. they won't need to be deleted");
                    return 0;
                }
            }
            finally
            {
                foreach (var intermediateFile in intermediateFiles)
                {
                    intermediateFile.Delete();
                }
            }

            Context.Diag.ICE("Going any farther than compilation is not yet supported");
            return 0;
        }
    }
}

public sealed class ChoirDriver
{
    public static ChoirDriver Create(DiagnosticWriter diag, ChoirDriverOptions options)
    {
        if (options.InputFiles.Any(inputFile => inputFile.Language == InputFileLanguage.Default))
        {
            diag.ICE("One or more input files did not get assigned a language before constructing the Choir driver.");
            throw new UnreachableException();
        }

        return new ChoirDriver(options);
    }

    public ChoirDriverOptions Options { get; }
    public ChoirContext Context { get; }

    private ChoirDriver(ChoirDriverOptions options)
    {
        Options = options;
        Context = new(ChoirTarget.X86_64, options.OutputColoring)
        {
            IncludeDirectories = options.IncludeDirectories,
            LibraryDirectories = options.LibraryDirectories,
        };
    }

    public int Execute()
    {
        try
        {
            var jobs = new List<ChoirJob>();

            var layeFileInfos = Options.InputFiles.Where(inputFile => inputFile.Language == InputFileLanguage.Laye);
            if (layeFileInfos.Any())
            {
                var layeFiles = layeFileInfos.Select(inputFile => inputFile.FileInfo).ToArray();
                var layeJob = new ChoirJob.CompileLayeTranslationUnit(this, layeFiles);
                jobs.Add(layeJob);
            }

            if (Context.HasIssuedError)
                return 1;

            foreach (var job in jobs)
            {
                int result = job.Run();
                if (result != 0)
                    return result;
            }

            return 0;
        }
        finally
        {
            Context.Diag.Flush();
        }
    }
}

public sealed class ChoirDriverOptions
{
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
    public bool ShowVerboseOutput { get; set; }
    public bool QuoteAndDoNotExecute { get; set; }

    public List<InputFileInfo> InputFiles { get; set; } = [];
    public string? OutputFile { get; set; }
    public ChoirDriverStage DriverStage { get; set; } = ChoirDriverStage.Link;
    public ChoirAssemblerFormat AssemblerFormat { get; set; } = ChoirAssemblerFormat.QBE;
    public bool PrintTokens { get; set; }
    public bool PrintAst { get; set; }
    public bool PrintIR { get; set; }
    public bool PrintFileScopes { get; set; }
    public bool PrintCliFilesOnly { get; set; }
    public bool OutputColoring { get; set; }
    public List<string> IncludeDirectories { get; set; } = [];
    public List<string> LibraryDirectories { get; set; } = [];

    #region Linker Options

    public List<string> LinkLibraries { get; set; } = [];
    public string? EntryName { get; set; }
    public bool NoStartFiles { get; set; }
    public bool NoStandardLibrary { get; set; }
    public bool NoLibC { get; set; }

    #endregion
}
