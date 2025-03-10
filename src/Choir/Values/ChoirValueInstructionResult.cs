namespace Choir.Values;

public sealed class ChoirValueInstructionResult
    : ChoirValue
{
    public ChoirContext Context { get; }
    public ChoirInstruction Instruction { get; }
    public int ResultIndex { get; }

    internal ChoirValueInstructionResult(ChoirContext context, ChoirInstruction instruction, int resultIndex)
    {
        Context = context;
        Instruction = instruction;
        ResultIndex = resultIndex;
    }
}
