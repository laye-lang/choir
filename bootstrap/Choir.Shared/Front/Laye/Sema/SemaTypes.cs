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

public sealed class SemaTypePoison : SemaType<SemaTypePoison>
{
    public static readonly SemaTypePoison Instance = new();
    public static readonly SemaTypeQual InstanceQualified = Instance.Qualified(Location.Nowhere);

    public override bool IsPoison { get; } = true;
    public override Size Size { get; } = Size.FromBytes(1);
    public override string ToDebugString(Colors colors) =>
        $"{colors.Red}POISON{colors.Default}";

    public override bool Equals(BaseSemaNode? other) => false;
    public override bool TypeEquals(SemaTypePoison other, TypeComparison comp) => false;
}

public sealed class SemaTypeTypeInfo : SemaType<SemaTypeTypeInfo>
{
    public static readonly SemaTypeTypeInfo Instance = new();

    // TODO(local): TypeInfo needs a size... if we even implement it in this version of Choir
    public override Size Size { get; } = Size.FromBytes(1);

    private SemaTypeTypeInfo() { }

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeKeyword()}typeinfo{colors.Default}";

    public override bool TypeEquals(SemaTypeTypeInfo other, TypeComparison comp) => true;
}

public sealed class SemaTypeBuiltIn(ChoirContext context, BuiltinTypeKind kind, int bitWidth = 0)
    : SemaType<SemaTypeBuiltIn>
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
    public override bool IsVoid { get; } = kind is BuiltinTypeKind.Void;
    public override bool IsNoReturn { get; } = kind is BuiltinTypeKind.NoReturn;
    public override bool IsBool { get; } = kind is BuiltinTypeKind.Bool;
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
        BuiltinTypeKind.Int => context.Target.SizeOfSizeType,

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
        BuiltinTypeKind.Int => context.Target.AlignOfSizeType,

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

    public override bool TypeEquals(SemaTypeBuiltIn other, TypeComparison comp) => other.Kind == Kind && (!IsExplicitlySized || other.BitWidth == BitWidth);

    public override SerializedTypeKind SerializedTypeKind => Kind switch
    {
        BuiltinTypeKind.Void => SerializedTypeKind.Void,
        BuiltinTypeKind.NoReturn => SerializedTypeKind.NoReturn,
        BuiltinTypeKind.Bool => SerializedTypeKind.Bool,
        BuiltinTypeKind.BoolSized => SerializedTypeKind.BoolSized,
        BuiltinTypeKind.Int => SerializedTypeKind.Int,
        BuiltinTypeKind.IntSized => SerializedTypeKind.IntSized,
        BuiltinTypeKind.FloatSized => Size.Bits switch
        {
            32 => SerializedTypeKind.Float32,
            64 => SerializedTypeKind.Float64,
            _ => throw new UnreachableException(),
        },
        BuiltinTypeKind.FFIBool => SerializedTypeKind.FFI,
        BuiltinTypeKind.FFIChar => SerializedTypeKind.FFI,
        BuiltinTypeKind.FFIShort => SerializedTypeKind.FFI,
        BuiltinTypeKind.FFIInt => SerializedTypeKind.FFI,
        BuiltinTypeKind.FFILong => SerializedTypeKind.FFI,
        BuiltinTypeKind.FFILongLong => SerializedTypeKind.FFI,
        BuiltinTypeKind.FFIFloat => SerializedTypeKind.FFI,
        BuiltinTypeKind.FFIDouble => SerializedTypeKind.FFI,
        BuiltinTypeKind.FFILongDouble => SerializedTypeKind.FFI,
        _ => throw new UnreachableException(),
    };

    public override void Serialize(ModuleSerializer serializer, BinaryWriter writer)
    {
        switch (Kind)
        {
            default: break;
            case BuiltinTypeKind.BoolSized: writer.Write((ushort)Size.Bits); break;
            case BuiltinTypeKind.IntSized: writer.Write((ushort)Size.Bits); break;
            case BuiltinTypeKind.FFIBool: writer.Write(SerializerConstants.FFIBoolTypeSigil); break;
            case BuiltinTypeKind.FFIChar: writer.Write(SerializerConstants.FFICharTypeSigil); break;
            case BuiltinTypeKind.FFIShort: writer.Write(SerializerConstants.FFIShortTypeSigil); break;
            case BuiltinTypeKind.FFIInt: writer.Write(SerializerConstants.FFIIntTypeSigil); break;
            case BuiltinTypeKind.FFILong: writer.Write(SerializerConstants.FFILongTypeSigil); break;
            case BuiltinTypeKind.FFILongLong: writer.Write(SerializerConstants.FFILongLongTypeSigil); break;
            case BuiltinTypeKind.FFIFloat: writer.Write(SerializerConstants.FFIFloatTypeSigil); break;
            case BuiltinTypeKind.FFIDouble: writer.Write(SerializerConstants.FFIDoubleTypeSigil); break;
            case BuiltinTypeKind.FFILongDouble: writer.Write(SerializerConstants.FFILongDoubleTypeSigil); break;
        }
    }
}

public sealed class SemaTypeOverloadSet
    : SemaType<SemaTypeOverloadSet>
{
    public static SemaTypeOverloadSet Instance { get; } = new();

    public override Size Size { get; } = Size.FromBytes(1);
    public override Align Align { get; } = Align.ByteAligned;

    private SemaTypeOverloadSet()
    {
    }

    public override string ToDebugString(Colors colors) => $"{colors.LayeKeyword()}overloads";
    public override bool TypeEquals(SemaTypeOverloadSet other, TypeComparison comp = TypeComparison.WithIdenticalQualifiers) => false;
}

#if false
public sealed class SemaTypeElaborated(string[] nameParts, SemaTypeQual namedType)
    : SemaType<SemaTypeElaborated>
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

    public override bool TypeEquals(SemaTypeElaborated other, TypeComparison comp) => NamedType.CanonicalType.TypeEquals(other.NamedType.CanonicalType, comp);
}
#endif

public sealed class SemaTypeTemplateParameter(string parameterName)
    : SemaType<SemaTypeTemplateParameter>
{
    public string ParameterName { get; } = parameterName;

    public override Size Size { get; } = Size.FromBits(0);
    public override Align Align { get; } = Align.ForBits(0);

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeTemplate()}{ParameterName}{colors.Default}";

    public override bool TypeEquals(SemaTypeTemplateParameter other, TypeComparison comp) => Id == other.Id;
}

public sealed class SemaTypeNil
    : SemaType<SemaTypeNil>
{
    public static readonly SemaTypeNil Instance = new();
    public static readonly SemaTypeQual InstanceQualified = Instance.Qualified(Location.Nowhere);

    public override Size Size { get; } = Size.FromBits(0);
    public override Align Align { get; } = Align.ForBits(0);

    private SemaTypeNil()
        : base()
    {
    }

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeKeyword()}nil{colors.Default}";

    public override bool TypeEquals(SemaTypeNil other, TypeComparison comp) => Id == other.Id;
}

public abstract class SemaContainerType<T>(SemaTypeQual elementType)
    : SemaType<T>
    where T : SemaContainerType<T>
{
    public SemaTypeQual ElementType { get; } = elementType;

    public override IEnumerable<BaseSemaNode> Children { get; } = [elementType];

    public override bool TypeEquals(T other, TypeComparison comp) => ElementType.CanonicalType.TypeEquals(other.ElementType.CanonicalType, comp);
}

public sealed class SemaTypePointer(ChoirContext context, SemaTypeQual elementType)
    : SemaContainerType<SemaTypePointer>(elementType)
{
    public override bool IsPointer { get; } = true;

    public override Size Size { get; } = context.Target.SizeOfPointer;
    public override Align Align { get; } = context.Target.AlignOfPointer;

    public override string ToDebugString(Colors colors) =>
        $"{ElementType.ToDebugString(colors)}{colors.Default}*";

    public override SerializedTypeKind SerializedTypeKind => SerializedTypeKind.Pointer;
    public override void Serialize(ModuleSerializer serializer, BinaryWriter writer)
    {
        serializer.WriteTypeQual(writer, ElementType);
    }

    public static SemaTypePointer Deserialize(ModuleDeserializer deserializer, BinaryReader reader)
    {
        var elementType = deserializer.ReadTypeQual(reader);
        return new SemaTypePointer(deserializer.Context, elementType);
    }
}

public sealed class SemaTypeBuffer(ChoirContext context, SemaTypeQual elementType, SemaExpr? terminator = null)
    : SemaContainerType<SemaTypeBuffer>(elementType)
{
    public SemaExpr? Terminator { get; } = terminator;
    
    public override bool IsBuffer { get; } = true;

    public override Size Size { get; } = context.Target.SizeOfPointer;
    public override Align Align { get; } = context.Target.AlignOfPointer;

    public override string ToDebugString(Colors colors)
    {
        if (Terminator is null)
            return $"{ElementType.ToDebugString(colors)}{colors.Default}[*]";
        else if (Terminator is SemaExprLiteralInteger li)
            return $"{ElementType.ToDebugString(colors)}{colors.Default}[*:{colors.LayeLiteral()}{li.LiteralValue}{colors.Default}]";
        else
        {
            context.Diag.ICE(Terminator.Location, "Invalid or unsupported sentinel terminator for buffer");
            return "";
        }
    }

    public override bool TypeEquals(SemaTypeBuffer other, TypeComparison comp)
    {
        if (!base.TypeEquals(other, comp))
            return false;

        context.Assert(Terminator is null && other.Terminator is null, "Currently can't compare equality of two buffer types when at least one has a terminator specified.");
        return true;
    }

    public override SerializedTypeKind SerializedTypeKind => SerializedTypeKind.Buffer;
    public override void Serialize(ModuleSerializer serializer, BinaryWriter writer)
    {
        serializer.Context.Assert(Terminator is null, "Dunno how to handle terminators, if we keep them.");
        serializer.WriteTypeQual(writer, ElementType);
    }

    public static SemaTypeBuffer Deserialize(ModuleDeserializer deserializer, BinaryReader reader)
    {
        var elementType = deserializer.ReadTypeQual(reader);
        return new SemaTypeBuffer(deserializer.Context, elementType);
    }
}

public sealed class SemaTypeNilable(SemaTypeQual elementType)
    : SemaContainerType<SemaTypeNilable>(elementType)
{
    public override bool IsNilable { get; } = true;

    public override Size Size => throw new NotImplementedException();
    public override Align Align => throw new NotImplementedException();

    public override string ToDebugString(Colors colors) =>
        $"{ElementType.ToDebugString(colors)}{colors.Default}?";

    public override SerializedTypeKind SerializedTypeKind => SerializedTypeKind.Nilable;
    public override void Serialize(ModuleSerializer serializer, BinaryWriter writer)
    {
        serializer.WriteTypeQual(writer, ElementType);
    }

    public static SemaTypeNilable Deserialize(ModuleDeserializer deserializer, BinaryReader reader)
    {
        var elementType = deserializer.ReadTypeQual(reader);
        return new SemaTypeNilable(elementType);
    }
}

public sealed class SemaTypeSlice(ChoirContext context, SemaTypeQual elementType)
    : SemaContainerType<SemaTypeSlice>(elementType)
{
    public override bool IsSlice { get; } = true;

    public override Size Size { get; } = context.Target.SizeOfPointer + context.Target.SizeOfSizeType;
    public override Align Align { get; } = Align.Max(context.Target.AlignOfPointer, context.Target.AlignOfSizeType);

    public override string ToDebugString(Colors colors) =>
        $"{ElementType.ToDebugString(colors)}{colors.Default}[]";

    public override SerializedTypeKind SerializedTypeKind => SerializedTypeKind.Slice;
    public override void Serialize(ModuleSerializer serializer, BinaryWriter writer)
    {
        serializer.WriteTypeQual(writer, ElementType);
    }

    public static SemaTypeSlice Deserialize(ModuleDeserializer deserializer, BinaryReader reader)
    {
        var elementType = deserializer.ReadTypeQual(reader);
        return new SemaTypeSlice(deserializer.Context, elementType);
    }
}

public sealed class SemaTypeArray(ChoirContext context, SemaTypeQual elementType, SemaExpr[] lengthExprs)
    : SemaContainerType<SemaTypeArray>(elementType)
{
    public IReadOnlyList<SemaExpr> LengthExprs { get; } = lengthExprs;
    public IReadOnlyList<long> Lengths { get; } = lengthExprs.Select(e =>
    {
        context.Assert(e is SemaExprEvaluatedConstant, "Expressions in an array type must be evaluated constants.");
        var eConst = (SemaExprEvaluatedConstant)e;
        context.Assert(eConst.Value.Kind == EvaluatedConstantKind.Integer, "Expressions in an array type must be evaluated integer constants.");
        context.Assert(eConst.Value.IntegerValue >= 0, "Expressions in an array type must be evaluated non-negative integer constants.");
        context.Assert(eConst.Value.IntegerValue.GetBitLength() <= 64, "Expressions in an array type must be evaluated non-negative integer constants that fith within 64 bits.");
        return (long)eConst.Value.IntegerValue;
    }).ToArray();

    public int Arity { get; } = lengthExprs.Length;

    public override bool IsArray { get; } = true;

    // TODO(local): don't cast to int here, use long for sizes
    public override Size Size => ElementType.Size * (int)FlatLength;
    public override Align Align => ElementType.Align;

    public long FlatLength => Lengths.Aggregate(1L, (agg, l) => agg * l);

    public override string ToDebugString(Colors colors)
    {
        string lengths = string.Join($"{colors.Default}, ", Lengths.Select(length => $"{colors.LayeLiteral()}{length}"));
        return $"{ElementType.ToDebugString(colors)}{colors.Default}[{lengths}{colors.Default}]";
    }

    public override bool TypeEquals(SemaTypeArray other, TypeComparison comp) => base.TypeEquals(other, comp) && Lengths.SequenceEqual(other.Lengths);
}

public sealed class SemaTypeErrorPair(SemaTypeQual resultType, SemaTypeQual errorType) 
   : SemaType<SemaTypeErrorPair>
{
    public SemaTypeQual ResultType { get; } = resultType;
    public SemaTypeQual ErrorType { get; } = errorType;

    public override Size Size => throw new NotImplementedException();
    public override Align Align => throw new NotImplementedException();

    public override IEnumerable<BaseSemaNode> Children { get; } = [resultType, errorType];

    public override string ToDebugString(Colors colors) =>
        $"{ResultType.ToDebugString(colors)}{colors.Default}!{ErrorType.ToDebugString(colors)}";

    public override bool TypeEquals(SemaTypeErrorPair other, TypeComparison comp) => ResultType.CanonicalType.TypeEquals(other.ResultType.CanonicalType, comp) && ErrorType.CanonicalType.TypeEquals(other.ErrorType.CanonicalType, comp);
}

public sealed class SemaTypeFunction(ChoirContext context, SemaTypeQual returnType, IReadOnlyList<SemaDeclParam> paramDecls)
    : SemaType<SemaTypeFunction>
{
    public override Size Size { get; } = context.Target.SizeOfPointer;
    public override Align Align { get; } = context.Target.AlignOfPointer;

    public SemaTypeQual ReturnType { get; } = returnType;
    public IReadOnlyList<SemaDeclParam> ParamDecls { get; } = paramDecls;
    public IReadOnlyList<SemaTypeQual> ParamTypes { get; } = paramDecls.Select(d => d.ParamType).ToArray();

    public CallingConvention CallingConvention { get; init; } = CallingConvention.Laye;
    public VarargsKind VarargsKind { get; init; } = VarargsKind.None;
    public bool IsDiscardable { get; init; } = false;

    public override string ToDebugString(Colors colors)
    {
        var builder = new StringBuilder();

        builder.Append(colors.LayeKeyword()).Append("function").Append(colors.Default);

        if (CallingConvention != CallingConvention.Laye)
        {
            builder.Append(' ').Append(colors.LayeKeyword()).Append("callconv").Append(colors.Default).Append('(').Append(colors.LayeName());
            switch (CallingConvention)
            {
                default: throw new InvalidOperationException();
                case CallingConvention.CDecl: builder.Append("cdecl"); break;
                case CallingConvention.StdCall: builder.Append("stdcall"); break;
                case CallingConvention.FastCall: builder.Append("fastcall"); break;
            }

            builder.Append(colors.Default).Append(") ");
        }

        if (IsDiscardable)
            builder.Append(' ').Append(colors.LayeKeyword()).Append("discardable").Append(colors.Default);

        builder.Append('(');
        for (int i = 0; i < ParamTypes.Count; i++)
        {
            if (i > 0) builder.Append(colors.Default).Append(", ");
            if (ParamDecls[i].IsRefParam)
                builder.Append(colors.LayeKeyword()).Append("ref ");
            if (VarargsKind == VarargsKind.Laye && i == ParamTypes.Count - 1)
                builder.Append(colors.LayeKeyword()).Append("varargs ");
            builder.Append(ParamTypes[i].ToDebugString(colors));
            if (!ParamDecls[i].Name.IsNullOrEmpty())
                builder.Append(' ').Append(colors.Default).Append(ParamDecls[i].Name);
        }

        if (VarargsKind == VarargsKind.C)
        {
            if (ParamTypes.Count > 0)
                builder.Append(colors.Default).Append(", ");
            builder.Append(colors.LayeKeyword()).Append("varargs");
        }

        builder.Append(colors.Default).Append(')');

        if (ReturnType.Type is not SemaTypeBuiltIn returnTypeBuiltin || returnTypeBuiltin.Kind != BuiltinTypeKind.Void)
        {
            builder.Append(" -> ");
            builder.Append(ReturnType.ToDebugString(colors));
        }

        return builder.Append(colors.Default).ToString();
    }

    public override bool TypeEquals(SemaTypeFunction other, TypeComparison comp)
    {
        if (CallingConvention != other.CallingConvention)
            return false;

        if (!ReturnType.CanonicalType.TypeEquals(other.ReturnType.CanonicalType, comp))
            return false;

        if (ParamTypes.Count != other.ParamTypes.Count)
            return false;

        for (int i = 0; i < ParamTypes.Count; i++)
        {
            if (!ParamTypes[i].CanonicalType.TypeEquals(other.ParamTypes[i].CanonicalType, comp))
                return false;
        }

        return true;
    }
}

public sealed class SemaTypeDelegate(ChoirContext context, SemaDeclDelegate declDelegate)
    : SemaType<SemaTypeDelegate>
{
    public override Size Size { get; } = context.Target.SizeOfPointer;
    public override Align Align { get; } = context.Target.AlignOfPointer;

    public override SemaTypeFunction CanonicalType => DeclDelegate.FunctionType(context);
    public SemaDeclDelegate DeclDelegate { get; } = declDelegate;

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeTypeName()}{DeclDelegate.Name}{colors.Default}";

    public override bool TypeEquals(SemaTypeDelegate other, TypeComparison comp) => CanonicalType.TypeEquals(other.CanonicalType, comp);
}

public sealed class SemaTypeStruct(SemaDeclStruct declStruct)
    : SemaType<SemaTypeStruct>
{
    public SemaDeclStruct DeclStruct { get; } = declStruct;

    public override Size Size => DeclStruct.Size;
    public override Align Align => DeclStruct.Align;

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeTypeName()}{DeclStruct.Name}{colors.Default}";

    public override bool TypeEquals(SemaTypeStruct other, TypeComparison comp = TypeComparison.WithIdenticalQualifiers)
    {
        return DeclStruct == other.DeclStruct;
    }
}

public sealed class SemaTypeEnum(SemaDeclEnum declEnum)
    : SemaType<SemaTypeEnum>
{
    public SemaDeclEnum DeclEnum { get; } = declEnum;
    
    public override Size Size { get; } = Size.FromBits(32);
    public override Align Align { get; } = Align.ForBits(32);

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeTypeName()}{DeclEnum.Name}{colors.Default}";

    public override bool TypeEquals(SemaTypeEnum other, TypeComparison comp = TypeComparison.WithIdenticalQualifiers)
    {
        throw new NotImplementedException();
    }
}

public sealed class SemaTypeAlias(SemaDeclAlias declAlias)
    : SemaType<SemaTypeAlias>
{
    public SemaDeclAlias DeclAlias { get; } = declAlias;

    public override Size Size => DeclAlias.AliasedType.Type.Size;
    public override Align Align => DeclAlias.AliasedType.Type.Align;

    public override SemaType CanonicalType => DeclAlias.AliasedType.CanonicalType.Type;

    public override string ToDebugString(Colors colors) =>
        $"{colors.LayeTypeName()}{DeclAlias.Name}{colors.Default}";

    public override bool TypeEquals(SemaTypeAlias other, TypeComparison comp = TypeComparison.WithIdenticalQualifiers)
    {
        throw new NotImplementedException();
    }
}

public abstract class SemaTypeLiteral<T>
    : SemaType<T>
    where T : SemaTypeLiteral<T>
{
    public override bool IsLiteral { get; } = true;
    public override Size Size { get; } = Size.FromBits(0);
    public override bool TypeEquals(T other, TypeComparison comp = TypeComparison.WithIdenticalQualifiers) => other.Id == Id;
}

public sealed class SemaTypeLiteralBool
    : SemaTypeLiteral<SemaTypeLiteralBool>
{
    public static readonly SemaTypeLiteralBool Instance = new();

    public override bool IsBool { get; } = true;

    private SemaTypeLiteralBool()
    {
    }

    public override string ToDebugString(Colors colors) => $"{colors.LayeKeyword()}<literal-bool>";
}

public sealed class SemaTypeLiteralInteger
    : SemaTypeLiteral<SemaTypeLiteralInteger>
{
    public static readonly SemaTypeLiteralInteger Instance = new();

    public override bool IsInteger { get; } = true;

    private SemaTypeLiteralInteger()
    {
    }

    public override string ToDebugString(Colors colors) => $"{colors.LayeKeyword()}<literal-integer>";
}

public sealed class SemaTypeLiteralFloat
    : SemaTypeLiteral<SemaTypeLiteralFloat>
{
    public static readonly SemaTypeLiteralFloat Instance = new();

    public override bool IsFloat { get; } = true;

    private SemaTypeLiteralFloat()
    {
    }

    public override string ToDebugString(Colors colors) => $"{colors.LayeKeyword()}<literal-float>";
}

public sealed class SemaTypeLiteralString
    : SemaTypeLiteral<SemaTypeLiteralString>
{
    public static readonly SemaTypeLiteralString Instance = new();

    private SemaTypeLiteralString()
    {
    }

    public override string ToDebugString(Colors colors) => $"{colors.LayeKeyword()}<literal-string>";
}
