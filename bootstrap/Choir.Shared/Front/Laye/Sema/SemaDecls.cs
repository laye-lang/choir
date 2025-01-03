using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Choir.Front.Laye.Sema;

public sealed class SemaDeclTemplateParameters(IReadOnlyList<SemaDeclTemplateParameter> templateParams)
    : BaseSemaNode
{
    public IReadOnlyList<SemaDeclTemplateParameter> TemplateParams { get; } = templateParams;
    public override IEnumerable<BaseSemaNode> Children { get; } = templateParams;
}

public abstract class SemaDeclTemplateParameter(Location location, string name) : SemaDeclNamed(location, name);

public sealed class SemaDeclTemplateTypeParameter(Location location, string name, bool isDuckTyped)
    : SemaDeclTemplateParameter(location, name)
{
    public bool IsDuckTyped { get; } = isDuckTyped;
    // TODO(local): constraints/contracts/concepts/whatever
}

public sealed class SemaDeclTemplateValueParameter(Location location, string name, SemaTypeQual paramType)
    : SemaDeclTemplateParameter(location, name)
{
    public SemaTypeQual ParamType { get; } = paramType;
    // TODO(local): constraints/contracts/concepts/whatever
    
    public override IEnumerable<BaseSemaNode> Children { get; } = [paramType];
}

public sealed class SemaDeclParam(Location location, string name, SemaTypeQual paramType)
    : SemaDeclNamed(location, name)
{
    public SemaTypeQual ParamType { get; } = paramType;
    // TODO(local): evaluated constant for default values, or even arbitrary semantic expressions depending
    
    public override IEnumerable<BaseSemaNode> Children { get; } = [paramType];
}

public sealed class SemaDeclFunction(Location location, string name)
    : SemaDeclNamed(location, name)
{
    public SemaTypeQual ReturnType { get; set; } = SemaTypePoison.Instance.Qualified(Location.Nowhere);
    public IReadOnlyList<SemaDeclParam> ParameterDecls { get; set; } = [];
    public SemaDeclTemplateParameters? TemplateParameters { get; set; }
    public SemaStmt? Body { get; set; }

    public CallingConvention CallingConvention { get; set; } = CallingConvention.Laye;
    public bool IsInline { get; set; } = false;
    public bool IsDiscardable { get; set; } = false;

    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            yield return ReturnType;
            foreach (var param in ParameterDecls)
                yield return param;
            if (TemplateParameters is not null)
                yield return TemplateParameters;
            if (Body is not null)
                yield return Body;
        }
    }

    public SemaTypeFunction FunctionType(ChoirContext context) => new(context, ReturnType, ParameterDecls.Select(p => p.ParamType).ToArray())
    {
        CallingConvention = CallingConvention,
    };

    public override SerializedDeclKind SerializedDeclKind { get; } = SerializedDeclKind.Function;
    public override void Serialize(ModuleSerializer serializer, BinaryWriter writer)
    {
        #region Flags

        ushort flags1 = 0;
        ushort flags2 = 0;

        switch (CallingConvention)
        {
            default:
            {
                serializer.Context.Unreachable($"Unhandled calling convention in serializer: {CallingConvention}.");
                throw new UnreachableException();
            }

            case CallingConvention.CDecl: flags1 |= SerializerConstants.Attrib1CallingConventionCDecl; break;
            case CallingConvention.Laye: flags1 |= SerializerConstants.Attrib1CallingConventionLaye; break;
            case CallingConvention.StdCall: flags1 |= SerializerConstants.Attrib1CallingConventionStdCall; break;
            case CallingConvention.FastCall: flags1 |= SerializerConstants.Attrib1CallingConventionFastCall; break;
        }

        if (IsForeign) flags1 |= SerializerConstants.Attrib1ForeignFlag;
        if (IsInline) flags1 |= SerializerConstants.Attrib1InlineFlag;
        if (IsDiscardable) flags1 |= SerializerConstants.Attrib1DiscardableFlag;

        // TODO(local): variadic-ness in the next flag

        if (flags2 != 0)
            flags1 |= SerializerConstants.AttribExtensionFlag;

        writer.Write(flags1);
        if (flags2 != 0)
            writer.Write(flags2);

        #endregion

        #region Flags Data

        if (IsForeign)
            serializer.WriteAtom(writer, ForeignSymbolName);

        #endregion

        #region Template Parameters

        if (TemplateParameters is { } templateParams)
        {
            serializer.Context.Todo("Serialize function template parameters");
            throw new UnreachableException();
        }
        // assume we want to write the number of template parameters as a 7-bit encoded int, so start with 0
        else writer.Write((byte)0);

        #endregion

        #region Return Type

        serializer.WriteTypeQual(writer, ReturnType);

        #endregion

        #region Parameters

        writer.Write((ushort)ParameterDecls.Count);
        for (int i = 0; i < ParameterDecls.Count; i++)
        {
            var param = ParameterDecls[i];
            serializer.WriteAtom(writer, param.Name);
            serializer.WriteLocation(writer, param.Location);
            serializer.WriteTypeQual(writer, param.ParamType);
        }

        #endregion
    }

    public override void Deserialize(ModuleDeserializer deserializer, BinaryReader reader)
    {
        #region Flags

        ushort flags1 = reader.ReadUInt16();
        CallingConvention = (flags1 & SerializerConstants.Attrib1CallingConventionMask) switch
        {
            SerializerConstants.Attrib1CallingConventionCDecl => CallingConvention.CDecl,
            SerializerConstants.Attrib1CallingConventionLaye => CallingConvention.Laye,
            SerializerConstants.Attrib1CallingConventionStdCall => CallingConvention.StdCall,
            SerializerConstants.Attrib1CallingConventionFastCall => CallingConvention.FastCall,
            _ => throw new UnreachableException(),
        };

        IsForeign = 0 != (flags1 & SerializerConstants.Attrib1ForeignFlag);
        IsInline = 0 != (flags1 & SerializerConstants.Attrib1InlineFlag);
        IsDiscardable = 0 != (flags1 & SerializerConstants.Attrib1DiscardableFlag);

        if (0 != (flags1 & SerializerConstants.AttribExtensionFlag))
        {
            ushort flags2 = reader.ReadUInt16();
            deserializer.Context.Unreachable("No flags2 deserialization");
        }

        #endregion

        #region Flags Data

        if (IsForeign)
            ForeignSymbolName = deserializer.ReadAtom(reader);

        #endregion

        #region Template Parameters

        int templateParamCount = reader.Read7BitEncodedInt();
        deserializer.Context.Assert(templateParamCount == 0, "Template parameters are not currently supported in the serializer");

        #endregion

        #region Return Type

        ReturnType = deserializer.ReadTypeQual(reader);

        #endregion

        #region Parameters

        int paramCount = reader.ReadUInt16();
        var paramDecls = new SemaDeclParam[paramCount];
        for (int i = 0; i < paramCount; i++)
        {
            string paramName = deserializer.ReadAtom(reader)!;
            var location = deserializer.ReadLocation(reader);
            var paramType = deserializer.ReadTypeQual(reader);
            paramDecls[i] = new SemaDeclParam(location, paramName, paramType);
        }

        ParameterDecls = paramDecls;

        #endregion
    }
}

public sealed class SemaDeclDelegate(Location location, string name)
    : SemaDeclNamed(location, name)
{
    public SemaTypeQual ReturnType { get; set; } = SemaTypePoison.Instance.Qualified(Location.Nowhere);
    public IReadOnlyList<SemaDeclParam> ParameterDecls { get; set; } = [];
    public SemaDeclTemplateParameters? TemplateParameters { get; set; }
    
    public CallingConvention CallingConvention { get; set; } = CallingConvention.Laye;

    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            yield return ReturnType;
            foreach (var param in ParameterDecls)
                yield return param;
            if (TemplateParameters is not null)
                yield return TemplateParameters;
        }
    }

    public SemaTypeFunction FunctionType(ChoirContext context) => new SemaTypeFunction(context, ReturnType, ParameterDecls.Select(p => p.ParamType).ToArray())
    {
        CallingConvention = CallingConvention,
    };
}

public sealed class SemaDeclBinding(Location location, string name)
    : SemaDeclNamed(location, name)
{
    public SemaTypeQual BindingType { get; set; } = SemaTypePoison.Instance.Qualified(Location.Nowhere);
    public SemaExpr? InitialValue { get; set; }

    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            yield return BindingType;
            if (InitialValue is not null)
                yield return InitialValue;
        }
    }
}

public sealed class SemaDeclField(Location location, string name, SemaTypeQual fieldType)
    : SemaDeclNamed(location, name)
{
    public SemaTypeQual FieldType { get; } = fieldType;

    public override IEnumerable<BaseSemaNode> Children { get; } = [fieldType];
}

public sealed class SemaDeclStruct(Location location, string name)
    : SemaDeclNamed(location, name)
{
    public IReadOnlyList<SemaDeclField> FieldDecls { get; set; } = [];
    public IReadOnlyList<SemaDeclStruct> VariantDecls { get; set; } = [];
    public SemaDeclTemplateParameters? TemplateParameters { get; init; }

    public required SemaDeclStruct? ParentStruct { get; init; }
    public required Scope Scope { get; init; }

    public bool IsVariant => ParentStruct is not null;
    public bool IsLeaf => VariantDecls.Count == 0;
    
    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            foreach (var @field in FieldDecls)
                yield return @field;
            foreach (var variant in VariantDecls)
                yield return variant;
            if (TemplateParameters is not null)
                yield return TemplateParameters;
        }
    }

    public bool TryLookupField(string fieldName, [NotNullWhen(true)] out SemaDeclField? declField, out Size fieldOffset)
    {
        fieldOffset = Size.FromBytes(0);
        var currentAlignment = Align.ByteAligned;

        for (int i = 0; i < FieldDecls.Count; i++)
        {
            fieldOffset = fieldOffset.AlignedTo(currentAlignment);
            if (FieldDecls[i].Name == fieldName)
            {
                declField = FieldDecls[i];
                return true;
            }

            currentAlignment = Align.Max(currentAlignment, FieldDecls[i].FieldType.Align);
        }

        declField = null;
        fieldOffset = Size.FromBytes(-1);
        return false;
    }
}

public sealed class SemaDeclEnumVariant(Location location, string name, BigInteger value)
    : SemaDeclNamed(location, name)
{
    public BigInteger Value { get; } = value;
}

public sealed class SemaDeclEnum(Location location, string name)
    : SemaDeclNamed(location, name)
{
    public IReadOnlyList<SemaDeclEnumVariant> Variants { get; set; } = [];

    public required Scope Scope { get; init; }

    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            foreach (var variant in Variants)
                yield return variant;
        }
    }
}

public sealed class SemaDeclAlias(Location location, string name, bool isStrict = false)
    : SemaDeclNamed(location, name)
{
    public bool IsStrict { get; set; } = isStrict;
    public SemaTypeQual AliasedType { get; set; } = SemaTypePoison.Instance.Qualified(Location.Nowhere);
    public SemaDeclTemplateParameters? TemplateParameters { get; set; }

    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            yield return AliasedType;
            if (TemplateParameters is not null)
                yield return TemplateParameters;
        }
    }
}

public sealed class SemaDeclTest(Location location, string description, SemaStmt body)
    : SemaDecl(location)
{
    public string Description { get; } = description;
    public SemaStmt Body { get; } = body;
}
