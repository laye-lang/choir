namespace Choir;

public sealed class Target
{
    public static readonly Target X86_64 = new()
    {
        SizeOfPointer = Size.FromBits(64),
        AlignOfPointer = Align.ForBits(64),

        SizeOfSizeType = Size.FromBits(64),
        AlignOfSizeType = Align.ForBits(64),

        SizeOfCBool = Size.FromBits(8),
        SizeOfCChar = Size.FromBits(8),
        SizeOfCShort = Size.FromBits(16),
        SizeOfCInt = Size.FromBits(32),
        SizeOfCLong = Size.FromBits(64),
        SizeOfCLongLong = Size.FromBits(64),
        SizeOfCFloat = Size.FromBits(32),
        SizeOfCDouble = Size.FromBits(64),
        SizeOfCLongDouble = Size.FromBits(128),

        AlignOfCBool = Align.ForBits(8),
        AlignOfCChar = Align.ForBits(8),
        AlignOfCShort = Align.ForBits(16),
        AlignOfCInt = Align.ForBits(32),
        AlignOfCLong = Align.ForBits(64),
        AlignOfCLongLong = Align.ForBits(64),
        AlignOfCFloat = Align.ForBits(32),
        AlignOfCDouble = Align.ForBits(64),
        AlignOfCLongDouble = Align.ForBits(128),
    };

    public required Size SizeOfPointer { get; init; }
    public required Align AlignOfPointer { get; init; }

    public required Size SizeOfSizeType { get; init; }
    public required Align AlignOfSizeType { get; init; }

    public required Size SizeOfCBool { get; init; }
    public required Size SizeOfCChar { get; init; }
    public required Size SizeOfCShort { get; init; }
    public required Size SizeOfCInt { get; init; }
    public required Size SizeOfCLong { get; init; }
    public required Size SizeOfCLongLong { get; init; }
    public required Size SizeOfCFloat { get; init; }
    public required Size SizeOfCDouble { get; init; }
    public required Size SizeOfCLongDouble { get; init; }

    public required Align AlignOfCBool { get; init; }
    public required Align AlignOfCChar { get; init; }
    public required Align AlignOfCShort { get; init; }
    public required Align AlignOfCInt { get; init; }
    public required Align AlignOfCLong { get; init; }
    public required Align AlignOfCLongLong { get; init; }
    public required Align AlignOfCFloat { get; init; }
    public required Align AlignOfCDouble { get; init; }
    public required Align AlignOfCLongDouble { get; init; }
}
