using Choir;

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
    public override string ToSourceString() => "void";
}

public sealed class ChoirTypeI32 : ChoirType
{
    public static readonly ChoirTypeI32 Instance = new();
    public override string ToSourceString() => "i32";
}

public sealed class ChoirTypeI64 : ChoirType
{
    public static readonly ChoirTypeI64 Instance = new();
    public override string ToSourceString() => "i64";
}

public sealed class ChoirTypeFunction(ChoirTypeLoc returnType, IReadOnlyList<ChoirTypeLoc> paramTypes)
    : ChoirType
{
    public ChoirTypeLoc ReturnType { get; } = returnType;
    public IReadOnlyList<ChoirTypeLoc> ParamTypes { get; } = paramTypes;
    public override string ToSourceString() => $"function({string.Join(", ", ParamTypes)}) {ReturnType}";
    public override bool Equals(ChoirType? type)
    {
        if (type is not ChoirTypeFunction typeFunction) return false;
        return ReturnType == typeFunction.ReturnType && ParamTypes.Count == typeFunction.ParamTypes.Count && ParamTypes.Zip(typeFunction.ParamTypes).All(pair => pair.First == pair.Second);
    }
}
