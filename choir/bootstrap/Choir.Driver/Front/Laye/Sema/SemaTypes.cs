using Choir.CommandLine;

namespace Choir.Front.Laye.Sema;

public enum BuiltinTypeKind
{
    Void,
    NoReturn,
    Bool,
    BoolSized,
    Int,
    IntSized,
    FloatSized,

    FFIChar,
    FFIShort,
    FFIInt,
    FFILong,
    FFILongLong,
    FFIFloat,
    FFIDouble,
    FFILongDouble,
}

public sealed class SemaTypePoison : SemaType
{
    public static readonly SemaTypePoison Instance = new();
    public override bool IsPoison { get; } = true;
    public override string ToDebugString(Colors colors) =>
        $"{colors.Red}POISON";
}

public sealed class SemaTypeTypeInfo : SemaType
{
    public static readonly SemaTypeTypeInfo Instance = new();
    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeKeyword()}typeinfo";
}

public sealed class SemaTypeBuiltIn(BuiltinTypeKind kind, int bitWidth = 0) : SemaType
{
    public BuiltinTypeKind Kind { get; } = kind;
    /// <summary>
    /// For explicitly sized builtins, the declared bit width.
    /// </summary>
    public int BitWidth { get; } = bitWidth;
    /// <summary>
    /// True when this type has an explicit size parameter, such as `i32` or `f64`.
    /// </summary>
    public bool IsExplicitlySized => Kind is BuiltinTypeKind.IntSized or BuiltinTypeKind.FloatSized or BuiltinTypeKind.BoolSized;

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeKeyword()}{Kind.ToString().ToLower()}";
}

public sealed class SemaTypeElaborated(string[] nameParts, SemaTypeQual namedType) : SemaType
{
    public IReadOnlyList<string> NameParts { get; } = nameParts;
    public SemaTypeQual NamedType { get; } = namedType;
    
    public override string ToDebugString(Colors colors) =>
        string.Join($"{colors.Reset}::", NameParts.Select(name => $"{colors.LayeName()}{name}"));
}

public sealed class SemaTypeTemplateParameter(string parameterName) : SemaType
{
    public string ParameterName { get; } = parameterName;

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeTemplate()}{ParameterName}";
}

public abstract class SemaContainerType(SemaTypeQual elementType) : SemaType
{
    public SemaTypeQual ElementType { get; } = elementType;
}

public sealed class SemaTypePointer(SemaTypeQual elementType) : SemaContainerType(elementType)
{
    public override string ToDebugString(Colors colors) =>
        $"{ElementType.ToDebugString(colors)}{colors.Reset}*";
}

public sealed class SemaTypeBuffer(SemaTypeQual elementType, byte? terminator = null) : SemaContainerType(elementType)
{
    public byte? Terminator { get; } = terminator;
    public override string ToDebugString(Colors colors)
    {
        if (Terminator is byte t)
            return $"{ElementType.ToDebugString(colors)}{colors.Reset}[*:{colors.LayeLiteral()}{Terminator}{colors.Reset}]";
        else return $"{ElementType.ToDebugString(colors)}{colors.Reset}[*]";
    }
}

public sealed class SemaTypeReference(SemaTypeQual elementType) : SemaContainerType(elementType)
{
    public override string ToDebugString(Colors colors) =>
        $"{ElementType.ToDebugString(colors)}{colors.Reset}&";
}

public sealed class SemaTypeNilable(SemaTypeQual elementType) : SemaContainerType(elementType)
{
    public override string ToDebugString(Colors colors) =>
        $"{ElementType.ToDebugString(colors)}{colors.Reset}?";
}

public sealed class SemaTypeSlice(SemaTypeQual elementType) : SemaContainerType(elementType)
{
    public override string ToDebugString(Colors colors) =>
        $"{ElementType.ToDebugString(colors)}{colors.Reset}[]";
}

public sealed class SemaTypeArray(SemaTypeQual elementType, long[] lengths) : SemaContainerType(elementType)
{
    public IReadOnlyList<long> Lengths { get; } = lengths;
    public int Arity { get; } = lengths.Length;

    public override string ToDebugString(Colors colors)
    {
        string lengths = string.Join($"{colors.Reset}, ", Lengths.Select(length => $"{colors.LayeLiteral()}{length}"));
        return $"{ElementType.ToDebugString(colors)}{colors.Reset}[{lengths}{colors.Reset}]";
    }
}

public sealed class SemaTypeErrorPair(SemaTypeQual resultType, SemaTypeQual errorType) : SemaType
{
    public SemaTypeQual ResultType { get; } = resultType;
    public SemaTypeQual ErrorType { get; } = errorType;
    public override string ToDebugString(Colors colors) =>
        $"{ResultType.ToDebugString(colors)}{colors.Reset}!{ErrorType.ToDebugString(colors)}";
}

public sealed class SemaTypeStruct(SemaDeclStruct declStruct) : SemaType
{
    public SemaDeclStruct DeclStruct { get; } = declStruct;
    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeName()}{DeclStruct.Name}";
}
