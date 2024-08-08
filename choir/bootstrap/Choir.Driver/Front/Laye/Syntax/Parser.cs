using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ClangSharp;

namespace Choir.Front.Laye.Syntax;

public partial class Parser(Module module)
{
    private enum ExprParseContext
    {
        Default,
        CheckForDeclarations,
    }

    public static void ParseSyntax(Module module)
    {
        if (module.TopLevelSyntax.Any())
            throw new InvalidOperationException("Can't repeatedly parse syntax into a module which already had syntax read into it.");
        
        var parser = new Parser(module);

        while (parser.ParseTopLevelSyntax() is {} topLevelNode)
            module.AddTopLevelSyntax(topLevelNode);
    }

    private static bool IsImportDeclForCHeader(SyntaxDeclImport importDecl)
    {
        return importDecl. ImportKind == ImportKind.FilePath && importDecl.ModuleNameText.EndsWith(".h", StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool IsDefinitelyTypeStart(TokenKind kind) => kind switch
    {
        TokenKind.Mut or TokenKind.Var or
        TokenKind.Void or TokenKind.NoReturn or
        TokenKind.Bool or TokenKind.BoolSized or
        TokenKind.Int or TokenKind.IntSized or
        TokenKind.FloatSized or TokenKind.BuiltinFFIBool or
        TokenKind.BuiltinFFIChar or TokenKind.BuiltinFFIShort or
        TokenKind.BuiltinFFIInt or TokenKind.BuiltinFFILong or
        TokenKind.BuiltinFFILongLong or TokenKind.BuiltinFFIFloat or
        TokenKind.BuiltinFFIDouble or TokenKind.BuiltinFFILongDouble => true,
        _ => false,
    };

    private static bool IsDefinitelyExprStart(TokenKind kind) => kind switch
    {
        TokenKind.LiteralFloat or TokenKind.LiteralInteger or
        TokenKind.LiteralRune or TokenKind.LiteralString => true,
        _ => false,
    };

    public Module Module { get; } = module;
    public SourceFile SourceFile { get; } = module.SourceFile;
    public ChoirContext Context { get; } = module.Context;

    private readonly SyntaxToken[] _tokens = module.Tokens.ToArray();

    private int _position = 0;
    private bool _hasOnlyReadImports = true;

    private readonly List<SyntaxDeclImport> _cHeaderImports = [];
    
    private bool IsAtEnd => _position >= _tokens.Length - 1;
    private SyntaxToken EndOfFileToken => _tokens[_tokens.Length - 1];
    private SyntaxToken CurrentToken => Peek(0);
    private Location CurrentLocation => CurrentToken.Location;

    private SyntaxToken Peek(int ahead)
    {
        Debug.Assert(ahead >= 0);
        
        int peekPosition = _position + ahead;
        if (peekPosition >= _tokens.Length - 1) // NOTE(local): `- 1` because EOF token
            return EndOfFileToken;
        
        return _tokens[peekPosition];
    }

    private bool At(params TokenKind[] kinds) => PeekAt(0, kinds);
    private bool At(params string[] contextualKeywordTexts) => PeekAt(0, contextualKeywordTexts);

    private bool PeekAt(int ahead, params TokenKind[] kinds)
    {
        foreach (var kind in kinds)
        {
            if (Peek(ahead).Kind == kind)
                return true;
        }

        return false;
    }

    private bool PeekAt(int ahead, params string[] contextualKeywordTexts)
    {
        foreach (string text in contextualKeywordTexts)
        {
            if (Peek(ahead).Kind == TokenKind.Identifier && Peek(ahead).TextValue == text)
                return true;
        }

        return false;
    }

    private void Advance() => Advance(out _);
    private void Advance(out SyntaxToken token)
    {
        token = CurrentToken;
        _position++;
    }

    private bool TryAdvance(TokenKind kind) => TryAdvance(kind, out _);
    private bool TryAdvance(TokenKind kind, [MaybeNullWhen(false)] out SyntaxToken token)
    {
        if (CurrentToken.Kind != kind)
        {
            token = null;
            return false;
        }

        token = CurrentToken;
        Advance();

        return true;
    }

    private bool TryAdvance(string keywordText, TokenKind contextualKind, [MaybeNullWhen(false)] out SyntaxToken token)
    {
        if (CurrentToken.Kind == contextualKind || (CurrentToken.Kind == TokenKind.Identifier && MemoryExtensions.SequenceEqual(CurrentToken.Location.Span(Context), keywordText)))
        {
            token = CurrentToken;
            token.Kind = contextualKind;
            Advance();
            return true;
        }

        token = null;
        return false;
    }

    private bool Consume(TokenKind kind)
    {
        if (CurrentToken.Kind != kind)
            return false;

        Advance();
        return true;
    }

    private bool Consume(TokenKind kind, out SyntaxToken token)
    {
        if (CurrentToken.Kind != kind)
        {
            token = new SyntaxToken(TokenKind.Missing, new Location(CurrentLocation.Offset, 0, SourceFile.FileId));
            return false;
        }

        token = CurrentToken;
        Advance();
        
        return true;
    }

    private bool Consume(TokenKind[] kinds, out SyntaxToken token)
    {
        foreach (var kind in kinds)
        {
            if (CurrentToken.Kind == kind)
            {
                token = CurrentToken;
                Advance();
                
                return true;
            }
        }
        
        token = new SyntaxToken(TokenKind.Missing, new Location(CurrentLocation.Offset, 0, SourceFile.FileId));
        return false;
    }

    private bool Consume(string keywordText, TokenKind contextualKind)
    {
        if (CurrentToken.Kind == TokenKind.Identifier && MemoryExtensions.SequenceEqual(CurrentToken.Location.Span(Context), keywordText))
        {
            CurrentToken.Kind = contextualKind;
            Advance();
            return true;
        }

        return false;
    }

    private bool Consume(string keywordText, TokenKind contextualKind, out SyntaxToken token)
    {
        if (CurrentToken.Kind == TokenKind.Identifier && MemoryExtensions.SequenceEqual(CurrentToken.Location.Span(Context), keywordText))
        {
            token = CurrentToken;
            token.Kind = contextualKind;
            Advance();
            return true;
        }

        token = new SyntaxToken(TokenKind.Missing, new Location(CurrentLocation.Offset, 0, SourceFile.FileId));
        return false;
    }

    private void Expect(TokenKind kind, string expected)
    {
        if (!Consume(kind))
            Context.Diag.Error(CurrentLocation, $"expected {expected}");
    }

    private void Expect(TokenKind kind, string expected, out SyntaxToken token)
    {
        if (!Consume(kind, out token))
            Context.Diag.Error(CurrentLocation, $"expected {expected}");
    }

    private void ExpectSemiColon() => Expect(TokenKind.SemiColon, "';'");
    private void ExpectSemiColon(out SyntaxToken token) => Expect(TokenKind.SemiColon, "';'", out token);
    private void ExpectIdentifier(out SyntaxToken token) => Expect(TokenKind.Identifier, "an identifier", out token);

    private void ExpectContextualKeyword(string keywordText, TokenKind contextualKind)
    {
        if (!Consume(keywordText, contextualKind))
            Context.Diag.Error(CurrentToken.Location, $"expected '{keywordText}'");
    }

    private void ExpectContextualKeyword(string keywordText, TokenKind contextualKind, out SyntaxToken token)
    {
        if (!Consume(keywordText, contextualKind, out token))
            Context.Diag.Error(CurrentToken.Location, $"expected '{keywordText}'");
    }

    private IReadOnlyList<T> ParseDelimited<T>(Func<T> parser, TokenKind tokenDelimiter, string expected, params TokenKind[] closers)
        where T : SyntaxNode
    {
        var results = new List<T>();
        do
        {
            if (At(closers))
            {
                Context.Diag.Error($"expected {expected}");
                break;
            }

            results.Add(parser());
        } while (TryAdvance(tokenDelimiter));
        return [.. results];
    }

    public SyntaxNode? ParseTopLevelSyntax()
    {
        if (_hasOnlyReadImports)
        {
            if (CurrentToken.Kind == TokenKind.Import)
            {
                var importDecl = ParseImportDeclaration();
                if (IsImportDeclForCHeader(importDecl))
                    _cHeaderImports.Add(importDecl);

                return importDecl;
            }
            else
            {
                _hasOnlyReadImports = false;
                ParseImportedCHeaders();
            }
        }
        
        if (IsAtEnd) return null;

        switch (CurrentToken.Kind)
        {
            case TokenKind.Import:
            {
                var importDecl = ParseImportDeclaration();
                if (IsImportDeclForCHeader(importDecl))
                {
                    Debug.Assert(!_hasOnlyReadImports);
                    Context.Diag.Error("import declarations referencing C header files must appear only at the top of the source file");
                }
                
                return importDecl;
            }

            default:
            {
                var declType = ParseType();
                return ParseBindingOrFunctionDeclStartingAtName(declType);
            }
        }
    }

    private IReadOnlyList<SyntaxImportQuery> ParseImportQueries() => ParseDelimited(ParseImportQuery, TokenKind.Comma, "an identifier or '*'", TokenKind.SemiColon, TokenKind.OpenBrace, TokenKind.CloseBrace);
    private SyntaxImportQuery ParseImportQuery()
    {
        if (TryAdvance(TokenKind.Star, out var tokenStar))
            return new SyntaxImportQueryWildcard(tokenStar);
        else
        {
            if (At("as")) CurrentToken.Kind = TokenKind.As;
            var queryNameref = ParseNameref();

            SyntaxToken? tokenAlias = null;
            if (TryAdvance("as", TokenKind.As, out SyntaxToken? tokenAs))
                ExpectIdentifier(out tokenAlias);

            return new SyntaxImportQueryNamed(queryNameref, tokenAs, tokenAlias);
        }
    }

    public SyntaxDeclImport ParseImportDeclaration()
    {
        Debug.Assert(CurrentToken.Kind == TokenKind.Import);
        Advance(out var tokenImport);

        SyntaxToken? tokenAs;
        SyntaxToken? tokenAlias = null;
        SyntaxToken tokenSemiColon;

        bool isQueryless = At(TokenKind.LiteralString) ||
            (At(TokenKind.Identifier) && PeekAt(1, TokenKind.SemiColon)) ||
            (At(TokenKind.Identifier) && PeekAt(1, "as") && PeekAt(2, TokenKind.Identifier) && PeekAt(3, TokenKind.SemiColon));

        if (isQueryless)
        {
            var tokenModuleName = CurrentToken;
            Advance();

            if (TryAdvance("as", TokenKind.As, out tokenAs))
                ExpectIdentifier(out tokenAlias);

            ExpectSemiColon(out tokenSemiColon);
            return new SyntaxDeclImport(tokenImport)
            {
                ImportKind = tokenModuleName.Kind == TokenKind.LiteralString ? ImportKind.FilePath : ImportKind.Library,
                Queries = [],
                TokenFrom = null,
                TokenModuleName = tokenModuleName,
                TokenSemiColon = tokenSemiColon,
                TokenAs = tokenAs,
                TokenAlias = tokenAlias,
            };
        }

        IReadOnlyList<SyntaxImportQuery> queries = [];
        if (!At("from"))
            queries = ParseImportQueries();
        else Context.Diag.Error(CurrentLocation, "expected an identifier");
        ExpectContextualKeyword("from", TokenKind.From, out var tokenFrom);
        
        if (!Consume([TokenKind.Identifier, TokenKind.LiteralString], out var tokenPath))
            Context.Diag.Error(tokenPath.Location, "expected an identifier or string literal");

        if (TryAdvance("as", TokenKind.As, out tokenAs))
            ExpectIdentifier(out tokenAlias);

        ExpectSemiColon(out tokenSemiColon);
        return new SyntaxDeclImport(tokenImport)
        {
            ImportKind = tokenPath.Kind == TokenKind.LiteralString ? ImportKind.FilePath
                       : tokenPath.Kind == TokenKind.Identifier ? ImportKind.Library : ImportKind.Invalid,
            Queries = queries,
            TokenFrom = tokenFrom,
            TokenModuleName = tokenPath,
            TokenSemiColon = tokenSemiColon,
            TokenAs = tokenAs,
            TokenAlias = tokenAlias,
        };
    }

    private SyntaxNode ParseBindingOrFunctionDeclStartingAtName(SyntaxNode declType)
    {
        if (PeekAt(1, TokenKind.OpenParen))
            return ParseFunctionDeclStartingAtName(declType);
            
        return ParseBindingDeclStartingAtName(declType);
    }

    private SyntaxDeclBinding ParseBindingDeclStartingAtName(SyntaxNode bindingType)
    {
        ExpectIdentifier(out var tokenName);
        ExpectSemiColon(out var tokenSemiColon);
        return new SyntaxDeclBinding(bindingType, tokenName, tokenSemiColon);
    }

    private SyntaxNode ParseFunctionParameter()
    {
        var paramType = ParseType();
        ExpectIdentifier(out var tokenName);
        return new SyntaxDeclParam(paramType, tokenName);
    }

    private SyntaxDeclFunction ParseFunctionDeclStartingAtName(SyntaxNode returnType)
    {
        ExpectIdentifier(out var tokenName);
        Expect(TokenKind.OpenParen, "'('");
        var parameters = ParseDelimited(ParseFunctionParameter, TokenKind.Comma, "type", TokenKind.CloseParen, TokenKind.SemiColon);
        Expect(TokenKind.CloseParen, "')'");

        if (At(TokenKind.OpenBrace))
        {
            var body = ParseStmtCompound();
            return new SyntaxDeclFunction(returnType, tokenName, parameters)
            {
                Body = body,
            };
        }

        ExpectSemiColon(out var tokenSemiColon);
        return new SyntaxDeclFunction(returnType, tokenName, parameters)
        {
            TokenSemiColon = tokenSemiColon,
        };
    }

    private SyntaxNode ParseSyntaxInStmtContext()
    {
        // if it's *definitely* a type, we *definitely* want to return a binding/function declaration
        if (IsDefinitelyTypeStart(CurrentToken.Kind))
        {
            var declType = ParseType();
            return ParseBindingOrFunctionDeclStartingAtName(declType);
        }

        var currentToken = CurrentToken;
        switch (CurrentToken.Kind)
        {
            default:
            {
                var syntax = ParseExpr(ExprParseContext.CheckForDeclarations);
                if (syntax.IsDecl) return syntax;

                if (syntax.CanBeType && At(TokenKind.Identifier))
                    return ParseBindingOrFunctionDeclStartingAtName(syntax);

                Context.Diag.Note(syntax.Location, syntax.GetType().Name);

                Context.Diag.ICE("we have a lot of work to do in the stmt/decl/expr parser...");
                throw new UnreachableException();
            }
        }
    }

    private SyntaxStmtCompound ParseStmtCompound()
    {
        Expect(TokenKind.OpenBrace, "'{'", out var tokenOpenBrace);

        var body = new List<SyntaxNode>();
        while (!IsAtEnd && !At(TokenKind.CloseBrace))
        {
            var stmt = ParseSyntaxInStmtContext();
            body.Add(stmt);
        }
        
        Expect(TokenKind.CloseBrace, "'}'");

        return new SyntaxStmtCompound(tokenOpenBrace.Location, [.. body]);
    }

    private SyntaxNameref ParseNameref()
    {
        NamerefKind kind = NamerefKind.Default;
        if (TryAdvance(TokenKind.ColonColon))
            kind = NamerefKind.Implicit;
        else if (TryAdvance(TokenKind.Global))
        {
            Expect(TokenKind.ColonColon, "'::'");
            kind = NamerefKind.Global;
        }

        var names = ParseDelimited(
            () => { ExpectIdentifier(out var tokenName); return tokenName; },
            TokenKind.ColonColon,
            "an identifier"
        );
        Debug.Assert(names.Count > 0);

        var lastNameLocation = names[names.Count - 1].Location;

        SyntaxTemplateArguments? templateArguments = null;
        if (At(TokenKind.Less) && CurrentLocation.Offset == lastNameLocation.Offset + lastNameLocation.Length)
        {
            // What we'll want to do instead, soon, is see if we can just look for `<` and ignore
            // the spacing, then parse a single expression with the precedence already set to `<`.
            // We'll have to also explicitly stop on any operator token which starts with `>`,
            // and also be prepared to split tokens at that character / keep track of testing and consume `>>`
            // or `>>>` at once to close multiple lists.
            // Splitting tokens is not ideal, especially since it's likely to mean ill-formed code in
            // the first place, so we may keep track of nesting and only allow instances of the
            // previous 3 mentioned tokens where it makes sense.
            templateArguments = ParseTemplateArguments();
        }

        return SyntaxNameref.Create(names[names.Count - 1].Location, kind, names, templateArguments);
    }

    private SyntaxTemplateArguments ParseTemplateArguments()
    {
        Expect(TokenKind.Less, "'<'");

        var args = new SyntaxTemplateArguments(ParseDelimited(
            ParseTemplateArgument,
            TokenKind.Comma,
            "a type or expression",
            TokenKind.Greater,
            TokenKind.GreaterGreater,
            TokenKind.GreaterGreaterGreater,
            TokenKind.GreaterGreaterEqual,
            TokenKind.GreaterGreaterGreaterEqual
        ));
        
        Expect(TokenKind.Greater, "'>'");

        return args;
    }

    private SyntaxNode ParseTemplateArgument()
    {
        // for now, since we don't have the type scanner in place, just assume it's type arguments
        // unless it's obviously not, like literal values.
        // Replace this trivial check for a call to the scanner for more reliable parses.

        if (IsDefinitelyExprStart(CurrentToken.Kind))
            return ParseExpr(ExprParseContext.Default);
        
        return ParseType();
    }

    private SyntaxNode ParseTypeSuffix(SyntaxNode typeNode)
    {
        var currentToken = CurrentToken;
        
        switch (CurrentToken.Kind)
        {
            default: return typeNode;
            
            case TokenKind.Mut: break;

            case TokenKind.OpenBracket when Peek(1).Kind == TokenKind.Star && Peek(2).Kind == TokenKind.CloseBracket:
            {
                for (int i = 0; i < 3; i++) Advance();
                typeNode = new SyntaxTypeBuffer(typeNode);
            } break;
            
            case TokenKind.OpenBracket when Peek(1).Kind == TokenKind.Star && Peek(2).Kind == TokenKind.Colon:
            {
                Context.Diag.Fatal("todo: parse [*:<terminator>] type suffix");
                throw new UnreachableException();
            }
        }

        if (TryAdvance(TokenKind.Mut, out var tokenMut))
        {
            typeNode = new SyntaxQualMut(typeNode, tokenMut);
            while (TryAdvance(TokenKind.Mut, out var leadingMut)) {
                Context.Diag.Error(leadingMut.Location, "duplicate 'mut' qualifier");
            }
        }

        return ParseTypeSuffix(typeNode);
    }

    private SyntaxNode ParseType()
    {
        var currentToken = CurrentToken;

        SyntaxToken? tokenMut = null;
        if (TryAdvance(TokenKind.Mut, out tokenMut))
        {
            while (TryAdvance(TokenKind.Mut, out var leadingMut)) {
                Context.Diag.Error(leadingMut.Location, "duplicate 'mut' qualifier");
            }
        }

        SyntaxNode typeNode;
        switch (CurrentToken.Kind)
        {
            case TokenKind.OpenParen:
            {
                Advance();
                var nestedType = ParseType();
                Expect(TokenKind.CloseParen, "')'");
                return new SyntaxGrouped(nestedType);
            }

            case TokenKind.Void: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeVoid); break;
            case TokenKind.NoReturn: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeNoReturn); break;
            case TokenKind.Bool: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeBool); break;
            case TokenKind.BoolSized: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeBoolSized((int)currentToken.IntegerValue)); break;
            case TokenKind.Int: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeInt); break;
            case TokenKind.IntSized: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeIntSized((int)currentToken.IntegerValue)); break;
            case TokenKind.FloatSized: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeFloatSized((int)currentToken.IntegerValue)); break;
            case TokenKind.BuiltinFFIBool: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeFFIBool); break;
            case TokenKind.BuiltinFFIChar: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeFFIChar); break;
            case TokenKind.BuiltinFFIShort: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeFFIShort); break;
            case TokenKind.BuiltinFFIInt: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeFFIInt); break;
            case TokenKind.BuiltinFFILong: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeFFILong); break;
            case TokenKind.BuiltinFFILongLong: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeFFILongLong); break;
            case TokenKind.BuiltinFFIFloat: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeFFIFloat); break;
            case TokenKind.BuiltinFFIDouble: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeFFIDouble); break;
            case TokenKind.BuiltinFFILongDouble: Advance(); typeNode = new SyntaxTypeBuiltIn(currentToken.Location, Context.Types.LayeTypeFFILongDouble); break;

            case TokenKind.Global:
            case TokenKind.ColonColon:
            case TokenKind.Identifier:
            {
                typeNode = ParseNameref();
            } break;

            default:
            {
                Context.Diag.ICE($"need to return a default syntax node when no type was parseable (at token kind {CurrentToken.Kind})");
                throw new UnreachableException();
            }
        }

        if (tokenMut is not null || TryAdvance(TokenKind.Mut, out tokenMut))
        {
            typeNode = new SyntaxQualMut(typeNode, tokenMut);
            while (TryAdvance(TokenKind.Mut, out var leadingMut)) {
                Context.Diag.Error(leadingMut.Location, "duplicate 'mut' qualifier");
            }
        }

        return ParseTypeSuffix(typeNode);
    }
    
    private SyntaxNode ParseExpr(ExprParseContext parseContext)
    {
        var primary = ParsePrimaryExpr();
        return ParseBinaryExpr(parseContext, primary);
    }

    private SyntaxNode ParsePrimaryExprSuffix(SyntaxNode primary)
    {
        var currentToken = CurrentToken;
        switch (CurrentToken.Kind)
        {
            default: return primary;

            // mut suffix applies to any type any time
            case TokenKind.Mut when primary.CanBeType:
            // `type* mut` and `type& mut` are always pointer/reference types
            case TokenKind.Star or TokenKind.Ampersand when PeekAt(1, TokenKind.Mut) && primary.CanBeType:
            // `type[]` is always a slice type
            case TokenKind.OpenBracket when Peek(1).Kind == TokenKind.CloseBracket && primary.CanBeType:
            // `type[*]` and `type[*:` are always buffer types
            case TokenKind.OpenBracket when Peek(1).Kind == TokenKind.Star && Peek(2).Kind is TokenKind.CloseBracket or TokenKind.Colon && primary.CanBeType:
                return ParseTypeSuffix(primary);
        }
    }

    private SyntaxNode ParsePrimaryExpr()
    {
        if (IsDefinitelyTypeStart(CurrentToken.Kind))
            return ParseType();

        var currentToken = CurrentToken;
        switch (CurrentToken.Kind)
        {
            case TokenKind.Mut:
            case TokenKind.Var:
            case TokenKind.Void:
            case TokenKind.NoReturn:
            case TokenKind.Bool:
            case TokenKind.BoolSized:
            case TokenKind.Int:
            case TokenKind.IntSized:
            case TokenKind.FloatSized:
            case TokenKind.BuiltinFFIBool:
            case TokenKind.BuiltinFFIChar:
            case TokenKind.BuiltinFFIShort:
            case TokenKind.BuiltinFFIInt:
            case TokenKind.BuiltinFFILong:
            case TokenKind.BuiltinFFILongLong:
            case TokenKind.BuiltinFFIFloat:
            case TokenKind.BuiltinFFIDouble:
            case TokenKind.BuiltinFFILongDouble:
            {
                Context.Diag.ICE($"a token which definitely starts a type (kind {CurrentToken.Kind}) made it to the primary expression parser");
                throw new UnreachableException();
            }

            case TokenKind.Global:
            case TokenKind.ColonColon:
            case TokenKind.Identifier:
            {
                var nameref = ParseNameref();
                return ParsePrimaryExprSuffix(nameref);
            }

            case TokenKind.LiteralInteger:
            {
                Advance();
                return currentToken;
            }

            default:
            {
                Context.Diag.ICE($"need to return a default syntax node when no expression was parseable (at token kind {CurrentToken.Kind})");
                throw new UnreachableException();
            }
        }
    }

    private SyntaxNode ParseBinaryExpr(ExprParseContext parseContext, SyntaxNode lhs, int precedence = 0)
    {
        int nextPrecedence = 0;
        while (AtBinaryOperatorWithPrecedence(precedence))
        {
            var tokenOperator = CurrentToken;
            Advance();

            if (parseContext == ExprParseContext.CheckForDeclarations &&
                tokenOperator.Kind is TokenKind.Star or TokenKind.Ampersand && At(TokenKind.Identifier) &&
                lhs.CanBeType)
            {
                if (PeekAt(1, TokenKind.Equal, TokenKind.SemiColon))
                {
                    // this is now a binding declaration
                    SyntaxNode bindingType = CreateTypeNodeFromOperator(lhs, tokenOperator);
                    return ParseBindingDeclStartingAtName(bindingType);
                }

                if (PeekAt(1, TokenKind.OpenParen))
                {
                    // we have some work to do to determine if this is a function
                    if (PeekAt(2, TokenKind.CloseParen) && PeekAt(3, TokenKind.SemiColon, TokenKind.OpenBrace, TokenKind.EqualGreater))
                    {
                        // this is a function declaration/definition with no parameters
                        SyntaxNode returnType = CreateTypeNodeFromOperator(lhs, tokenOperator);
                        return ParseFunctionDeclStartingAtName(returnType);
                    }
                }
            }

            var rhs = ParsePrimaryExpr();

            int rhsPrecedence = nextPrecedence;
            while (AtBinaryOperatorWithPrecedence(rhsPrecedence))
                rhs = ParseBinaryExpr(parseContext, rhs, rhsPrecedence);
                
            lhs = new SyntaxExprBinary(lhs, rhs, tokenOperator);
        }

        return lhs;

        bool AtBinaryOperatorWithPrecedence(int checkPrecedence)
        {
            if (!CurrentToken.Kind.CanBeBinaryOperator())
                return false;
            
            int currentTokenPrecedence = CurrentToken.Kind.GetBinaryOperatorPrecedence();
            if (currentTokenPrecedence >= checkPrecedence)
            {
                nextPrecedence = currentTokenPrecedence;
                return true;
            }

            return false;
        }

        static SyntaxNode CreateTypeNodeFromOperator(SyntaxNode inner, SyntaxToken tokenOperator)
        {
            if (tokenOperator.Kind == TokenKind.Star)
                return new SyntaxTypePointer(inner);
            else if (tokenOperator.Kind == TokenKind.Ampersand)
                return new SyntaxTypeReference(inner);
            else throw new UnreachableException();
        }
    }
}
