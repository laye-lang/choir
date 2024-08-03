using System.Diagnostics;

namespace Choir;

public enum ChoirDriverStage
{
    Lex,
    Parse,
    Sema,
    Compile,
}

public enum InputFileLanguage
{
    Default,
    Laye,
    C,
    CPP,
    Choir,
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
        Context = new(options.OutputColoring);
    }

    public int Execute()
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

        return 0;
    }
}

public sealed class ChoirDriverOptions
{
    public List<InputFileInfo> InputFiles { get; set; } = [];
    public ChoirDriverStage DriverStage { get; set; } = ChoirDriverStage.Compile;
    public bool OutputColoring { get; set; }
}
