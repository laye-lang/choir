namespace Choir.TestRunner;

public enum TestLogLevel
{
    Info,
    Error,
}

public static class TestLog
{
    public static void Log(TestLogLevel level, string message)
    {
        Console.Error.Write($"[{level}] ");
        Console.Error.WriteLine(message);
    }

    public static void Info(string message) => Log(TestLogLevel.Info, message);
    public static void Error(string message) => Log(TestLogLevel.Error, message);
}
