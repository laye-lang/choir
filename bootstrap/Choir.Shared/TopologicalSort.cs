namespace Choir;

public abstract class TopologicalSortResult<T>
{
}

public sealed class TopologicalSortOk<T>
    : TopologicalSortResult<T>
{
    public static readonly TopologicalSortOk<T> Instance = new();
}

public sealed class TopologicalSortSuccess<T>(T[] sorted)
    : TopologicalSortResult<T>
{
    public T[] Sorted { get; } = sorted;
}

public sealed class TopologicalSortCircular<T>(T from, T to)
    : TopologicalSortResult<T>
{
    public T From { get; } = from;
    public T To { get; } = to;
}

public static class TopologicalSort
{
    public static TopologicalSortResult<T> Sort<T>(IEnumerable<T> nodes, IEnumerable<(T From, T To)> edges)
        where T : IEquatable<T>
    {
        var resolved = new List<T>();
        var seen = new HashSet<T>();

        foreach (var entity in nodes)
            Impl(entity);

        return new TopologicalSortSuccess<T>([.. resolved]);

        TopologicalSortResult<T> Impl(T entity)
        {
            if (resolved.Contains(entity))
                return TopologicalSortOk<T>.Instance;

            seen.Add(entity);

            var dependencies = edges.Where(edge => EqualityComparer<T>.Default.Equals(entity, edge.From))
                .Select(edge => edge.To).ToArray();
            foreach (var dep in dependencies)
            {
                if (resolved.Contains(dep)) continue;
                if (seen.Contains(dep))
                    return new TopologicalSortCircular<T>(entity, dep);

                var nested = Impl(dep);
                if (nested is not TopologicalSortOk<T>)
                    return nested;
            }

            resolved.Add(entity);
            seen.Remove(entity);

            return TopologicalSortOk<T>.Instance;
        }
    }
}
