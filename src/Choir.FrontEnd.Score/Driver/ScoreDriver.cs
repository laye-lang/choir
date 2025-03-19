using Choir.Driver;

namespace Choir.FrontEnd.Score.Driver;

public sealed class ScoreDriver
    : ICompilerDriver
{
    public static ScoreDriver Create()
    {
        return new ScoreDriver();
    }

    private ScoreDriver()
    {
    }

    public int Execute()
    {
        return 0;
    }
}
