namespace Choir.FrontEnd.Score.Syntax;

public abstract class ScoreSyntaxNode
    : IEquatable<ScoreSyntaxNode>
{
    private static long _counter = 0;

    public long Id { get; }

    public virtual IEnumerable<ScoreSyntaxNode> Children { get; } = [];

    protected ScoreSyntaxNode()
    {
        Id = Interlocked.Increment(ref _counter);
    }

    public override int GetHashCode() => HashCode.Combine(Id);

    public override bool Equals(object? obj) => obj is ScoreSyntaxNode other && Equals(other);
    public virtual bool Equals(ScoreSyntaxNode? other) => other is not null && Id == other.Id;
}
