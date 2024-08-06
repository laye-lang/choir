using Choir.CommandLine;

namespace Choir.Front.Laye.Sema;

[Flags]
public enum TypeQualifiers
{
    None = 0,
    Mutable = 1 << 0,
}

public enum ValueCategory
{
    LValue,
    RValue,
}

public enum ExprDependence
{
}

public abstract class BaseSemaNode
{
    public bool CompilerGenerated { get; set; } = false;
    public virtual IEnumerable<BaseSemaNode> Children { get; } = [];
}

public abstract class SemaType : BaseSemaNode
{
    public virtual bool IsPoison { get; } = false;
    public override string ToString() => ToDebugString(Colors.Off);
    public abstract string ToDebugString(Colors colors);
}

public sealed class SemaTypeQual(SemaType type, Location location, TypeQualifiers qualifiers = TypeQualifiers.None)
    : BaseSemaNode
{
    public static implicit operator SemaType(SemaTypeQual typeLoc) => typeLoc.Type;

    public SemaType Type { get; } = type;
    public Location Location { get; } = location;
    public TypeQualifiers Qualifiers { get; } = qualifiers;
    
    public override IEnumerable<BaseSemaNode> Children { get; } = [type];

    public bool IsQualified => Qualifiers != TypeQualifiers.None;
    public SemaTypeQual Unqualified => new(Type, Location);

    public override string ToString() => ToDebugString(Colors.Off);
    public string ToDebugString(Colors colors)
    {
        string typeString = Type.ToDebugString(colors);
        if (Qualifiers.HasFlag(TypeQualifiers.Mutable))
            typeString += $" {colors.LayeKeyword()}mut";
        return typeString;
    }
}

public abstract class SemaDecl(Location location) : BaseSemaNode
{
    public Location Location { get; } = location;
}

public abstract class SemaStmt(Location location) : BaseSemaNode
{
    public Location Location { get; } = location;
}

public abstract class SemaExpr(Location location, SemaTypeQual type) : BaseSemaNode
{
    public Location Location { get; } = location;
    public SemaTypeQual Type { get; } = type;
}

public abstract class SemaPattern(Location location) : BaseSemaNode
{
    public Location Location { get; } = location;
}

public abstract class SemaAttribute(Location location) : BaseSemaNode
{
    public Location Location { get; } = location;
}
