using Choir.Source;

namespace Choir.FrontEnd.Laye.Semantics;

public abstract class LayeSemaNode
{
    private static long _counter = 0;

    public long Id { get; }

    protected LayeSemaNode()
    {
        Id = Interlocked.Increment(ref _counter);
    }

    public override int GetHashCode() => HashCode.Combine(Id);
}
