namespace Choir;

public sealed record class TopologicalSortResult<T>
{
    public bool CircularDependencies { get; init; } = false;
    public required T[] Sorted { get; init; }
}

public static class TopologicalSort
{
    public static TopologicalSortResult<T> Sort<T>(IEnumerable<T> nodes, IEnumerable<(T From, T To)> edges)
        where T : IEquatable<T>
    {
        return Impl(new HashSet<T>(nodes), new HashSet<(T From, T To)>(edges));
        static TopologicalSortResult<T> Impl(HashSet<T> nodes, HashSet<(T From, T To)> edges)
        {
            var result = new List<T>();
            // Set of all nodes with no incoming edges
            var toProcess = new HashSet<T>(nodes.Where(n => edges.All(e => e.Item2.Equals(n) == false)));

            while (toProcess.Count != 0)
            {
                // remove a node n from S
                var from = toProcess.First();
                toProcess.Remove(from);

                // add n to tail of L
                result.Add(from);

                // for each node m with an edge e from n to m do
                foreach (var edge in edges.Where(e => e.From.Equals(from)))
                {
                    var to = edge.To;

                    // remove edge e from the graph
                    edges.Remove(edge);

                    // if m has no other incoming edges then
                    if (edges.All(me => me.From.Equals(to) == false))
                    {
                        // insert m into S
                        toProcess.Add(to);
                    }
                }
            }

            if (edges.Count != 0)
            {
                return new TopologicalSortResult<T>()
                {
                    CircularDependencies = true,
                    Sorted = [],
                };
            }

            result.Reverse();
            return new TopologicalSortResult<T>()
            {
                Sorted = [.. result],
            };
        }
    }
}
