namespace System.Text;

public static class RuneExtensions
{
    public static void AppendRune(this StringBuilder builder, Rune rune)
    {
        Span<char> buffer = stackalloc char[2];
        int n = rune.EncodeToUtf16(buffer);
        builder.Append(buffer[..n]);
    }

    public static void AppendRunes(this StringBuilder builder, Rune[] runes)
    {
        foreach (var rune in runes)
            builder.AppendRune(rune);
    }

    public static string EncodeToString(this Rune[] runes)
    {
        var builder = new StringBuilder();
        builder.AppendRunes(runes);
        return builder.ToString();
    }
}
