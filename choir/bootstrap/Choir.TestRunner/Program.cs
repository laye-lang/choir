using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using Choir.Driver;

namespace Choir.TestRunner;

public enum TestStatus
{
    NotRun,
    Failed,
    Passed,
}

public abstract record class TestInstance(FileInfo SourceFile)
{
    public TestStatus Status { get; protected set; } = TestStatus.NotRun;
    public abstract string TestName { get; }

    protected abstract void RunTestImpl();
    public void RunTest()
    {
        TestLog.Info($"Running test \"{TestName}\"");
        RunTestImpl();
        Debug.Assert(Status != TestStatus.NotRun);
    }
}

public class TestRunnerInternalCompilerError : Exception { }

public sealed record class ExecTestInstance(DirectoryInfo LibDir, FileInfo SourceFile) : TestInstance(SourceFile)
{
    public override string TestName => $"Execute {SourceFile.Name}";

    protected override void RunTestImpl()
    {
        var diag = new StreamingDiagnosticWriter(writer: Console.Error, useColor: !Console.IsErrorRedirected)
        {
            OnICE = () => throw new TestRunnerInternalCompilerError(),
        };

        var outputDir = new DirectoryInfo(Path.GetTempPath()).ChildDirectory("laye-test-suite");
        if (!outputDir.Exists) outputDir.Create();

        var coreLibFile = LibDir.ChildFile("core.mod");
        Debug.Assert(coreLibFile.Exists);

        var outputFile = outputDir.ChildFile($"{Path.GetFileNameWithoutExtension(SourceFile.Name)}.mod");
        var executableFile = outputDir.ChildFile($"{Path.GetFileNameWithoutExtension(SourceFile.Name)}{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ".out")}");

        try
        {
            int exitCode;

            exitCode = LayecDriver.RunWithArgs(diag, [
                SourceFile.FullName,
                coreLibFile.FullName,
                "-o", outputFile.FullName,
            ]);

            if (exitCode != 0)
            {
                Status = TestStatus.Failed;
                return;
            }

            var linkProcess = Process.Start("clang", [
                "-o", executableFile.FullName,
                outputFile.FullName,
                coreLibFile.FullName,
            ]);
            linkProcess.WaitForExit();
            exitCode = linkProcess.ExitCode;

            if (exitCode != 0)
            {
                Status = TestStatus.Failed;
                return;
            }

            var process = Process.Start(executableFile.FullName);
            process.WaitForExit();
            Status = process.ExitCode == 0 ? TestStatus.Passed : TestStatus.Failed;
        }
        catch (TestRunnerInternalCompilerError)
        {
            Status = TestStatus.Failed;
        }
        finally
        {
            if (outputFile.Exists) outputFile.Delete();
            if (executableFile.Exists) executableFile.Delete();
        }
    }
}

internal static class Program
{
    const string LAYE_LIB_DIR_PATH = "lib/laye";
    const string LAYE_TEST_DIR_PATH = "test/laye";

    static int BuildCoreLibrary(DirectoryInfo libDir)
    {
        var diag = new StreamingDiagnosticWriter(writer: Console.Error, useColor: !Console.IsErrorRedirected)
        {
            OnICE = () => throw new TestRunnerInternalCompilerError(),
        };

        try
        {
            return LayecDriver.RunWithArgs(diag, [
                "--no-corelib",
                "-o", libDir.ChildFile("core.mod").FullName,
                libDir.ChildDirectory("core").ChildFile("entry.laye").FullName
            ]);
        }
        catch (TestRunnerInternalCompilerError)
        {
            return 1;
        }
    }

    public static int Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        TestLog.Info("Searching for Laye lib directory...");

        DirectoryInfo? layeLibDir = new DirectoryInfo(Environment.CurrentDirectory);
        while (layeLibDir.Exists && !layeLibDir.ChildDirectory(LAYE_TEST_DIR_PATH).Exists)
        {
            Debug.Assert(layeLibDir.Parent is not null, $"We've gone too far looking for the lib directory '{LAYE_LIB_DIR_PATH}'");
            layeLibDir = layeLibDir.Parent;
        }

        if (layeLibDir is null)
        {
            TestLog.Error($"Could not find a Laye lib directory ('{LAYE_LIB_DIR_PATH}') relative to '{Environment.CurrentDirectory}' or any of its ancestors.");
            return 1;
        }

        layeLibDir = layeLibDir.ChildDirectory(LAYE_LIB_DIR_PATH);
        Debug.Assert(layeLibDir.Exists);

        TestLog.Info("Building core library...");

        int buildCoreExitCode = BuildCoreLibrary(layeLibDir);
        if (buildCoreExitCode != 0)
        {
            TestLog.Error($"Could not build the Laye core library.");
            return 1;
        }

        TestLog.Info("Searching for Laye test directory...");

        DirectoryInfo? layeTestsDir = new DirectoryInfo(Environment.CurrentDirectory);
        while (layeTestsDir.Exists && !layeTestsDir.ChildDirectory(LAYE_TEST_DIR_PATH).Exists)
        {
            Debug.Assert(layeTestsDir.Parent is not null, $"We've gone too far looking for the test directory '{LAYE_TEST_DIR_PATH}'");
            layeTestsDir = layeTestsDir.Parent;
        }

        if (layeTestsDir is null)
        {
            TestLog.Error($"Could not find a Laye test directory ('{LAYE_TEST_DIR_PATH}') relative to '{Environment.CurrentDirectory}' or any of its ancestors.");
            return 1;
        }

        layeTestsDir = layeTestsDir.ChildDirectory(LAYE_TEST_DIR_PATH);
        Debug.Assert(layeTestsDir.Exists);

        TestLog.Info($"Found Laye test directory at '{layeTestsDir.FullName}'.");

        TestLog.Info("Collecting Laye tests...");
        var testInstances = new List<TestInstance>();

        foreach (var testFile in layeTestsDir.GetFiles())
        {
            testInstances.Add(new ExecTestInstance(layeLibDir, testFile));
        }

        TestLog.Info($"Running {testInstances.Count} Laye tests...");

        foreach (var testInstance in testInstances)
        {
            testInstance.RunTest();
        }

        int numPassed = testInstances.Where(r => r.Status == TestStatus.Passed).Count();
        int numFailed = testInstances.Where(r => r.Status == TestStatus.Failed).Count();
        int numNotRun = testInstances.Where(r => r.Status == TestStatus.NotRun).Count();
        TestLog.Info($"{numPassed} Passed, {numFailed} Failed, {numNotRun} Not run");

        return numFailed > 0 ? 1 : 0;
    }
}
