namespace System.Collections.Generic;

public static class IEnumerableExtensions
{
    public static IEnumerable<TResult> WhereIs<TResult>(this IEnumerable source)
    {
        if (source is IEnumerable<TResult> typedSource)
            return typedSource;

        return source is null
            ? throw new ArgumentNullException(nameof(source))
            : WhereIsIterator<TResult>(source);
    }

    private static IEnumerable<TResult> WhereIsIterator<TResult>(IEnumerable source)
    {
        foreach (object obj in source)
        {
            if (obj is TResult resultObj)
                yield return resultObj;
        }
    }
}
