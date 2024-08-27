namespace Choir;

public static class ArrayExtensions
{
    public static T[][] Split<T>(this T[] arr, T value)
    {
        Span<T> span = arr;
        List<T[]> parts = [];

        do
        {
            int valueIndex = Array.IndexOf(arr, value);
            if (valueIndex < 0)
                parts.Add(span.ToArray());
            else
            {
                parts.Add(span[0..valueIndex].ToArray());
                span = span[(valueIndex + 1)..];
            }
        } while (span.Length > 0);

        return [.. parts];
    }
}
