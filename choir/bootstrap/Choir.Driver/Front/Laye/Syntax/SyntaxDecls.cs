namespace Choir.Front.Laye.Syntax;

public enum ImportKind
{
    Invalid,
    FilePath,
    Library,
}

public abstract class SyntaxTemplateParam(Location location) : SyntaxNode(location)
{
    public required SyntaxNode? DefaultValue { get; init; }
}

public sealed class SyntaxTemplateParamType(SyntaxToken tokenName)
    : SyntaxTemplateParam(tokenName.Location)
{
    public SyntaxToken TokenName { get; } = tokenName;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return TokenName;
            if (DefaultValue is not null)
                yield return DefaultValue;
        }
    }
}

public sealed class SyntaxTemplateParamDuckType(SyntaxToken tokenVar, SyntaxToken tokenName)
    : SyntaxTemplateParam(tokenName.Location)
{
    public SyntaxToken TokenVar { get; } = tokenVar;
    public SyntaxToken TokenName { get; } = tokenName;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return TokenVar;
            yield return TokenName;
            if (DefaultValue is not null)
                yield return DefaultValue;
        }
    }
}

public sealed class SyntaxTemplateParamValue(SyntaxNode type, SyntaxToken tokenName)
    : SyntaxTemplateParam(tokenName.Location)
{
    public SyntaxNode Type { get; } = type;
    public SyntaxToken TokenName { get; } = tokenName;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return Type;
            yield return TokenName;
            if (DefaultValue is not null)
                yield return DefaultValue;
        }
    }
}

public sealed class SyntaxTemplateParams(SyntaxToken tokenTemplate, IReadOnlyList<SyntaxTemplateParam> templateParams)
    : SyntaxNode(tokenTemplate.Location)
{
    public SyntaxToken TokenTemplate { get; } = tokenTemplate;
    public IReadOnlyList<SyntaxTemplateParam> TemplateParams { get; } = templateParams;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenTemplate, .. templateParams];
}

public abstract class SyntaxImportQuery(Location location) : SyntaxNode(location)
{
}

public sealed class SyntaxImportQueryWildcard(SyntaxToken tokenStar) : SyntaxImportQuery(tokenStar.Location)
{
    public SyntaxToken TokenStar { get; } = tokenStar;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenStar];
}

public sealed class SyntaxImportQueryNamed(SyntaxNameref query, SyntaxToken? tokenAs, SyntaxToken? tokenAlias) : SyntaxImportQuery(query.Location)
{
    public SyntaxNameref Query { get; } = query;
    public SyntaxToken? TokenAs { get; } = tokenAs;
    public SyntaxToken? TokenAlias { get; } = tokenAlias;

    public bool IsAliased => TokenAlias is not null;

    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return Query;
            if (TokenAs is not null)
                yield return TokenAs;
            if (TokenAlias is not null)
                yield return TokenAlias;
        }
    }
}

public sealed class SyntaxImportCFlags(SyntaxToken tokenCFlags, SyntaxToken tokenOpenBrace, IReadOnlyList<SyntaxToken> flags, SyntaxToken tokenCloseBrace)
    : SyntaxNode(tokenCFlags.Location)
{
    public SyntaxToken TokenCFlags { get; } = tokenCFlags;
    public SyntaxToken TokenOpenBrace { get; } = tokenOpenBrace;
    public IReadOnlyList<SyntaxToken> Flags { get; } = flags;
    public SyntaxToken TokenCloseBrace { get; } = tokenCloseBrace;

    public override IEnumerable<SyntaxNode> Children { get; } = [tokenCFlags, tokenOpenBrace, .. flags, tokenCloseBrace];
}

public sealed class SyntaxDeclImport(SyntaxToken tokenImport) : SyntaxNode(tokenImport.Location)
{
    public required ImportKind ImportKind { get; init; }
    public SyntaxToken TokenImport { get; } = tokenImport;
    public required IReadOnlyList<SyntaxImportQuery> Queries { get; init; }
    public SyntaxToken? TokenFrom { get; init; }
    public required SyntaxToken TokenModuleName { get; init; }
    public SyntaxToken? TokenAs { get; init; }
    public SyntaxToken? TokenAlias { get; init; }
    public required SyntaxImportCFlags? CFlags { get; init; }
    public required SyntaxToken? TokenSemiColon { get; init; }

    public string ModuleNameText => TokenModuleName.TextValue;
    public bool IsAliased => TokenAlias is not null;
    public string AliasNameText => TokenAlias?.TextValue ?? "";

    public override bool IsDecl { get; } = true;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return TokenImport;
            foreach (var query in Queries)
                yield return query;
            if (TokenFrom is not null)
                yield return TokenFrom;
            yield return TokenModuleName;
            if (TokenAs is not null)
                yield return TokenAs;
            if (TokenAlias is not null)
                yield return TokenAlias;
            if (CFlags is not null)
                yield return CFlags;
            if (TokenSemiColon is not null)
                yield return TokenSemiColon;
        }
    }
}

public sealed class SyntaxDeclFunction(SyntaxNode returnType, SyntaxToken tokenName, IReadOnlyList<SyntaxNode> parameters)
    : SyntaxNode(tokenName.Location)
{
    public required SyntaxTemplateParams? TemplateParams { get; init; }
    public required IReadOnlyList<SyntaxAttrib> Attribs { get; init; }

    public SyntaxNode ReturnType { get; } = returnType;
    public SyntaxToken TokenName { get; } = tokenName;
    public IReadOnlyList<SyntaxNode> Params { get; } = parameters;

    public SyntaxNode? Body { get; init; } = null;
    public SyntaxToken? TokenSemiColon { get; init; } = null;

    public override bool IsDecl { get; } = true;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            if (TemplateParams is not null)
                yield return TemplateParams;
            foreach (var attrib in Attribs)
                yield return attrib;
            yield return ReturnType;
            yield return TokenName;
            foreach (var param in Params)
                yield return param;
            if (Body is not null)
                yield return Body;
            if (TokenSemiColon is not null)
                yield return TokenSemiColon;
        }
    }
}

public sealed class SyntaxDeclBinding(SyntaxNode bindingType, SyntaxToken tokenName, SyntaxToken? tokenAssign, SyntaxNode? initializer, SyntaxToken? tokenSemiColon)
    : SyntaxNode(tokenName.Location)
{
    public required SyntaxTemplateParams? TemplateParams { get; init; }
    public required IReadOnlyList<SyntaxAttrib> Attribs { get; init; }

    public SyntaxNode BindingType { get; } = bindingType;
    public SyntaxToken TokenName { get; } = tokenName;
    public SyntaxToken? TokenAssign { get; } = tokenAssign;
    public SyntaxNode? Initializer { get; } = initializer;
    public SyntaxToken? TokenSemiColon { get; } = tokenSemiColon;

    public override bool IsDecl { get; } = true;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            if (TemplateParams is not null)
                yield return TemplateParams;
            foreach (var attrib in Attribs)
                yield return attrib;
            yield return BindingType;
            yield return TokenName;
            if (TokenAssign is not null)
                yield return TokenAssign;
            if (Initializer is not null)
                yield return Initializer;
            if (TokenSemiColon is not null)
                yield return TokenSemiColon;
        }
    }
}

public sealed class SyntaxDeclParam(SyntaxNode paramType, SyntaxToken tokenName)
    : SyntaxNode(tokenName.Location)
{
    public SyntaxNode ParamType { get; } = paramType;
    public SyntaxToken TokenName { get; } = tokenName;

    public override bool IsDecl { get; } = true;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return ParamType;
            yield return TokenName;
        }
    }
}

public sealed class SyntaxDeclField(SyntaxNode fieldType, SyntaxToken tokenName)
    : SyntaxNode(tokenName.Location)
{
    public SyntaxNode FieldType { get; } = fieldType;
    public SyntaxToken TokenName { get; } = tokenName;

    public override bool IsDecl { get; } = true;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return FieldType;
            yield return TokenName;
        }
    }
}

public sealed class SyntaxDeclStruct(SyntaxToken tokenStructOrVariant, SyntaxToken tokenName, IReadOnlyList<SyntaxDeclField> fields, IReadOnlyList<SyntaxDeclStruct> variants)
    : SyntaxNode(tokenName.Location)
{
    public required SyntaxTemplateParams? TemplateParams { get; init; }
    public required IReadOnlyList<SyntaxAttrib> Attribs { get; init; }
    
    public SyntaxToken TokenStructOrVariant { get; } = tokenStructOrVariant;
    public SyntaxToken TokenName { get; } = tokenName;
    public IReadOnlyList<SyntaxDeclField> Fields { get; } = fields;
    public IReadOnlyList<SyntaxDeclStruct> Variants { get; } = variants;

    public bool IsVariant => TokenStructOrVariant.Kind == TokenKind.Variant;
    public bool IsVoidVariant => IsVariant && TokenName.Kind == TokenKind.Void;
    public override bool IsDecl { get; } = true;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            if (TemplateParams is not null)
                yield return TemplateParams;
            foreach (var attrib in Attribs)
                yield return attrib;
            yield return TokenStructOrVariant;
            yield return TokenName;
            foreach (var @field in Fields)
                yield return @field;
            foreach (var variant in Variants)
                yield return variant;
        }
    }
}

public sealed class SyntaxDeclEnumVariant(SyntaxToken tokenName, SyntaxNode? value)
    : SyntaxNode(tokenName.Location)
{
    public SyntaxToken TokenName { get; } = tokenName;
    public SyntaxNode? Value { get; } = value;

    public override bool IsDecl { get; } = true;
    public override IEnumerable<SyntaxNode> Children { get; } = value is not null ? [tokenName, value] : [tokenName];
}

public sealed class SyntaxDeclEnum(SyntaxToken tokenEnum, SyntaxToken tokenName, IReadOnlyList<SyntaxDeclEnumVariant> variants)
    : SyntaxNode(tokenName.Location)
{
    public required SyntaxTemplateParams? TemplateParams { get; init; }
    public required IReadOnlyList<SyntaxAttrib> Attribs { get; init; }

    public SyntaxToken TokenEnum { get; } = tokenEnum;
    public SyntaxToken TokenName { get; } = tokenName;
    public IReadOnlyList<SyntaxDeclEnumVariant> Variants { get; } = variants;

    public override bool IsDecl { get; } = true;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            if (TemplateParams is not null)
                yield return TemplateParams;
            foreach (var attrib in Attribs)
                yield return attrib;
            yield return TokenEnum;
            yield return TokenName;
            foreach (var variant in Variants)
                yield return variant;
        }
    }
}

public sealed class SyntaxDeclAlias(SyntaxToken? tokenStrict, SyntaxToken tokenAlias, SyntaxToken tokenName, SyntaxNode type)
    : SyntaxNode(tokenName.Location)
{
    public required SyntaxTemplateParams? TemplateParams { get; init; }
    public required IReadOnlyList<SyntaxAttrib> Attribs { get; init; }

    public SyntaxToken? TokenStrict { get; } = tokenStrict;
    public SyntaxToken TokenAlias { get; } = tokenAlias;
    public SyntaxToken TokenName { get; } = tokenName;
    public SyntaxNode Type { get; } = type;

    public bool IsStrict { get; } = tokenStrict is not null;
    public override bool IsDecl { get; } = true;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            if (TemplateParams is not null)
                yield return TemplateParams;
            foreach (var attrib in Attribs)
                yield return attrib;
            if (TokenStrict is not null)
                yield return TokenStrict;
            yield return TokenAlias;
            yield return TokenName;
            yield return Type;
        }
    }
}
