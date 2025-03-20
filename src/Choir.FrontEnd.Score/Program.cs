using System.Text;

using Choir.Diagnostics;
using Choir.FrontEnd.Score.Driver;

namespace Choir.FrontEnd.Score;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        return ScoreDriver.RunWithArgs(useColor => new FormattedDiagnosticWriter(Console.Out, useColor), args);
    }
}
