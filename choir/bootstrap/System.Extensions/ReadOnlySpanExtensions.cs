namespace System;

public static class ReadOnlySpanExtensions
{
    public static bool All<T>(this ReadOnlySpan<T> s, Predicate<T> predicate)
    {
        for (int i = 0; i < s.Length; i++)
        {
            if (!predicate(s[i]))
                return false;
        }

        return true;
    }
}
