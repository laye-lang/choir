namespace Choir.Front.Laye.Syntax;

public enum ImportKind
{
    Invalid,
    FilePath,
    Library,
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

public sealed class SyntaxDeclImport(SyntaxToken tokenImport) : SyntaxNode(tokenImport.Location)
{
    public required ImportKind ImportKind { get; init; }
    public SyntaxToken TokenImport { get; } = tokenImport;
    public required IReadOnlyList<SyntaxImportQuery> Queries { get; init; }
    public SyntaxToken? TokenFrom { get; init; }
    public required SyntaxToken TokenModuleName { get; init; }
    public SyntaxToken? TokenAs { get; init; }
    public SyntaxToken? TokenAlias { get; init; }
    public required SyntaxToken TokenSemiColon { get; init; }

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
            yield return TokenSemiColon;
        }
    }
}

public sealed class SyntaxDeclFunction(SyntaxNode returnType, SyntaxToken tokenName, IReadOnlyList<SyntaxNode> parameters)
    : SyntaxNode(tokenName.Location)
{
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

public sealed class SyntaxDeclBinding(SyntaxNode bindingType, SyntaxToken tokenName, SyntaxToken? tokenAssign, SyntaxNode? initializer, SyntaxToken tokenSemiColon)
    : SyntaxNode(tokenName.Location)
{
    public SyntaxNode BindingType { get; } = bindingType;
    public SyntaxToken TokenName { get; } = tokenName;
    public SyntaxToken? TokenAssign { get; } = tokenAssign;
    public SyntaxNode? Initializer { get; } = initializer;
    public SyntaxToken TokenSemiColon { get; } = tokenSemiColon;

    public override bool IsDecl { get; } = true;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return BindingType;
            yield return TokenName;
            if (TokenAssign is not null)
                yield return TokenAssign;
            if (Initializer is not null)
                yield return Initializer;
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
