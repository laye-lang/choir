namespace Choir.FrontEnd.Score.Syntax;

public abstract class LayeSyntaxNode
{
    private static long _counter = 0;

    public long Id { get; }

    protected LayeSyntaxNode()
    {
        Id = Interlocked.Increment(ref _counter);
    }

    public override int GetHashCode() => HashCode.Combine(Id);
}
