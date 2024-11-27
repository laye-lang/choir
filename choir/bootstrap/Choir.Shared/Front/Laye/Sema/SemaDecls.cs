using System.Numerics;

namespace Choir.Front.Laye.Sema;

public sealed class SemaDeclTemplateParameters(IReadOnlyList<SemaDeclTemplateParameter> templateParams)
    : BaseSemaNode
{
    public IReadOnlyList<SemaDeclTemplateParameter> TemplateParams { get; } = templateParams;
    public override IEnumerable<BaseSemaNode> Children { get; } = templateParams;
}

public abstract class SemaDeclImportQuery(Location location)
    : SemaDecl(location)
{
}

public sealed class SemaDeclImportQueryWildcard(Location location)
    : SemaDeclImportQuery(location)
{
}

public sealed class SemaDeclImport(Location location, OldModule module, bool isExport, IReadOnlyList<SemaDeclImportQuery> queries)
    : SemaDecl(location)
{
    public OldModule ImportedModule { get; } = module;

    public bool IsExport { get; } = isExport;
    public IReadOnlyList<SemaDeclImportQuery> Queries { get; } = queries;
    public bool IsWildcard => Queries.Count == 1 && Queries[0] is SemaDeclImportQueryWildcard;

    public override IEnumerable<BaseSemaNode> Children { get; } = queries;
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

    public SemaTypeFunction FunctionType(ChoirContext context) => new SemaTypeFunction(context, ReturnType, ParameterDecls.Select(p => p.ParamType).ToArray())
    {
        CallingConvention = CallingConvention,
    };
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
