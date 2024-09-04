using System.Numerics;

namespace Choir.IR;

public sealed class ChoirBuilder(ChoirModule module)
{
    public ChoirContext Context { get; } = module.Context;
    public ChoirModule Module { get; } = module;

    private ChoirBlock? _block;
    private int _index = -1;

    private ChoirInst Insert(ChoirInst inst)
    {
        Context.Assert(_block is not null, "Can't insert into a builder without first positioning it within a block.");
        Context.Assert(_index >= 0, "Can't insert into a builder without first positioning it within a block.");
        _block._instructions.Insert(_index, inst);
        _index++;
        return inst;
    }

    public void PositionAtEnd(ChoirBlock block)
    {
        _block = block;
        _index = block._instructions.Count;
    }

    public ChoirValue BuildIAdd(Location location, string name, ChoirTypeLoc type, ChoirValue left, ChoirValue right)
    {
        Context.Assert(left.Type == type, location, "The type of an add instruction must match its left operand's.");
        Context.Assert(right.Type == type, location, "The type of an add instruction must match its right operand's.");
        return Insert(new ChoirInstBinary(location, name, ChoirBinaryOperatorKind.IAdd, type, left, right));
    }

    public ChoirValue BuildRet(Location location, ChoirValue value)
    {
        Context.Assert(value.Type.Type is not ChoirTypeVoid, location, $"Cannot use {nameof(BuildRet)} to return a void value.");
        return Insert(new ChoirInstRet(location, value));
    }

    public ChoirValue BuildRetVoid(Location location)
    {
        return Insert(new ChoirInstRetVoid(location));
    }

    public ChoirValue BuildAlloca(Location location, string name, ChoirTypeLoc type, int count, Align align)
    {
        return Insert(new ChoirInstAlloca(location, name, Context.Types.ChoirPointerType, type, count, align));
    }

    public ChoirValue BuildStore(Location location, ChoirValue address, ChoirValue value)
    {
        return Insert(new ChoirInstStore(location, address, value));
    }

    public ChoirValue BuildLoad(Location location, string name, ChoirTypeLoc type, ChoirValue address)
    {
        return Insert(new ChoirInstLoad(location, name, type, address));
    }
}
