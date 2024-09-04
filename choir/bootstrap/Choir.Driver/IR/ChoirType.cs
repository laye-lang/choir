namespace Choir.IR;

public readonly struct ChoirTypeLoc(ChoirType type, Location location) : IEquatable<ChoirTypeLoc>
{
    public static bool operator ==(ChoirTypeLoc left, ChoirTypeLoc right) => left.Type.Equals(right.Type);
    public static bool operator !=(ChoirTypeLoc left, ChoirTypeLoc right) => !(left.Type == right.Type);

    public static bool operator ==(ChoirTypeLoc left, ChoirType right) => left.Type.Equals(right);
    public static bool operator !=(ChoirTypeLoc left, ChoirType right) => !(left.Type == right);

    public static bool operator ==(ChoirType left, ChoirTypeLoc right) => left.Equals(right.Type);
    public static bool operator !=(ChoirType left, ChoirTypeLoc right) => !(left == right.Type);

    public readonly ChoirType Type = type;
    public readonly Location Location = location;

    public override string ToString() => ToSourceString();
    public readonly string ToSourceString() => Type.ToSourceString();

    public override int GetHashCode() => HashCode.Combine(Type, Location);
    public override bool Equals(object? obj) => obj is ChoirTypeLoc typeLoc && Equals(typeLoc);
    public bool Equals(ChoirTypeLoc typeLoc) => Type == typeLoc.Type;
}

public abstract class ChoirType : IEquatable<ChoirType>
{
    public static bool operator ==(ChoirType left, ChoirType right) => left.Equals(right);
    public static bool operator !=(ChoirType left, ChoirType right) => !(left == right);

    public abstract Size Size { get; }
    public virtual Align Align => Align.ForBytes(Size.Bytes);

    public virtual bool IsBasic { get; } = false;
    public virtual bool IsExtended { get; } = false;
    public virtual bool IsAggregate { get; } = false;

    public ChoirTypeLoc TypeLoc(Location? location = null) => new(this, location ?? Location.Nowhere);
    public override string ToString() => ToSourceString();
    public abstract string ToSourceString();

    public override int GetHashCode() => base.GetHashCode();
    public override bool Equals(object? obj) => obj is ChoirType type && Equals(type);
    public virtual bool Equals(ChoirType? type)
    {
        if (type is null) return false;
        return ReferenceEquals(type, this);
    }
}

public sealed class ChoirTypeVoid : ChoirType
{
    public static readonly ChoirTypeVoid Instance = new();
    public override Size Size { get; } = Size.FromBytes(0);
    public override string ToSourceString() => "void";
}

public sealed class ChoirTypeByte : ChoirType
{
    public static readonly ChoirTypeByte Instance = new();
    public override Size Size { get; } = Size.FromBytes(1);
    public override bool IsExtended { get; } = true;
    private ChoirTypeByte() { }
    public override string ToSourceString() => $"byte";
}

public sealed class ChoirTypeShort : ChoirType
{
    public static readonly ChoirTypeShort Instance = new();
    public override Size Size { get; } = Size.FromBytes(2);
    public override bool IsExtended { get; } = true;
    private ChoirTypeShort() { }
    public override string ToSourceString() => $"short";
}

public sealed class ChoirTypeInt : ChoirType
{
    public static readonly ChoirTypeInt Instance = new();
    public override Size Size { get; } = Size.FromBytes(4);
    public override bool IsBasic { get; } = true;
    private ChoirTypeInt() { }
    public override string ToSourceString() => $"int";
}

public sealed class ChoirTypeLong : ChoirType
{
    public static readonly ChoirTypeLong Instance = new();
    public override Size Size { get; } = Size.FromBytes(8);
    public override bool IsBasic { get; } = true;
    private ChoirTypeLong() { }
    public override string ToSourceString() => $"long";
}

public sealed class ChoirTypeSingle : ChoirType
{
    public static readonly ChoirTypeSingle Instance = new();
    public override Size Size { get; } = Size.FromBytes(4);
    public override bool IsBasic { get; } = true;
    private ChoirTypeSingle() { }
    public override string ToSourceString() => $"single";
}

public sealed class ChoirTypeDouble : ChoirType
{
    public static readonly ChoirTypeDouble Instance = new();
    public override Size Size { get; } = Size.FromBytes(8);
    public override bool IsBasic { get; } = true;
    private ChoirTypeDouble() { }
    public override string ToSourceString() => $"double";
}
