using System.Diagnostics;

using Choir.CommandLine;

namespace Choir.Front.Laye.Sema;

public enum Linkage
{
    Internal,
    Exported,
    Imported,
    ReExported,
}

[Flags]
public enum TypeQualifiers
{
    None = 0,
    Mutable = 1 << 0,
}

public enum ValueCategory
{
    RValue,
    LValue,
}

public static class ValueCategoryExtensions
{
    public static string ToHumanString(this ValueCategory vc, bool includeArticle = false)
    {
        switch (vc)
        {
            default: throw new UnreachableException();
            case ValueCategory.LValue: return includeArticle ? "an l-value" : "l-value";
            case ValueCategory.RValue: return includeArticle ? "an r-value" : "r-value";
        }
    }
}

/// Bitmask that indicates whether a node is dependent.
///
/// A node is *type-dependent* if its type depends on a template
/// parameter. For example, in the program
///
///   T add<var T>(T a, T b) { return a + b; }
///
/// the expression `a` is type-dependent because it *is* a template
/// parameter, and the expression `a + b` is type-dependent because
/// the type of a `+` expression is the common type of its operands,
/// which in this case are both type-dependent.
///
/// A node is *value-dependent* if its *value* depends on a template
/// parameter. Value dependence is usually only relevant in contexts
/// that require constant evaluation. For example, in the program
///
///   void foo<int N>() { int[N] a; }
///
/// in the array type `int[N]`, the expression `N` is value-dependent
/// because its value won’t be known until instantiation time; this
/// means we also can’t check if the array is valid until instantiation
/// time. As a result, the type of the array is dependent.
///
/// Finally, a type is *instantiation-dependent* if it contains a
/// template parameter. Type-dependence and value-dependence both
/// imply instantiation-dependence.
///
/// Note that value-dependence does not imply type-dependence or vice
/// versa. Furthermore, an expression that contains a value-dependent
/// expression or a type-dependent type need not be value- or type-
/// dependent itself, but it *is* instantiation-dependent.
///
/// For example, if `a` is a type template parameter, then `a[4]` is
/// type-dependent, but `sizeof(a[4])` is not type-dependent since the
/// type of `sizeof(...)` is always `int`. However, it *is* value-dependent
/// since the size of a dependent type is unknown. Furthermore, the
/// expression `sizeof(sizeof(a[4]))` is neither type- nor value-dependent:
/// `sizeof(...)` is never type-dependent, and only value-dependent
/// if its operand is type-dependent, so `sizeof(sizeof(a[4])) is neither.
///
/// However, it *is* still instantiation-dependent since we need to instantiate
/// it at instantiation time, not to know its value, but still to check that
/// the inner `sizeof` is actually valid. For example, although the value of
/// `sizeof(sizeof(a[-1]))` is logically always `sizeof(int)`, this expression
/// is still invalid because it contains an invalid type.
///
/// Finally, error handling in sema is very similar to handling dependence:
/// if an expression is type-dependent, we can’t perform any checks that rely
/// on its type, and the same applies to value-dependence. The same is true
/// for expressions that contain an error, so we simply model errors as another
/// form of dependence, though error-dependence, unlike type- or value-dependence
/// is never resolved. Error-dependent expressions are simply skipped at instantiation
/// time.
[Flags]
public enum ExprDependence
{
    // not dependent.
    None = 0,

    // node contains a dependent node.
    Instantiation = 1 << 0,
    
    // the node's type depends on a template parameter
    Type = 1 << 1,
    TypeDependent = Instantiation | Type,

    // the node's value depends on a template parameter
    Value = 1 << 2,
    ValueDependent = Instantiation | Value,

    // the node contains an error.
    Error = 1 << 7,
    ErrorDependent = Instantiation | Error,
}

public enum CallingConvention
{
    CDecl,
    Laye,
    StdCall,
    FastCall,
}

public abstract class BaseSemaNode
    : IEquatable<BaseSemaNode>
{
    public static bool operator ==(BaseSemaNode left, BaseSemaNode right) => left.Equals(right);
    public static bool operator !=(BaseSemaNode left, BaseSemaNode right) => !left.Equals(right);

    private static long _idCounter;

    public readonly long Id = Interlocked.Increment(ref _idCounter);

    public ExprDependence Dependence { get; set; } = ExprDependence.None;
    public bool IsErrored => Dependence.HasFlag(ExprDependence.Error);

    public bool IsCompilerGenerated { get; set; } = false;
    public virtual IEnumerable<BaseSemaNode> Children { get; } = [];

    public override int GetHashCode() => Id.GetHashCode();
    public override bool Equals(object? obj) => obj is BaseSemaNode other && Equals(other);
    public virtual bool Equals(BaseSemaNode? other)
    {
        return other is not null && other.Id == Id;
    }
}

public abstract class SemaType : BaseSemaNode
{
    public virtual bool IsBuiltin { get; } = false;
    public virtual bool IsPoison { get; } = false;
    public virtual bool IsNumeric { get; } = false;
    public virtual bool IsVoid { get; } = false;
    public virtual bool IsNoReturn { get; } = false;
    public virtual bool IsBool { get; } = false;
    public virtual bool IsInteger { get; } = false;
    public virtual bool IsFloat { get; } = false;

    public abstract Size Size { get; }
    public virtual Align Align => Align.ForBytes(Size.Bytes);

    public virtual SemaType CanonicalType => this;
    public SemaTypeQual Qualified(Location location, TypeQualifiers qualifiers = TypeQualifiers.None) =>
        new(this, location, qualifiers);

    public override string ToString() => ToDebugString(Colors.Off);
    public abstract string ToDebugString(Colors colors);

    public override bool Equals(BaseSemaNode? other) => other is SemaType otherType && TypeEquals(otherType, TypeComparison.WithIdenticalQualifiers);
    public abstract bool TypeEquals(SemaType other, TypeComparison comp = TypeComparison.WithIdenticalQualifiers);

    public virtual SerializedTypeKind SerializedTypeKind { get; } = SerializedTypeKind.Invalid;
    public virtual void Serialize(ModuleSerializer serializer, BinaryWriter writer)
    {
        serializer.Context.Diag.ICE($"Attempt to serialize type of type {GetType().Name}, which is currently not supported.");
        throw new UnreachableException();
    }

    public static SemaType Deserialize(ModuleDeserializer deserializer, SerializedTypeKind kind, BinaryReader reader)
    {
        switch (kind)
        {
            default:
            {
                deserializer.Context.Diag.ICE($"Attempt to deserialize type of kind {kind}, which is currently not supported.");
                throw new UnreachableException();
            }

            case SerializedTypeKind.Buffer: return SemaTypeBuffer.Deserialize(deserializer, reader);

            case SerializedTypeKind.Void: return deserializer.Context.Types.LayeTypeVoid;
            case SerializedTypeKind.NoReturn: return deserializer.Context.Types.LayeTypeNoReturn;
            case SerializedTypeKind.Bool: return deserializer.Context.Types.LayeTypeBool;
            case SerializedTypeKind.BoolSized: return deserializer.Context.Types.LayeTypeBoolSized(reader.ReadUInt16());
            case SerializedTypeKind.Int: return deserializer.Context.Types.LayeTypeInt;
            case SerializedTypeKind.IntSized: return deserializer.Context.Types.LayeTypeIntSized(reader.ReadUInt16());
            case SerializedTypeKind.Float32: return deserializer.Context.Types.LayeTypeFloatSized(32);
            case SerializedTypeKind.Float64: return deserializer.Context.Types.LayeTypeFloatSized(64);
            case SerializedTypeKind.FFI:
            {
                char ffiKind = (char)reader.ReadByte();
                switch (ffiKind)
                {
                    default:
                    {
                        deserializer.Context.Diag.ICE($"Attempt to deserialize FFI type of kind {ffiKind}, which is currently not supported.");
                        throw new UnreachableException();
                    }

                    case SerializerConstants.FFIBoolTypeSigil: return deserializer.Context.Types.LayeTypeFFIBool;
                    case SerializerConstants.FFICharTypeSigil: return deserializer.Context.Types.LayeTypeFFIChar;
                    case SerializerConstants.FFIShortTypeSigil: return deserializer.Context.Types.LayeTypeFFIShort;
                    case SerializerConstants.FFIIntTypeSigil: return deserializer.Context.Types.LayeTypeFFIInt;
                    case SerializerConstants.FFILongTypeSigil: return deserializer.Context.Types.LayeTypeFFILong;
                    case SerializerConstants.FFILongLongTypeSigil: return deserializer.Context.Types.LayeTypeFFILongLong;
                    case SerializerConstants.FFIFloatTypeSigil: return deserializer.Context.Types.LayeTypeFFIFloat;
                    case SerializerConstants.FFIDoubleTypeSigil: return deserializer.Context.Types.LayeTypeFFIDouble;
                    case SerializerConstants.FFILongDoubleTypeSigil: return deserializer.Context.Types.LayeTypeFFILongDouble;
                }
            }
        }
    }
}

public enum TypeComparison
{
    WithIdenticalQualifiers,
    WithQualifierConversions,
    TypeOnly,
}

public abstract class SemaType<T> : SemaType
    where T : SemaType<T>
{
    public override bool Equals(BaseSemaNode? other) => other is T otherType && TypeEquals(otherType, TypeComparison.WithIdenticalQualifiers);
    public override bool TypeEquals(SemaType other, TypeComparison comp) => other is T otherType && TypeEquals(otherType, comp);
    public abstract bool TypeEquals(T other, TypeComparison comp = TypeComparison.WithIdenticalQualifiers);
}

public sealed class SemaTypeQual(SemaType type, Location location, TypeQualifiers qualifiers = TypeQualifiers.None)
    : BaseSemaNode
{
    //public static implicit operator SemaType(SemaTypeQual typeLoc) => typeLoc.Type;

    public SemaType Type { get; } = type;
    public Location Location { get; } = location;
    public TypeQualifiers Qualifiers { get; } = qualifiers;

    public override IEnumerable<BaseSemaNode> Children { get; } = [type];

    public Size Size => Type.Size;
    public Align Align => Type.Align;

    public bool IsPoison => Type.IsPoison;
    public bool IsBuiltin => Type.IsBuiltin;
    public bool IsNumeric => Type.IsNumeric;
    public bool IsVoid => Type.IsVoid;
    public bool IsNoReturn => Type.IsNoReturn;
    public bool IsBool => Type.IsBool;
    public bool IsInteger => Type.IsInteger;
    public bool IsFloat => Type.IsFloat;

    public bool IsQualified => Qualifiers != TypeQualifiers.None;
    public bool IsMutable => Qualifiers.HasFlag(TypeQualifiers.Mutable);
    public SemaTypeQual Unqualified => new(Type, Location);

    public SemaTypeQual CanonicalType
    {
        get
        {
            var canon = Type.CanonicalType;
            if (Type != canon) return Type.CanonicalType.Qualified(Location, Qualifiers);
            return this;
        }
    }

    public SemaTypeQual Qualified(TypeQualifiers qualifiers = TypeQualifiers.None) =>
        new(Type, Location, Qualifiers | qualifiers);

    public SemaTypeQual Qualified(Location location, TypeQualifiers qualifiers = TypeQualifiers.None) =>
        new(Type, location, Qualifiers | qualifiers);

    public SemaTypeQual Requalified(TypeQualifiers qualifiers = TypeQualifiers.None) =>
        new(Type, Location, qualifiers);

    public SemaTypeQual Requalified(Location location, TypeQualifiers qualifiers = TypeQualifiers.None) =>
        new(Type, location, qualifiers);

    public override string ToString() => ToDebugString(Colors.Off);
    public string ToDebugString(Colors colors)
    {
        string typeString = Type.ToDebugString(colors);
        if (Qualifiers.HasFlag(TypeQualifiers.Mutable))
            typeString += $" {colors.LayeKeyword()}mut{colors.Default}";
        return typeString;
    }

    public bool TypeEquals(SemaTypeQual other, TypeComparison comp)
    {
        if (comp == TypeComparison.WithIdenticalQualifiers && Qualifiers != other.Qualifiers)
            return false;

        if (comp == TypeComparison.WithQualifierConversions)
        {
            if (!IsMutable && other.IsMutable)
                return false;
        }

        return Type.TypeEquals(other.Type, comp);
    }
}

public enum StmtControlFlow
{
    Fallthrough,
    Jump,
    Return,
}

public abstract class SemaStmt(Location location) : BaseSemaNode
{
    public Location Location { get; } = location;
    public virtual StmtControlFlow ControlFlow { get; } = StmtControlFlow.Fallthrough;
}

public abstract class SemaDecl(Location location)
    : SemaStmt(location)
{
}

public abstract class SemaDeclNamed(Location location, string name)
    : SemaDecl(location)
{
    public string Name { get; } = name;
    public bool IsForeign { get; set; } = false;
    public string? ForeignSymbolName { get; set; }
    public Linkage Linkage { get; set; } = Linkage.Internal;

    public virtual SerializedDeclKind SerializedDeclKind { get; } = SerializedDeclKind.Invalid;
    public virtual void Serialize(ModuleSerializer serializer, BinaryWriter writer)
    {
        serializer.Context.Diag.ICE(Location, $"Attempt to serialize named declaration of type {GetType().Name}, which is currently not supported.");
        throw new UnreachableException();
    }

    public virtual void Deserialize(ModuleDeserializer deserializer, BinaryReader reader)
    {
        deserializer.Context.Diag.ICE(Location, $"Attempt to deserialize named declaration of type {GetType().Name}, which is currently not supported.");
        throw new UnreachableException();
    }
}

public abstract class SemaExpr(Location location, SemaTypeQual type) : BaseSemaNode
{
    public Location Location { get; } = location;
    public SemaTypeQual Type { get; } = type;

    public ValueCategory ValueCategory { get; init; } = ValueCategory.RValue;
    public bool IsLValue => ValueCategory == ValueCategory.LValue;
}

public abstract class SemaPattern(Location location) : BaseSemaNode
{
    public Location Location { get; } = location;
}
