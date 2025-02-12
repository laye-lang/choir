using Choir.Front.Laye.Sema;

namespace Choir.Front.Laye.Syntax;

public enum NamerefKind
{
    Default,
    Global,
    Implicit,
}

public sealed class SyntaxExprEmpty(Location location)
    : SyntaxNode(location)
{
}

public sealed class SyntaxTemplateArguments(IReadOnlyList<SyntaxNode> templateArguments)
    : SyntaxNode(templateArguments.Count == 0 ? Location.Nowhere : templateArguments[templateArguments.Count - 1].Location)
{
    public IReadOnlyList<SyntaxNode> TemplateArguments { get; init; } = templateArguments;

    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            foreach (var arg in TemplateArguments)
                yield return arg;
        }
    }
}

// foo
// foo::bar
// ::foo
// global::foo
public sealed class SyntaxNameref : SyntaxNode
{
    public static SyntaxNameref Create(ChoirContext context, SyntaxNode name) =>
        new(context, name.Location, NamerefKind.Default, [name], null);
        
    public static SyntaxNameref Create(ChoirContext context, SyntaxNode name, SyntaxTemplateArguments? templateArguments) =>
        new(context, name.Location, NamerefKind.Default, [name], templateArguments);
        
    public static SyntaxNameref Create(ChoirContext context, Location location, NamerefKind kind, IReadOnlyList<SyntaxNode> names) =>
        new(context, location, kind, names, null);
        
    public static SyntaxNameref Create(ChoirContext context, Location location, NamerefKind kind, IReadOnlyList<SyntaxNode> names, SyntaxTemplateArguments? templateArguments) =>
        new(context, location, kind, names, templateArguments);

    public NamerefKind NamerefKind { get; }
    /// <summary>
    /// All names except for the last must be token identifiers.
    /// The last name may be an instance of type derived from <see cref="SyntaxOperatorName"/> or a token identifier.
    /// </summary>
    public IReadOnlyList<SyntaxNode> Names { get; }
    public SyntaxTemplateArguments? TemplateArguments { get; }

    public override bool CanBeType { get; } = true;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            foreach (var name in Names)
                yield return name;
            if (TemplateArguments is not null)
                yield return TemplateArguments;
        }
    }

    private SyntaxNameref(ChoirContext context, Location location, NamerefKind kind, IReadOnlyList<SyntaxNode> names, SyntaxTemplateArguments? templateArguments)
        : base(location)
    {
        context.Assert(names.Count != 0, location, "nameref must contain at least one name");
        context.Assert(names.Take(names.Count - 1).All(n => n is SyntaxToken token && token.Kind == TokenKind.Identifier), location, "all except for the last name in a nameref *must* be token identifiers");
        context.Assert((names[names.Count - 1] is SyntaxToken token && token.Kind == TokenKind.Identifier) || names[names.Count - 1] is SyntaxOperatorName, location, $"the last name in a nameref *must* be a token identifier or an instance of {nameof(SyntaxOperatorName)}. found a {names[names.Count - 1].GetType().Name}.");
        NamerefKind = kind;
        Names = names;
        TemplateArguments = templateArguments;
    }
}

public sealed class SyntaxGrouped(SyntaxNode inner)
    : SyntaxNode(inner.Location)
{
    public SyntaxNode Inner { get; } = inner;
    public override bool CanBeType => Inner.CanBeType;
    public override IEnumerable<SyntaxNode> Children { get; } = [inner];
}

public sealed class SyntaxExprCall(SyntaxNode callee, IReadOnlyList<SyntaxNode> args)
    : SyntaxNode(callee.Location)
{
    public SyntaxNode Callee { get; } = callee;
    public IReadOnlyList<SyntaxNode> Args { get; } = args;
    public override IEnumerable<SyntaxNode> Children { get; } = [callee, ..args];
}

public sealed class SyntaxExprBinary(SyntaxNode lhs, SyntaxNode rhs, SyntaxToken tokenOperator)
    : SyntaxNode(tokenOperator.Location)
{
    public SyntaxNode Left { get; } = lhs;
    public SyntaxNode Right { get; } = rhs;
    public SyntaxToken TokenOperator { get; } = tokenOperator;

    public override bool CanBeType => TokenOperator.Kind == TokenKind.DotDot && Right is SyntaxExprEmpty && Left.CanBeType;
    public override IEnumerable<SyntaxNode> Children { get; } = [lhs, tokenOperator, rhs];
}

public sealed class SyntaxExprUnaryPrefix(SyntaxToken tokenOperator, SyntaxNode operand)
    : SyntaxNode(tokenOperator.Location)
{
    public SyntaxToken TokenOperator { get; } = tokenOperator;
    public SyntaxNode Operand { get; } = operand;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenOperator, operand];
}

public sealed class SyntaxExprUnaryPostfix(SyntaxNode operand, SyntaxToken tokenOperator)
    : SyntaxNode(tokenOperator.Location)
{
    public SyntaxNode Operand { get; } = operand;
    public SyntaxToken TokenOperator { get; } = tokenOperator;
    public override IEnumerable<SyntaxNode> Children { get; } = [operand, tokenOperator];
}

public sealed class SyntaxExprCast(SyntaxToken tokenCast, SyntaxNode? targetType, SyntaxNode expr)
    : SyntaxNode(tokenCast.Location)
{
    public SyntaxToken TokenCast { get; } = tokenCast;
    public SyntaxNode? TargetType { get; } = targetType;
    public SyntaxNode Expr { get; } = expr;

    public bool IsAutoCast => TargetType is null;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return TokenCast;
            if (TargetType is not null)
                yield return TargetType;
            yield return Expr;
        }
    }
}

public abstract class SyntaxDesignator(Location location)
    : SyntaxNode(location)
{
}

public sealed class SyntaxDesignatorField(SyntaxToken fieldToken)
    : SyntaxDesignator(fieldToken.Location)
{
    public string FieldName { get; } = fieldToken.TextValue;
    public override IEnumerable<SyntaxNode> Children { get; } = [fieldToken];
}

public sealed class SyntaxDesignatorIndex(SyntaxNode indexExpr)
    : SyntaxDesignator(indexExpr.Location)
{
    public SyntaxNode IndexExpr { get; } = indexExpr;
    public override IEnumerable<SyntaxNode> Children { get; } = [indexExpr];
}

public sealed class SyntaxDesignatorInvalid(SyntaxNode node)
    : SyntaxDesignator(node.Location)
{
    public SyntaxNode Node { get; } = node;
    public override IEnumerable<SyntaxNode> Children { get; } = [node];
}

public class SyntaxConstructorInit(Location location, SyntaxNode value)
    : SyntaxNode(location)
{
    public SyntaxNode Value { get; } = value;
    public override IEnumerable<SyntaxNode> Children { get; } = [value];
}

public sealed class SyntaxConstructorInitDesignated(Location location, IReadOnlyList<SyntaxDesignator> designators, SyntaxNode value)
    : SyntaxConstructorInit(location, value)
{
    public IReadOnlyList<SyntaxDesignator> Designators { get; } = designators;
    public override IEnumerable<SyntaxNode> Children { get; } = [..designators, value];
}

public sealed class SyntaxExprConstructor(SyntaxNode type, IReadOnlyList<SyntaxConstructorInit> inits)
    : SyntaxNode(type.Location)
{
    public SyntaxNode Type { get; } = type;
    public IReadOnlyList<SyntaxConstructorInit> Inits { get; } = inits;
    public override IEnumerable<SyntaxNode> Children { get; } = [type, ..inits];
}

public sealed class SyntaxExprNew(SyntaxToken tokenNew, IReadOnlyList<SyntaxNode> @params, SyntaxNode type, IReadOnlyList<SyntaxConstructorInit> inits)
    : SyntaxNode(tokenNew.Location)
{
    public SyntaxToken TokenNew { get; } = tokenNew;
    public IReadOnlyList<SyntaxNode> Params { get; } = @params;
    public SyntaxNode Type { get; } = type;
    public IReadOnlyList<SyntaxConstructorInit> Inits { get; } = inits;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenNew, .. @params, type, ..inits];
}

public sealed class SyntaxExprField(SyntaxNode operand, SyntaxToken tokenFieldName)
    : SyntaxNode(tokenFieldName.Location)
{
    public SyntaxNode Operand { get; } = operand;
    public SyntaxToken TokenFieldName { get; } = tokenFieldName;
    public string FieldNameText => TokenFieldName.TextValue;
    public override IEnumerable<SyntaxNode> Children { get; } = [operand, tokenFieldName];
}

public sealed class SyntaxIndex(SyntaxNode operand, SyntaxToken tokenOpenBracket, IReadOnlyList<SyntaxNode> indices, SyntaxToken tokenCloseBracket)
    : SyntaxNode(operand.Location)
{
    public SyntaxToken TokenOpenBracket { get; } = tokenOpenBracket;
    public SyntaxNode Operand { get; } = operand;
    public IReadOnlyList<SyntaxNode> Indices { get; } = indices;
    public SyntaxToken TokenCloseBracket { get; } = tokenCloseBracket;

    public override bool CanBeType => Operand.CanBeType;
    public override IEnumerable<SyntaxNode> Children
    {
        get
        {
            yield return Operand;
            yield return TokenOpenBracket;
            foreach (var index in Indices)
                yield return index;
            yield return TokenCloseBracket;
        }
    }
}

#if LAYE_DECONSTRUCTOR_PATTERN_ENABLED
public sealed class SyntaxPatternStructDeconstruction(SyntaxNode? type, SyntaxToken tokenOpenBrace, IReadOnlyList<SyntaxNode> children, SyntaxToken tokenCloseBrace)
    : SyntaxNode(tokenOpenBrace.Location)
{
    public SyntaxNode? Type { get; } = type;
    public IReadOnlyList<SyntaxNode> ChildPatterns { get; } = children;
    public override IEnumerable<SyntaxNode> Children { get; } = type is not null ? [type, tokenOpenBrace, .. children, tokenCloseBrace] : [tokenOpenBrace, .. children, tokenCloseBrace];
}
#else
public sealed class SyntaxPatternStructured(SyntaxNode? type, SyntaxToken tokenOpenBrace, IReadOnlyList<SyntaxNode> children, SyntaxToken tokenCloseBrace)
    : SyntaxNode(tokenOpenBrace.Location)
{
    public SyntaxNode? Type { get; } = type;
    public IReadOnlyList<SyntaxNode> ChildPatterns { get; } = children;
    public override IEnumerable<SyntaxNode> Children { get; } = type is not null ? [type, tokenOpenBrace, .. children, tokenCloseBrace] : [tokenOpenBrace, .. children, tokenCloseBrace];
}
#endif

public sealed class SyntaxExprPatternMatch(SyntaxNode expr, SyntaxToken tokenIs, SyntaxNode pattern)
    : SyntaxNode(tokenIs.Location)
{
    public SyntaxNode Expr { get; } = expr;
    public SyntaxToken TokenIs { get; } = tokenIs;
    public SyntaxNode Pattern { get; } = pattern;

    public override IEnumerable<SyntaxNode> Children { get; } = [expr, tokenIs, pattern];
}

public sealed class SyntaxExprSizeof(SyntaxToken tokenSizeof, SyntaxNode operand)
    : SyntaxNode(tokenSizeof.Location)
{
    public SyntaxToken TokenSizeof { get; } = tokenSizeof;
    public SyntaxNode Operand { get; } = operand;
    
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenSizeof, operand];
}

public sealed class SyntaxExprCountof(SyntaxToken tokenCountof, SyntaxNode operand)
    : SyntaxNode(tokenCountof.Location)
{
    public SyntaxToken TokenCountof { get; } = tokenCountof;
    public SyntaxNode Operand { get; } = operand;

    public override IEnumerable<SyntaxNode> Children { get; } = [tokenCountof, operand];
}

public sealed class SyntaxExprRankof(SyntaxToken tokenRankof, SyntaxNode operand)
    : SyntaxNode(tokenRankof.Location)
{
    public SyntaxToken TokenRankof { get; } = tokenRankof;
    public SyntaxNode Operand { get; } = operand;

    public override IEnumerable<SyntaxNode> Children { get; } = [tokenRankof, operand];
}

public sealed class SyntaxExprAlignof(SyntaxToken tokenAlignof, SyntaxNode operand)
    : SyntaxNode(tokenAlignof.Location)
{
    public SyntaxToken TokenAlignof { get; } = tokenAlignof;
    public SyntaxNode Operand { get; } = operand;
    
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenAlignof, operand];
}

public sealed class SyntaxExprOffsetof(SyntaxToken tokenOffsetof, SyntaxNode operand)
    : SyntaxNode(tokenOffsetof.Location)
{
    public SyntaxToken TokenOffsetof { get; } = tokenOffsetof;
    public SyntaxNode Operand { get; } = operand;

    public override IEnumerable<SyntaxNode> Children { get; } = [tokenOffsetof, operand];
}

public sealed class SyntaxExprLambda(IReadOnlyList<SyntaxDeclParam> @params, SyntaxToken tokenArrow, SyntaxNode body)
    : SyntaxNode(tokenArrow.Location)
{
    public IReadOnlyList<SyntaxDeclParam> Params { get; } = @params;
    public SyntaxToken TokenArrow { get; } = tokenArrow;
    public SyntaxNode Body { get; } = body;

    public override IEnumerable<SyntaxNode> Children { get; } = [.. @params, tokenArrow, body];
}

public sealed class SyntaxExprRef(SyntaxToken tokenRef, SyntaxNode operand)
    : SyntaxNode(tokenRef.Location)
{
    public SyntaxToken TokenRef { get; } = tokenRef;
    public SyntaxNode Operand { get; } = operand;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenRef, operand];
}

public sealed class SyntaxQualMut(SyntaxNode inner, SyntaxToken tokenMut)
    : SyntaxNode(inner.Location)
{
    public SyntaxNode Inner { get; } = inner;
    public SyntaxToken TokenMut { get; } = tokenMut;

    public override bool CanBeType { get; } = true;
    public override IEnumerable<SyntaxNode> Children { get; } = [inner, tokenMut];
}

public sealed class SyntaxTypeBuiltIn(Location location, SemaTypeBuiltIn type)
    : SyntaxNode(location)
{
    public SemaTypeBuiltIn Type { get; } = type;

    public override bool CanBeType { get; } = true;
}

public sealed class SyntaxTypePointer(SyntaxNode inner)
    : SyntaxNode(inner.Location)
{
    public SyntaxNode Inner { get; } = inner;

    public override bool CanBeType { get; } = true;
    public override IEnumerable<SyntaxNode> Children { get; } = [inner];
}

public sealed class SyntaxTypeBuffer(SyntaxNode inner, SyntaxNode? terminatorExpr)
    : SyntaxNode(inner.Location)
{
    public SyntaxNode Inner { get; } = inner;
    public SyntaxNode? TerminatorExpr { get; } = terminatorExpr;
    
    public override bool CanBeType { get; } = true;
    public override IEnumerable<SyntaxNode> Children { get; } = terminatorExpr is not null ? [inner, terminatorExpr] : [inner];
}

public sealed class SyntaxTypeSlice(SyntaxNode inner)
    : SyntaxNode(inner.Location)
{
    public SyntaxNode Inner { get; } = inner;
    
    public override bool CanBeType { get; } = true;
    public override IEnumerable<SyntaxNode> Children { get; } = [inner];
}

public sealed class SyntaxTypeNilable(SyntaxNode inner)
    : SyntaxNode(inner.Location)
{
    public SyntaxNode Inner { get; } = inner;
    
    public override bool CanBeType { get; } = true;
    public override IEnumerable<SyntaxNode> Children { get; } = [inner];
}

public sealed class SyntaxTypeRange(SyntaxNode inner)
    : SyntaxNode(inner.Location)
{
    public SyntaxNode Inner { get; } = inner;

    public override bool CanBeType { get; } = true;
    public override IEnumerable<SyntaxNode> Children { get; } = [inner];
}

public sealed class SyntaxTypeof(SyntaxToken tokenTypeof, SyntaxNode operand)
    : SyntaxNode(tokenTypeof.Location)
{
    public SyntaxToken TokenTypeof { get; } = tokenTypeof;
    public SyntaxNode Operand { get; } = operand;

    public override bool CanBeType { get; } = true;
    public override IEnumerable<SyntaxNode> Children { get; } = [tokenTypeof, operand];
}
