using System.Text;

namespace Choir;

public static class StringBuilderExtensions
{
    public static bool EndsWith(this StringBuilder builder, string pattern)
    {
        if (builder.Length < pattern.Length)
            return false;
        
        for (int i = 0; i < pattern.Length; i++)
        {
            if (builder[builder.Length - pattern.Length + i] != pattern[i])
                return false;
        }

        return true;
    }
}
