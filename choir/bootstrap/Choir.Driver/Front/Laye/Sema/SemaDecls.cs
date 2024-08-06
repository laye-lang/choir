using System.Numerics;

namespace Choir.Front.Laye.Sema;

public sealed class SemaDeclImportQuery(Location location)
    : SemaDecl(location)
{
}

public sealed class SemaDeclImport(Location location)
    : SemaDecl(location)
{
}

public sealed class SemaDeclOverloadSet(Location location)
    : SemaDecl(location)
{
}

public abstract class SemaDeclTemplateParameter(Location location) : SemaDecl(location);

public sealed class SemaDeclTemplateTypeParameter(Location location, string name, bool isDuckTyped)
    : SemaDeclTemplateParameter(location)
{
    public string Name { get; } = name;
    public bool IsDuckTyped { get; } = isDuckTyped;
    // TODO(local): constraints/contracts/concepts/whatever
}

public sealed class SemaDeclTemplateValueParameter(Location location, string name, SemaTypeQual paramType)
    : SemaDeclTemplateParameter(location)
{
    public string Name { get; } = name;
    public SemaTypeQual ParamType { get; } = paramType;
    // TODO(local): constraints/contracts/concepts/whatever
    
    public override IEnumerable<BaseSemaNode> Children { get; } = [paramType];
}

public sealed class SemaDeclParameter(Location location, string name, SemaTypeQual paramType)
    : SemaDecl(location)
{
    public string Name { get; } = name;
    public SemaTypeQual ParamType { get; } = paramType;
    // TODO(local): evaluated constant for default values, or even arbitrary semantic expressions depending
    
    public override IEnumerable<BaseSemaNode> Children { get; } = [paramType];
}

public sealed class SemaDeclFunction(Location location, string name, SemaTypeQual returnType, SemaDeclParameter[] parameterDecls)
    : SemaDecl(location)
{
    public string Name { get; } = name;
    public SemaTypeQual ReturnType { get; } = returnType;
    public IReadOnlyList<SemaDeclParameter> ParameterDecls { get; } = parameterDecls;
    public IReadOnlyList<SemaDeclTemplateParameter> TemplateParams { get; init; } = [];

    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            yield return ReturnType;
            foreach (var param in ParameterDecls)
                yield return param;
        }
    }
}

public sealed class SemaDeclBinding(Location location, string name, SemaTypeQual bindingType, SemaExpr? initialValue)
    : SemaDecl(location)
{
    public string Name { get; } = name;
    public SemaTypeQual BindingType { get; } = bindingType;
    public SemaExpr? InitialValue { get; } = initialValue;

    public override IEnumerable<BaseSemaNode> Children { get; } = initialValue is not null ? [bindingType, initialValue] : [bindingType];
}

public sealed class SemaDeclField(Location location, string name, SemaTypeQual fieldType)
    : SemaDecl(location)
{
    public string Name { get; } = name;
    public SemaTypeQual FieldType { get; } = fieldType;

    public override IEnumerable<BaseSemaNode> Children { get; } = [fieldType];
}

public sealed class SemaDeclStruct(Location location, string name, SemaDeclField[] fields, SemaDeclStruct[] variants)
    : SemaDecl(location)
{
    public string Name { get; } = name;
    public IReadOnlyList<SemaDeclField> FieldDecls { get; } = fields;
    public IReadOnlyList<SemaDeclStruct> VariantDecls { get; } = variants;
    public IReadOnlyList<SemaDeclTemplateParameter> TemplateParams { get; init; } = [];

    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            foreach (var @field in FieldDecls)
                yield return @field;
            foreach (var variant in VariantDecls)
                yield return variant;
        }
    }
}

public sealed class SemaDeclEnumVariant(Location location, string name, BigInteger value)
    : SemaDecl(location)
{
    public string Name { get; } = name;
    public BigInteger Value { get; } = value;
}

public sealed class SemaDeclEnum(Location location, string name, SemaDeclEnumVariant[] variants)
    : SemaDecl(location)
{
    public string Name { get; } = name;
    public IReadOnlyList<SemaDeclEnumVariant> Variants { get; } = variants;

    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            foreach (var variant in Variants)
                yield return variant;
        }
    }
}

public sealed class SemaDeclAlias(Location location, string name, SemaTypeQual alaisedType, bool isStrict = false)
    : SemaDecl(location)
{
    public string Name { get; } = name;
    public bool IsStrict { get; } = isStrict;
    public SemaTypeQual AliasedType { get; } = alaisedType;
    public IReadOnlyList<SemaDeclTemplateParameter> TemplateParams { get; init; } = [];

    public override IEnumerable<BaseSemaNode> Children
    {
        get
        {
            yield return AliasedType;
        }
    }
}

public sealed class SemaDeclTest(Location location, string description, SemaStmt body)
    : SemaDecl(location)
{
    public string Description { get; } = description;
    public SemaStmt Body { get; } = body;
}
