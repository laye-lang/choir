namespace System;

public static class ArrayExtensions
{
    static int SpanIndexOf<T>(Span<T> s, T value)
    {
        for (int i = 0; i < s.Length; i++)
        {
            if (EqualityComparer<T>.Default.Equals(value, s[i]))
                return i;
        }

        return -1;
    }

    public static T[][] Split<T>(this T[] arr, T value)
    {
        Span<T> span = arr;
        List<T[]> parts = [];

        do
        {
            int valueIndex = SpanIndexOf(span, value);
            //int valueIndex = Array.IndexOf(arr, value);
            if (valueIndex < 0)
            {
                parts.Add(span.ToArray());
                span = [];
            }
            else
            {
                parts.Add(span[0..valueIndex].ToArray());
                span = span[(valueIndex + 1)..];
            }
        } while (span.Length > 0);

        return [.. parts];
    }
}
