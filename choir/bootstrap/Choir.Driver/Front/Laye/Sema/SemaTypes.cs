using System.Diagnostics;
using System.Text;
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

    FFIBool,
    FFIChar,
    FFIShort,
    FFIInt,
    FFILong,
    FFILongLong,
    FFIFloat,
    FFIDouble,
    FFILongDouble,

    __FFI_FIRST = FFIBool,
    __FFI_LAST = FFILongDouble,

    __FFI_FIRST_INTEGER = FFIChar,
    __FFI_LAST_INTEGER = FFILongLong,

    __FFI_FIRST_FLOAT = FFIFloat,
    __FFI_LAST_FLOAT = FFILongDouble,
}

public static class BuiltinTypeKindExtensions
{
    public static string ToLanguageKeywordString(this BuiltinTypeKind kind, int bitWidth = 0) => kind switch {
        BuiltinTypeKind.Void => "void",
        BuiltinTypeKind.NoReturn => "noreturn",
        BuiltinTypeKind.Bool => "bool",
        BuiltinTypeKind.BoolSized => $"b{bitWidth}",
        BuiltinTypeKind.Int => "int",
        BuiltinTypeKind.IntSized => $"i{bitWidth}",
        BuiltinTypeKind.FloatSized => $"f{bitWidth}",
        BuiltinTypeKind.FFIBool => "__laye_ffi_bool",
        BuiltinTypeKind.FFIChar => "__laye_ffi_char",
        BuiltinTypeKind.FFIShort => "__laye_ffi_short",
        BuiltinTypeKind.FFIInt => "__laye_ffi_int",
        BuiltinTypeKind.FFILong => "__laye_ffi_long",
        BuiltinTypeKind.FFILongLong => "__laye_ffi_longlong",
        BuiltinTypeKind.FFIFloat => "__laye_ffi_float",
        BuiltinTypeKind.FFIDouble => "__laye_ffi_double",
        BuiltinTypeKind.FFILongDouble => "__laye_ffi_longdouble",
        _ => "{{unknown builtin type}}",
    };

    public static bool IsExplicitlySized(this BuiltinTypeKind kind) => kind switch {
        BuiltinTypeKind.BoolSized => true,
        BuiltinTypeKind.IntSized => true,
        BuiltinTypeKind.FloatSized => true,
        _ => false,
    };
}

public sealed class SemaTypePoison : SemaType
{
    public static readonly SemaTypePoison Instance = new();
    public static readonly SemaTypeQual InstanceQualified = Instance.Qualified(Location.Nowhere);

    public override bool IsPoison { get; } = true;
    public override Size Size { get; } = Size.FromBytes(1);
    public override string ToDebugString(Colors colors) =>
        $"{colors.Red}POISON{colors.Default}";
}

public sealed class SemaTypeTypeInfo : SemaType
{
    public static readonly SemaTypeTypeInfo Instance = new();

    // TODO(local): TypeInfo needs a size... if we even implement it in this version of Choir
    public override Size Size { get; } = Size.FromBytes(1);

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeKeyword()}typeinfo{colors.Default}";
}

public sealed class SemaTypeBuiltIn(ChoirContext context, BuiltinTypeKind kind, int bitWidth = 0) : SemaType
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

    public override bool IsBuiltin { get; } = true;
    public override bool IsNumeric { get; } = kind is BuiltinTypeKind.Int or BuiltinTypeKind.IntSized or BuiltinTypeKind.FloatSized
                                                   or (>= BuiltinTypeKind.__FFI_FIRST_INTEGER and <= BuiltinTypeKind.__FFI_LAST_INTEGER)
                                                   or (>= BuiltinTypeKind.__FFI_FIRST_FLOAT and <= BuiltinTypeKind.__FFI_LAST_FLOAT);
    public override bool IsInteger { get; } = kind is BuiltinTypeKind.Int or BuiltinTypeKind.IntSized
                                                   or (>= BuiltinTypeKind.__FFI_FIRST_INTEGER and <= BuiltinTypeKind.__FFI_LAST_INTEGER);
    public override bool IsFloat { get; } = kind is BuiltinTypeKind.FloatSized
                                                 or (>= BuiltinTypeKind.__FFI_FIRST_FLOAT and <= BuiltinTypeKind.__FFI_LAST_FLOAT);

    private static Size ICEOnInvalidSize(ChoirContext context, BuiltinTypeKind kind)
    {
        context.Diag.ICE($"Unhandled case for builtin type-kind {kind}: unknown size");
        throw new UnreachableException();
    }

    private static Align ICEOnInvalidAlign(ChoirContext context, BuiltinTypeKind kind)
    {
        context.Diag.ICE($"Unhandled case for builtin type-kind {kind}: unknown align");
        throw new UnreachableException();
    }

    public override Size Size { get; } = bitWidth != 0 ? Size.FromBits(bitWidth) : kind switch
    {
        BuiltinTypeKind.Void => Size.FromBits(0),
        BuiltinTypeKind.NoReturn => Size.FromBits(0),
        BuiltinTypeKind.Bool => Size.FromBits(8),
        BuiltinTypeKind.Int => context.Target.SizeOfPointer,

        BuiltinTypeKind.FFIBool => context.Target.SizeOfCBool,
        BuiltinTypeKind.FFIChar => context.Target.SizeOfCChar,
        BuiltinTypeKind.FFIShort => context.Target.SizeOfCShort,
        BuiltinTypeKind.FFIInt => context.Target.SizeOfCInt,
        BuiltinTypeKind.FFILong => context.Target.SizeOfCLong,
        BuiltinTypeKind.FFILongLong => context.Target.SizeOfCLongLong,
        BuiltinTypeKind.FFIFloat => context.Target.SizeOfCFloat,
        BuiltinTypeKind.FFIDouble => context.Target.SizeOfCDouble,
        BuiltinTypeKind.FFILongDouble => context.Target.SizeOfCLongDouble,

        _ => ICEOnInvalidSize(context, kind),
    };

    public override Align Align { get; } = bitWidth != 0 ? Align.ForBits(bitWidth) : kind switch
    {
        BuiltinTypeKind.Void => Align.ForBits(0),
        BuiltinTypeKind.NoReturn => Align.ForBits(0),
        BuiltinTypeKind.Bool => Align.ForBits(8),
        BuiltinTypeKind.Int => context.Target.AlignOfPointer,

        BuiltinTypeKind.FFIBool => context.Target.AlignOfCBool,
        BuiltinTypeKind.FFIChar => context.Target.AlignOfCChar,
        BuiltinTypeKind.FFIShort => context.Target.AlignOfCShort,
        BuiltinTypeKind.FFIInt => context.Target.AlignOfCInt,
        BuiltinTypeKind.FFILong => context.Target.AlignOfCLong,
        BuiltinTypeKind.FFILongLong => context.Target.AlignOfCLongLong,
        BuiltinTypeKind.FFIFloat => context.Target.AlignOfCFloat,
        BuiltinTypeKind.FFIDouble => context.Target.AlignOfCDouble,
        BuiltinTypeKind.FFILongDouble => context.Target.AlignOfCLongDouble,

        _ => ICEOnInvalidAlign(context, kind),
    };

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeKeyword()}{Kind.ToLanguageKeywordString(BitWidth)}{colors.Default}";
}

public sealed class SemaTypeElaborated(string[] nameParts, SemaTypeQual namedType) : SemaType
{
    public IReadOnlyList<string> NameParts { get; } = nameParts;
    public SemaTypeQual NamedType { get; } = namedType;

    public override Size Size { get; } = namedType.Type.Size;
    public override Align Align { get; } = namedType.Type.Align;

    public override string ToDebugString(Colors colors)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < NameParts.Count; i++) {
            if (i > 0) builder.Append($"{colors.Default}::");
            if (i == NameParts.Count - 1)
                builder.Append(colors.LayeTypeName());
            else builder.Append(colors.LayeName());
            builder.Append(NameParts[i]);
        }

        return builder.Append(colors.Default).ToString();
    }

    public override IEnumerable<BaseSemaNode> Children { get; } = [namedType];
}

public sealed class SemaTypeTemplateParameter(string parameterName) : SemaType
{
    public string ParameterName { get; } = parameterName;

    public override Size Size { get; } = Size.FromBits(0);
    public override Align Align { get; } = Align.ForBits(0);

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeTemplate()}{ParameterName}{colors.Default}";
}

public abstract class SemaContainerType(SemaTypeQual elementType) : SemaType
{
    public SemaTypeQual ElementType { get; } = elementType;

    public override IEnumerable<BaseSemaNode> Children { get; } = [elementType];
}

public sealed class SemaTypePointer(ChoirContext context, SemaTypeQual elementType) : SemaContainerType(elementType)
{
    public override Size Size { get; } = context.Target.SizeOfPointer;
    public override Align Align { get; } = context.Target.AlignOfPointer;

    public override string ToDebugString(Colors colors) =>
        $"{ElementType.ToDebugString(colors)}{colors.Default}*";
}

public sealed class SemaTypeBuffer(ChoirContext context, SemaTypeQual elementType, byte? terminator = null)
    : SemaContainerType(elementType)
{
    public byte? Terminator { get; } = terminator;

    public override Size Size { get; } = context.Target.SizeOfPointer;
    public override Align Align { get; } = context.Target.AlignOfPointer;

    public override string ToDebugString(Colors colors)
    {
        if (Terminator is byte t)
            return $"{ElementType.ToDebugString(colors)}{colors.Default}[*:{colors.LayeLiteral()}{Terminator}{colors.Default}]";
        else return $"{ElementType.ToDebugString(colors)}{colors.Default}[*]";
    }
}

public sealed class SemaTypeReference(ChoirContext context, SemaTypeQual elementType) : SemaContainerType(elementType)
{
    public override Size Size { get; } = context.Target.SizeOfPointer;
    public override Align Align { get; } = context.Target.AlignOfPointer;

    public override string ToDebugString(Colors colors) =>
        $"{ElementType.ToDebugString(colors)}{colors.Default}&";
}

public sealed class SemaTypeNilable(SemaTypeQual elementType) : SemaContainerType(elementType)
{
    public override Size Size => throw new NotImplementedException();
    public override Align Align => throw new NotImplementedException();

    public override string ToDebugString(Colors colors) =>
        $"{ElementType.ToDebugString(colors)}{colors.Default}?";
}

public sealed class SemaTypeSlice(ChoirContext context, SemaTypeQual elementType) : SemaContainerType(elementType)
{
    public override Size Size { get; } = context.Target.SizeOfPointer + context.Target.SizeOfPointer;
    public override Align Align { get; } = context.Target.AlignOfPointer;

    public override string ToDebugString(Colors colors) =>
        $"{ElementType.ToDebugString(colors)}{colors.Default}[]";
}

public sealed class SemaTypeArray(SemaTypeQual elementType, SemaExpr[] lengths) : SemaContainerType(elementType)
{
    public IReadOnlyList<SemaExpr> Lengths { get; } = lengths;
    public int Arity { get; } = lengths.Length;

    public override Size Size => throw new NotImplementedException();
    public override Align Align => throw new NotImplementedException();

    public override string ToDebugString(Colors colors)
    {
        string lengths = string.Join($"{colors.Default}, ", Lengths.Select(length => $"{colors.LayeLiteral()}{length}"));
        return $"{ElementType.ToDebugString(colors)}{colors.Default}[{lengths}{colors.Default}]";
    }
}

public sealed class SemaTypeErrorPair(SemaTypeQual resultType, SemaTypeQual errorType) : SemaType
{
    public SemaTypeQual ResultType { get; } = resultType;
    public SemaTypeQual ErrorType { get; } = errorType;

    public override Size Size => throw new NotImplementedException();
    public override Align Align => throw new NotImplementedException();

    public override string ToDebugString(Colors colors) =>
        $"{ResultType.ToDebugString(colors)}{colors.Default}!{ErrorType.ToDebugString(colors)}";

    public override IEnumerable<BaseSemaNode> Children { get; } = [resultType, errorType];
}

public sealed class SemaTypeFunction(ChoirContext context, SemaTypeQual returnType, IReadOnlyList<SemaTypeQual> paramTypes)
    : SemaType
{
    public override Size Size { get; } = context.Target.SizeOfPointer;
    public override Align Align { get; } = context.Target.AlignOfPointer;

    public SemaTypeQual ReturnType { get; } = returnType;
    public IReadOnlyList<SemaTypeQual> ParamTypes { get; } = paramTypes;

    public CallingConvention CallingConvention { get; init; } = CallingConvention.Laye;

    public override string ToDebugString(Colors colors)
    {
        var builder = new StringBuilder();
        
        builder.Append(colors.LayeKeyword()).Append("function");

        if (CallingConvention != CallingConvention.Laye)
        {
            builder.Append(' ').Append("callconv").Append(colors.Default).Append('(').Append(colors.LayeName());
            switch (CallingConvention)
            {
                default: throw new InvalidOperationException();
                case CallingConvention.CDecl: builder.Append("cdecl"); break;
                case CallingConvention.StdCall: builder.Append("stdcall"); break;
                case CallingConvention.FastCall: builder.Append("fastcall"); break;
            }

            builder.Append(colors.Default).Append(") ");
        }
        else builder.Append(colors.Default);

        builder.Append('(');
        for (int i = 0; i < ParamTypes.Count; i++)
        {
            if (i > 0) builder.Append(colors.Default).Append(", ");
            builder.Append(ParamTypes[i].ToDebugString(colors));
        }

        builder.Append(colors.Default).Append(')');

        if (ReturnType.Type is not SemaTypeBuiltIn returnTypeBuiltin || returnTypeBuiltin.Kind != BuiltinTypeKind.Void)
        {
            builder.Append(" -> ");
            builder.Append(ReturnType.ToDebugString(colors));
        }

        return builder.Append(colors.Default).ToString();
    }
}

public sealed class SemaTypeDelegate(ChoirContext context, SemaDeclDelegate declDelegate) : SemaType
{
    public override Size Size { get; } = context.Target.SizeOfPointer;
    public override Align Align { get; } = context.Target.AlignOfPointer;

    public override SemaType CanonicalType => DeclDelegate.FunctionType(context);
    public SemaDeclDelegate DeclDelegate { get; } = declDelegate;
    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeTypeName()}{DeclDelegate.Name}{colors.Default}";
}

public sealed class SemaTypeStruct(SemaDeclStruct declStruct) : SemaType
{
    public SemaDeclStruct DeclStruct { get; } = declStruct;

    public override Size Size => throw new NotImplementedException();
    public override Align Align => throw new NotImplementedException();

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeTypeName()}{DeclStruct.Name}{colors.Default}";
}

public sealed class SemaTypeEnum(SemaDeclEnum declEnum) : SemaType
{
    public SemaDeclEnum DeclEnum { get; } = declEnum;
    
    public override Size Size { get; } = Size.FromBits(32);
    public override Align Align { get; } = Align.ForBits(32);

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeTypeName()}{DeclEnum.Name}{colors.Default}";
}

public sealed class SemaTypeAlias(SemaDeclAlias declAlias) : SemaType
{
    public SemaDeclAlias DeclAlias { get; } = declAlias;

    public override Size Size { get; } = declAlias.AliasedType.Type.Size;
    public override Align Align { get; } = declAlias.AliasedType.Type.Align;

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeTypeName()}{DeclAlias.Name}{colors.Default}";
}
