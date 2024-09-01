using System.Numerics;
using System.Text;
using Choir.Front.Laye.Sema;

using ClangSharp;

namespace Choir.IR;

public abstract class ChoirValue(Location location, string name)
{
    public const string GlobalSigil = "$";
    public const string UsertypeSigil = ":";
    public const string BlockSigil = "@";
    public const string InstSigil = "%";

    public abstract string Sigil { get; }

    public Location Location { get; } = location;
    public string Name { get; } = name;
    public abstract ChoirTypeLoc Type { get; }

    public override string ToString() => ToIRString();
    protected internal abstract string ToIRString();

    protected virtual string ToValueStringImpl() => $"{Sigil}{Name}";
    public string ToValueString(bool includeType = true)
    {
        includeType |= this is ChoirValueLiteralInteger;
        return includeType ? $"{Type} {ToValueStringImpl()}" : ToValueStringImpl();
    }
}

public sealed class ChoirFunctionParam(Location location, string name, ChoirTypeLoc paramType)
    : ChoirValue(location, name)
{
    public override string Sigil { get; } = InstSigil;
    public override ChoirTypeLoc Type { get; } = paramType;
    protected internal override string ToIRString() => ToValueString(true);
}

public sealed class ChoirFunction(Location location, string name, ChoirTypeLoc returnType, ChoirFunctionParam[] @params)
    : ChoirValue(location, name)
{
    public override string Sigil { get; } = GlobalSigil;
    public ChoirTypeLoc ReturnType { get; } = returnType;
    public IReadOnlyList<ChoirFunctionParam> Params { get; } = @params;
    public override ChoirTypeLoc Type { get; } = new ChoirTypeFunction(returnType, @params.Select(p => p.Type).ToArray()).TypeLoc();
    public Linkage Linkage { get; set; }

    internal readonly List<ChoirValue> _blocks = [];
    public IReadOnlyList<ChoirValue> Blocks => _blocks;

    public ChoirFunction? Definition { get; internal set; }

    public ChoirBlock AppendBlock(Location location, string name)
    {
        var block = new ChoirBlock(location, name, this);
        _blocks.Add(block);
        return block;
    }

    protected internal override string ToIRString()
    {
        var builder = new StringBuilder();
        if (Linkage == Linkage.Exported)
            builder.Append("export ");

        builder.Append("function ");

        if (ReturnType.Type is not ChoirTypeVoid)
            builder.Append(ReturnType).Append(' ');

        builder.Append(Sigil).Append(Name).Append('(');
        for (int i = 0; i < Params.Count; i++)
        {
            if (i > 0) builder.Append(", ");
            builder.Append(Params[i].ToValueString(true));
        }

        builder.Append(')');

        if (_blocks.Count != 0)
        {
            builder.AppendLine(" {");
            for (int i = 0; i < _blocks.Count; i++)
            {
                if (i > 0) builder.AppendLine();
                builder.AppendLine(_blocks[i].ToIRString());
            }

            builder.Append('}');
        }

        return builder.ToString();
    }
}

public sealed class ChoirBlock(Location location, string name, ChoirFunction function)
    : ChoirValue(location, name)
{
    public override string Sigil { get; } = BlockSigil;
    public override ChoirTypeLoc Type { get; } = ChoirTypeVoid.Instance.TypeLoc();
    public ChoirFunction Function { get; } = function;

    internal readonly List<ChoirInst> _instructions = [];
    public IReadOnlyList<ChoirInst> Instructions => _instructions;

    public bool IsTerminated => _instructions.Count > 0 && _instructions[_instructions.Count - 1].IsTerminal;

    protected internal override string ToIRString()
    {
        var builder = new StringBuilder();

        builder.Append(Sigil).AppendLine(Name);
        for (int i = 0; i < _instructions.Count; i++)
        {
            if (i > 0) builder.AppendLine();
            builder.Append("  ").Append(_instructions[i].ToIRString());
        }

        return builder.ToString();
    }
}

public sealed class ChoirValueLiteralInteger(Location location, BigInteger value, ChoirTypeLoc type)
    : ChoirValue(location, "")
{
    public override string Sigil { get; } = "";
    public BigInteger Value { get; } = value;
    public override ChoirTypeLoc Type { get; } = type;
    protected override string ToValueStringImpl() => Value.ToString();
    protected internal override string ToIRString() => Value.ToString();
}

public abstract class ChoirInst(Location location, string? name, ChoirTypeLoc? type = null)
    : ChoirValue(location, name ?? "")
{
    public override string Sigil { get; } = InstSigil;
    public override ChoirTypeLoc Type { get; } = type ?? ChoirTypeVoid.Instance.TypeLoc();
    public virtual bool IsTerminal { get; } = false;

    protected abstract string ToInstString();
    protected internal override string ToIRString()
    {
        if (Type.Type is not ChoirTypeVoid)
            return $"{Sigil}{Name} {Type} = {ToInstString()}";
        return ToInstString();
    }
}

public sealed class ChoirInstRetVoid(Location location)
    : ChoirInst(location, null)
{
    protected override string ToInstString() => "ret void";
}

public sealed class ChoirInstRet(Location location, ChoirValue value)
    : ChoirInst(location, null)
{
    public ChoirValue Value { get; } = value;
    public override bool IsTerminal { get; } = true;
    protected override string ToInstString() => $"ret {Value.ToValueString()}";
}

public sealed class ChoirInstAdd(Location location, string name, ChoirTypeLoc type, ChoirValue left, ChoirValue right)
    : ChoirInst(location, name, type)
{
    public ChoirValue Left { get; } = left;
    public ChoirValue Right { get; } = right;
    public override bool IsTerminal { get; } = true;
    protected override string ToInstString() => $"add {type} {Left.ToValueString(false)}, {Right.ToValueString(false)}";
}
