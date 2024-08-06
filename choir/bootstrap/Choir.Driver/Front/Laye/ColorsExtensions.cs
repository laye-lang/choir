using Choir.CommandLine;

namespace Choir.Front.Laye;

public static class ColorsExtensions
{
    public static string LayeName(this Colors colors) => colors.Yellow;
    public static string LayeKeyword(this Colors colors) => colors.Blue;
    public static string LayeLiteral(this Colors colors) => colors.Yellow;
    public static string LayeTemplate(this Colors colors) => colors.Yellow;
}
