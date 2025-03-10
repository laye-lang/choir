using Choir.Values;

namespace Choir;

public abstract class ChoirInstruction
{
    private static int _counter = 0;

    public int Id { get; }
    public ChoirContext Context { get; }

    public virtual int ResultCount { get; } = 0;

    protected ChoirInstruction(ChoirContext context)
    {
        _counter = Interlocked.Increment(ref _counter);
        Context = context;
    }

    public ChoirValue GetResultAt(int index)
    {
        Context.Assert(index >= 0 && index < ResultCount, $"Result index {index} is out of range for this instruction. Must be in the range [0, {ResultCount}).");
        return new ChoirValueInstructionResult(Context, this, index);
    }
}
