using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

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

public sealed record class ExecTestInstance(FileInfo SourceFile) : TestInstance(SourceFile)
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

        var outputFile = outputDir.ChildFile($"{SourceFile.Name}{(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ".out")}");
        //var outputFile = outputDir.ChildFile($"{SourceFile.Name}.o");

        try
        {

            int exitCode = ChoirDriver.RunWithArgs(diag, [SourceFile.FullName, "-o", outputFile.FullName]);
            //int exitCode = ChoirDriver.RunWithArgs(diag, [SourceFile.FullName, "--codegen", "--ir"]);
            if (exitCode != 0)
            {
                Status = TestStatus.Failed;
                return;
            }

            var process = Process.Start(outputFile.FullName);
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
        }
    }
}

internal static class Program
{
    const string LAYE_TEST_DIR_PATH = "test/laye";

    public static int Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

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
            testInstances.Add(new ExecTestInstance(testFile));
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
