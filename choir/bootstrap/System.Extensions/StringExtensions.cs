namespace System;

public static class StringExtensions
{
    public static bool ContainsAny(this string s, params char[] chars) => s.ContainsAny((IEnumerable<char>)chars);
    public static bool ContainsAny(this string s, IEnumerable<char> chars)
    {
        for (int i = 0; i < s.Length; i++)
        {
            foreach (char c in chars)
            {
                if (s[i] == c)
                    return true;
            }
        }

        return false;
    }
}
