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
        TemplateArguments,
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

    public SyntaxToken Consume()
    {
        var result = CurrentToken;
        Advance();
        return result;
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

    private SyntaxToken Expect(TokenKind kind, string expected)
    {
        if (!Consume(kind, out var token))
            Context.Diag.Error(CurrentLocation, $"expected {expected}");
        return token;
    }

    // private void Expect(TokenKind kind, string expected)
    // {
    //     if (!Consume(kind))
    //         Context.Diag.Error(CurrentLocation, $"expected {expected}");
    // }

    private void Expect(TokenKind kind, string expected, out SyntaxToken token)
    {
        if (!Consume(kind, out token))
            Context.Diag.Error(CurrentLocation, $"expected {expected}");
    }

    private SyntaxToken ExpectSemiColon() { Expect(TokenKind.SemiColon, "';'", out var token); return token; }
    private void ExpectSemiColon(out SyntaxToken token) => Expect(TokenKind.SemiColon, "';'", out token);
    private SyntaxToken ExpectIdentifier() { Expect(TokenKind.Identifier, "an identifier", out var token); return token; }
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

    private void ExpectTemplateArgumentClose(out SyntaxToken tokenGreater)
    {
        tokenGreater = CurrentToken;

        // easy
        if (TryAdvance(TokenKind.Greater)) return;

        if (!At(TokenKind.GreaterGreater, TokenKind.GreaterGreaterGreater, TokenKind.GreaterEqual, TokenKind.GreaterGreaterEqual, TokenKind.GreaterGreaterGreaterEqual))
        {
            tokenGreater = new SyntaxToken(TokenKind.Missing, new Location(CurrentLocation.Offset, 0, SourceFile.FileId));
            Context.Diag.Error(CurrentToken.Location, $"expected '>'");
            return;
        }

        tokenGreater = new SyntaxToken(TokenKind.Greater, new Location(CurrentLocation.Offset, 1, SourceFile.FileId));

        TokenKind newTokenKind;
        switch (CurrentToken.Kind)
        {
            default:
            {
                Context.Diag.ICE("Checked for tokens which start with `>`, but ended up at a token that doesn't.");
                throw new UnreachableException();
            }

            case TokenKind.GreaterEqual: newTokenKind = TokenKind.Equal; break;
            case TokenKind.GreaterGreater: newTokenKind = TokenKind.Greater; break;
            case TokenKind.GreaterGreaterEqual: newTokenKind = TokenKind.GreaterEqual; break;
            case TokenKind.GreaterGreaterGreater: newTokenKind = TokenKind.GreaterGreater; break;
            case TokenKind.GreaterGreaterGreaterEqual: newTokenKind = TokenKind.GreaterGreaterEqual; break;
        }

        var newTokenLocation = new Location(CurrentLocation.Offset + 1, CurrentLocation.Length - 1, SourceFile.FileId);
        var newToken = new SyntaxToken(newTokenKind, newTokenLocation);

        _tokens[_position] = newToken;
    }

    private void SyncThrough(params TokenKind[] kinds)
    {
        while (!At(kinds)) Advance();
        if (At(kinds)) Advance();
    }

    private IReadOnlyList<T> ParseDelimited<T>(Func<T> parser, TokenKind tokenDelimiter, string expected, bool allowTrailing, params TokenKind[] closers)
        where T : SyntaxNode
    {
        Debug.Assert(!allowTrailing || closers.Length > 0, "if `allowTrailing` is specified, it must also specify closers");
        if (At(closers)) return [];

        var results = new List<T>();
        do
        {
            if (At(closers))
            {
                if (!allowTrailing)
                    Context.Diag.Error(CurrentLocation, $"expected {expected}");
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
            case TokenKind.Identifier when CurrentToken.TextValue == "static" && PeekAt(1, TokenKind.If):
                return ParseStaticIf(true);

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
                int position = _position;
                var declType = ParseType();
                if (position == _position)
                {
                    Context.Diag.Error(CurrentLocation, "expected a declaration");
                    var token = CurrentToken;
                    SyncThrough(TokenKind.SemiColon, TokenKind.CloseBrace);
                    return new SyntaxExprEmpty(token);
                }

                return ParseBindingOrFunctionDeclStartingAtName(declType);
            }
        }
    }

    private IReadOnlyList<SyntaxImportQuery> ParseImportQueries() => ParseDelimited(ParseImportQuery, TokenKind.Comma, "an identifier or '*'", false, TokenKind.SemiColon, TokenKind.OpenBrace, TokenKind.CloseBrace);
    private SyntaxImportQuery ParseImportQuery()
    {
        if (TryAdvance(TokenKind.Star, out var tokenStar))
            return new SyntaxImportQueryWildcard(tokenStar);
        else
        {
            if (At("as")) CurrentToken.Kind = TokenKind.As;
            var queryNameref = ParseNamerefNoTemplateArguments();

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
        SyntaxImportCFlags? cflags = null;
        SyntaxToken? tokenSemiColon = null;

        bool isQueryless = At(TokenKind.LiteralString) ||
            (At(TokenKind.Identifier) && PeekAt(1, TokenKind.SemiColon)) ||
            (At(TokenKind.Identifier) && PeekAt(1, "as") && PeekAt(2, TokenKind.Identifier) && PeekAt(3, TokenKind.SemiColon));

        if (isQueryless)
        {
            var tokenModuleName = CurrentToken;
            Advance();

            if (TryAdvance("as", TokenKind.As, out tokenAs))
                ExpectIdentifier(out tokenAlias);

            if (At("cflags"))
                cflags = ParseCFlags();
            else ExpectSemiColon(out tokenSemiColon);

            return new SyntaxDeclImport(tokenImport)
            {
                ImportKind = tokenModuleName.Kind == TokenKind.LiteralString ? ImportKind.FilePath : ImportKind.Library,
                Queries = [],
                TokenFrom = null,
                TokenModuleName = tokenModuleName,
                TokenAs = tokenAs,
                TokenAlias = tokenAlias,
                CFlags = cflags,
                TokenSemiColon = tokenSemiColon,
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

        if (At("cflags"))
            cflags = ParseCFlags();
        else ExpectSemiColon(out tokenSemiColon);

        return new SyntaxDeclImport(tokenImport)
        {
            ImportKind = tokenPath.Kind == TokenKind.LiteralString ? ImportKind.FilePath
                       : tokenPath.Kind == TokenKind.Identifier ? ImportKind.Library : ImportKind.Invalid,
            Queries = queries,
            TokenFrom = tokenFrom,
            TokenModuleName = tokenPath,
            TokenAs = tokenAs,
            TokenAlias = tokenAlias,
            CFlags = cflags,
            TokenSemiColon = tokenSemiColon,
        };

        SyntaxImportCFlags ParseCFlags()
        {
            if (!TryAdvance("cflags", TokenKind.CFlags, out var tokenCFlags))
                throw new UnreachableException();
            
            Expect(TokenKind.OpenBrace, "'{'", out var tokenOpenBrace);
            var flags = ParseDelimited(() =>
            {
                if (!Consume(TokenKind.LiteralString, out var tokenFlag))
                    Context.Diag.Error(CurrentLocation, "expected a string literal");
                return tokenFlag;
            }, TokenKind.Comma, "a string literal", true, TokenKind.CloseBrace);
            Expect(TokenKind.CloseBrace, "'}'", out var tokenCloseBrace);
            
            return new SyntaxImportCFlags(tokenCFlags, tokenOpenBrace, [.. flags], tokenCloseBrace);
        }
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

        SyntaxToken? tokenAssign = null;
        SyntaxNode? initializer = null;
        if (CurrentToken.Kind.IsAssignmentOperator())
        {
            tokenAssign = CurrentToken;
            Advance();
            initializer = ParseExpr(ExprParseContext.Default);
        }
        
        ExpectSemiColon(out var tokenSemiColon);
        return new SyntaxDeclBinding(bindingType, tokenName, tokenAssign, initializer, tokenSemiColon);
    }

    private SyntaxDeclParam ParseFunctionParameter()
    {
        var paramType = ParseType();
        ExpectIdentifier(out var tokenName);
        return new SyntaxDeclParam(paramType, tokenName);
    }

    private SyntaxDeclFunction ParseFunctionDeclStartingAtName(SyntaxNode returnType)
    {
        ExpectIdentifier(out var tokenName);
        Expect(TokenKind.OpenParen, "'('");
        var paramDecls = ParseDelimited(ParseFunctionParameter, TokenKind.Comma, "type", false, TokenKind.CloseParen, TokenKind.SemiColon);
        Expect(TokenKind.CloseParen, "')'");

        if (At(TokenKind.OpenBrace))
        {
            var body = ParseCompound();
            return new SyntaxDeclFunction(returnType, tokenName, paramDecls)
            {
                Body = body,
            };
        }

        ExpectSemiColon(out var tokenSemiColon);
        return new SyntaxDeclFunction(returnType, tokenName, paramDecls)
        {
            TokenSemiColon = tokenSemiColon,
        };
    }

    private SyntaxDeclFunction ParseFunctionDeclStartingWithinParameters(SyntaxNode returnType, SyntaxToken tokenName, SyntaxNode firstParamType)
    {
        Consume(TokenKind.Identifier, out var tokenFirstParamName);

        var firstParam = new SyntaxDeclParam(firstParamType, tokenFirstParamName);

        SyntaxDeclParam[] paramDecls;
        if (Consume(TokenKind.Comma))
        {
            var remainingParams = ParseDelimited(ParseFunctionParameter, TokenKind.Comma, "type", false, TokenKind.CloseParen, TokenKind.SemiColon);
            paramDecls = [firstParam, .. remainingParams];
        }
        else paramDecls = [firstParam];

        Expect(TokenKind.CloseParen, "')'");

        if (At(TokenKind.OpenBrace))
        {
            var body = ParseCompound();
            return new SyntaxDeclFunction(returnType, tokenName, paramDecls)
            {
                Body = body,
            };
        }

        ExpectSemiColon(out var tokenSemiColon);
        return new SyntaxDeclFunction(returnType, tokenName, paramDecls)
        {
            TokenSemiColon = tokenSemiColon,
        };
    }

    private SyntaxNode ParseStmtContinue(SyntaxNode expr)
    {
        SyntaxToken tokenSemiColon;

        if (CurrentToken.Kind.IsAssignmentOperator())
        {
            var tokenAssign = CurrentToken;
            Advance();

            var rhs = ParseExpr(ExprParseContext.Default);
            ExpectSemiColon(out tokenSemiColon);
            return new SyntaxStmtAssign(expr, tokenAssign, rhs, tokenSemiColon);
        }
        
        ExpectSemiColon(out tokenSemiColon);
        return new SyntaxStmtExpr(expr, tokenSemiColon);
    }

    private SyntaxNode ParseSyntaxInStmtContext()
    {
        // if it's *definitely* a type, we *definitely* want to return a binding/function declaration
        if (IsDefinitelyTypeStart(CurrentToken.Kind))
        {
            var declType = ParseType();
            return ParseBindingOrFunctionDeclStartingAtName(declType);
        }

        switch (CurrentToken.Kind)
        {
            case TokenKind.OpenBrace:
            case TokenKind.Assert:
            case TokenKind.Break:
            case TokenKind.Continue:
            case TokenKind.Defer:
            case TokenKind.Do:
            case TokenKind.For:
            case TokenKind.Goto:
            case TokenKind.Identifier when CurrentToken.TextValue == "static" && PeekAt(1, TokenKind.If):
            case TokenKind.If:
            case TokenKind.Return:
            case TokenKind.While:
            case TokenKind.Xyzzy:
            case TokenKind.Yield:
                return ParseStmt();

            default:
            {
                int position = _position;
                var syntax = ParseExpr(ExprParseContext.CheckForDeclarations);
                if (syntax is SyntaxExprEmpty syntaxEmpty) return new SyntaxStmtExpr(syntax, syntaxEmpty.TokenSemiColon);
                if (position == _position)
                {
                    Context.Diag.Error(CurrentLocation, "expected an expression");
                    var token = CurrentToken;
                    SyncThrough(TokenKind.SemiColon, TokenKind.CloseBrace);
                    return new SyntaxExprEmpty(token);
                }

                if (syntax.IsDecl) return syntax;
                if (syntax.CanBeType && At(TokenKind.Identifier))
                    return ParseBindingOrFunctionDeclStartingAtName(syntax);

                return ParseStmtContinue(syntax);
            }
        }
    }

    private SyntaxNode ParseStmt()
    {
        switch (CurrentToken.Kind)
        {
            case TokenKind.OpenBrace: return ParseCompound();

            case TokenKind.Assert:
            {
                var tokenAssert = Consume();
                var condition = ParseExpr(ExprParseContext.Default);
                SyntaxToken? message = null;
                if (TryAdvance(TokenKind.Comma, out var tokenComma)) message = Expect(TokenKind.LiteralString, "a literal string");
                var tokenSemiColon = ExpectSemiColon();
                return new SyntaxStmtAssert(tokenAssert, condition, tokenComma, message, tokenSemiColon);
            }

            case TokenKind.Break:
            {
                var tokenBreak = Consume();
                TryAdvance(TokenKind.Identifier, out var tokenLabel);
                ExpectSemiColon(out var tokenSemiColon);
                return new SyntaxStmtBreak(tokenBreak, tokenLabel, tokenSemiColon);
            }

            case TokenKind.Continue:
            {
                var tokenContinue = Consume();
                TryAdvance(TokenKind.Identifier, out var tokenLabel);
                ExpectSemiColon(out var tokenSemiColon);
                return new SyntaxStmtContinue(tokenContinue, tokenLabel, tokenSemiColon);
            }

            case TokenKind.Defer:
            {
                var tokenDefer = Consume();
                var stmt = ParseStmt();
                return new SyntaxStmtDefer(tokenDefer, stmt);
            }

            case TokenKind.Do:
            {
                var tokenDo = Consume();
                var body = ParseStmt();
                Expect(TokenKind.While, "'while'", out var tokenWhile);
                Expect(TokenKind.OpenParen, "'('", out var tokenOpenParen);
                var condition = ParseExpr(ExprParseContext.Default);
                Expect(TokenKind.CloseParen, "')'", out var tokenCloseParen);
                var tokenSemiColon = ExpectSemiColon();
                return new SyntaxStmtDoLoop(tokenDo, body, tokenWhile, condition);
            }

            case TokenKind.For: throw new UnreachableException();

            case TokenKind.Goto: return new SyntaxStmtGoto(Consume(), ExpectIdentifier(), ExpectSemiColon());

            case TokenKind.Identifier when CurrentToken.TextValue == "static" && PeekAt(1, TokenKind.If):
                return ParseStaticIf(false);

            case TokenKind.If: return ParseIf();

            case TokenKind.Return:
            {
                var tokenReturn = Consume();
                SyntaxNode? value = At(TokenKind.SemiColon) ? null : ParseExpr(ExprParseContext.Default);
                ExpectSemiColon(out var tokenSemiColon);
                return new SyntaxStmtReturn(tokenReturn, value, tokenSemiColon);
            }

            case TokenKind.While: throw new UnreachableException();

            case TokenKind.Xyzzy: return new SyntaxStmtXyzzy(Consume(), ExpectSemiColon());

            case TokenKind.Yield when !PeekAt(1, TokenKind.Break, TokenKind.Return):
                return new SyntaxStmtYield(Consume(), ParseExpr(ExprParseContext.Default), ExpectSemiColon());

            default:
            {
                var expr = ParseExpr(ExprParseContext.Default);
                return ParseStmtContinue(expr);
            }
        }
    }

    private SyntaxCompound ParseCompound()
    {
        Expect(TokenKind.OpenBrace, "'{'", out var tokenOpenBrace);

        var body = new List<SyntaxNode>();
        while (!IsAtEnd && !At(TokenKind.CloseBrace))
        {
            var stmt = ParseSyntaxInStmtContext();
            body.Add(stmt);
        }
        
        Expect(TokenKind.CloseBrace, "'}'");

        return new SyntaxCompound(tokenOpenBrace.Location, [.. body]);
    }

    private SyntaxStaticIf ParseStaticIf(bool isTopLevelOnly)
    {
        Debug.Assert(At("static") && PeekAt(1, TokenKind.If));
        if (!TryAdvance("static", TokenKind.Static, out var tokenStatic))
            throw new UnreachableException();

        var primaries = new List<SyntaxIfPrimary>
        {
            ParseStaticIfPrimary()
        };

        while (TryAdvance(TokenKind.Else, out var tokenElse))
        {
            if (At(TokenKind.If))
                primaries.Add(ParseStaticIfPrimary());
            else
            {
                var elseBody = ParseStaticBody();
                return new SyntaxStaticIf(tokenStatic, [.. primaries], tokenElse, elseBody);
            }
        }
        
        return new SyntaxStaticIf(tokenStatic, [.. primaries], null, null);

        SyntaxIfPrimary ParseStaticIfPrimary()
        {
            Debug.Assert(At(TokenKind.If));

            var tokenIf = CurrentToken;
            Advance();

            Expect(TokenKind.OpenParen, "'('");
            var condition = ParseExpr(ExprParseContext.Default);
            Expect(TokenKind.CloseParen, "')'");

            SyntaxNode body = ParseStaticBody();
            return new SyntaxIfPrimary(tokenIf, condition, body);
        }

        SyntaxNode ParseStaticBody()
        {
            if (At(TokenKind.OpenBrace))
            {
                var tokenOpenBrace = Consume();

                var bodyNodes = new List<SyntaxNode>();
                while (!IsAtEnd && !At(TokenKind.CloseBrace))
                {
                    if (isTopLevelOnly)
                    {
                        var stmt = ParseTopLevelSyntax();
                        if (stmt is null) break;
                        bodyNodes.Add(stmt);
                    }
                    else bodyNodes.Add(ParseSyntaxInStmtContext());
                }
                
                Expect(TokenKind.CloseBrace, "'}'");
                return new SyntaxCompound(tokenOpenBrace.Location, [.. bodyNodes]);
            }
            else return isTopLevelOnly ? (ParseTopLevelSyntax() ?? Expect(TokenKind.OpenBrace, "'{' or declaration")) : ParseStmt();
        }
    }

    private SyntaxIf ParseIf()
    {
        var primaries = new List<SyntaxIfPrimary>
        {
            ParseIfPrimary()
        };

        while (TryAdvance(TokenKind.Else, out var tokenElse))
        {
            if (At(TokenKind.If))
                primaries.Add(ParseIfPrimary());
            else
            {
                var elseBody = ParseStmt();
                return new SyntaxIf([.. primaries], tokenElse, elseBody);
            }
        }
        
        return new SyntaxIf([.. primaries], null, null);

        SyntaxIfPrimary ParseIfPrimary()
        {
            Debug.Assert(At(TokenKind.If));

            var tokenIf = CurrentToken;
            Advance();

            Expect(TokenKind.OpenParen, "'('");
            var condition = ParseExpr(ExprParseContext.Default);
            Expect(TokenKind.CloseParen, "')'");

            var body = ParseStmt();
            return new SyntaxIfPrimary(tokenIf, condition, body);
        }
    }

    private SyntaxToken[] ParseNamerefImpl(out NamerefKind kind)
    {
        kind = NamerefKind.Default;
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
            "an identifier",
            false
        );
        Debug.Assert(names.Count > 0);

        return [.. names];
    }

    private SyntaxNameref ParseNamerefNoTemplateArguments()
    {
        var names = ParseNamerefImpl(out var kind);
        return SyntaxNameref.Create(names[names.Length - 1].Location, kind, names, null);
    }

    private SyntaxNode ParseNamerefWithTemplateArgumentCheck()
    {
        var names = ParseNamerefImpl(out var kind);
        var lastNameLocation = names[names.Length - 1].Location;

        SyntaxTemplateArguments? templateArguments = null;
        if (TryAdvance(TokenKind.Less, out var tokenLess))
        {
            var arg = ParseTemplateArgument();
            if (!At(TokenKind.Comma, TokenKind.Greater, TokenKind.GreaterGreater, TokenKind.GreaterGreaterGreater, TokenKind.GreaterEqual, TokenKind.GreaterGreaterEqual, TokenKind.GreaterGreaterGreaterEqual))
            {
                var nameref = SyntaxNameref.Create(lastNameLocation, kind, names, null);
                var binary = new SyntaxExprBinary(nameref, arg, tokenLess);
                return ParseBinaryExpr(ExprParseContext.Default, binary, TokenKind.Less.GetBinaryOperatorPrecedence());
            }

            var args = new List<SyntaxNode>() { arg };
            if (TryAdvance(TokenKind.Comma))
            {
                do
                {
                    arg = ParseTemplateArgument();
                    args.Add(arg);
                } while (TryAdvance(TokenKind.Comma));
            }

            ExpectTemplateArgumentClose(out var tokenGreater);
            templateArguments = new([.. args]);
        }

        return SyntaxNameref.Create(lastNameLocation, kind, names, templateArguments);
    }

    private SyntaxNode ParseTemplateArgument()
    {
        if (IsDefinitelyTypeStart(CurrentToken.Kind))
            return ParseType();
    
        return ParseExpr(ExprParseContext.TemplateArguments);
    }

    private SyntaxNode ParseTypeContinuation(SyntaxNode typeNode)
    {
        switch (CurrentToken.Kind)
        {
            default: return typeNode;
            
            case TokenKind.Mut: break;

            case TokenKind.OpenBracket when Peek(1).Kind == TokenKind.CloseBracket:
            {
                for (int i = 0; i < 2; i++) Advance();
                typeNode = new SyntaxTypeSlice(typeNode);
            } break;

            case TokenKind.OpenBracket when Peek(1).Kind == TokenKind.Star && Peek(2).Kind == TokenKind.CloseBracket:
            {
                for (int i = 0; i < 3; i++) Advance();
                typeNode = new SyntaxTypeBuffer(typeNode, null);
            } break;
            
            case TokenKind.OpenBracket when Peek(1).Kind == TokenKind.Star && Peek(2).Kind == TokenKind.Colon:
            {
                for (int i = 0; i < 3; i++) Advance();
                var terminatorExpr = ParseExpr(ExprParseContext.Default);
                Expect(TokenKind.CloseBracket, "']'");
                typeNode = new SyntaxTypeBuffer(typeNode, terminatorExpr);
            } break;
            
            case TokenKind.OpenBracket:
            {
                var tokenOpenBracket = CurrentToken;
                Advance();

                var indices = ParseDelimited(() => ParseExpr(ExprParseContext.Default), TokenKind.Comma, "an expression", false, TokenKind.CloseBracket, TokenKind.SemiColon);

                Expect(TokenKind.CloseBracket, "']'", out var tokenCloseBracket);
                // the index node represents both expression forms of indexing as well as the array type, (hopefully) for simplicity
                typeNode = new SyntaxIndex(typeNode, tokenOpenBracket, indices, tokenCloseBracket);
            } break;

            case TokenKind.Star:
            {
                Advance();
                typeNode = new SyntaxTypePointer(typeNode);
            } break;

            case TokenKind.Ampersand:
            {
                Advance();
                typeNode = new SyntaxTypeReference(typeNode);
            } break;

            case TokenKind.Question:
            {
                Advance();
                typeNode = new SyntaxTypeNilable(typeNode);
            } break;
        }

        if (TryAdvance(TokenKind.Mut, out var tokenMut))
        {
            typeNode = new SyntaxQualMut(typeNode, tokenMut);
            while (TryAdvance(TokenKind.Mut, out var leadingMut)) {
                Context.Diag.Error(leadingMut.Location, "duplicate 'mut' qualifier");
            }
        }

        return ParseTypeContinuation(typeNode);
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
                var innerType = ParseType();
                Expect(TokenKind.CloseParen, "')'");
                return new SyntaxGrouped(innerType);
            }

            case TokenKind.Var: Advance(); typeNode = currentToken; break;
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
                typeNode = ParseNamerefWithTemplateArgumentCheck();
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

        return ParseTypeContinuation(typeNode);
    }
    
    private SyntaxNode ParseExpr(ExprParseContext parseContext)
    {
        var primary = ParsePrimaryExpr();
        return ParseBinaryExpr(parseContext, primary);
    }

    private SyntaxExprCall ParseExprCallFromFirstArg(SyntaxNode callee, SyntaxNode firstArg)
    {
        SyntaxNode[] args;
        if (TryAdvance(TokenKind.Comma))
        {
            var remainingArgs = ParseDelimited(() => ParseExpr(ExprParseContext.Default), TokenKind.Comma, "an expression", false, TokenKind.CloseParen, TokenKind.SemiColon);
            args = [firstArg, .. remainingArgs];
        }
        else args = [firstArg];

        Expect(TokenKind.CloseParen, "')'");
        return new SyntaxExprCall(callee, args);
    }

    private SyntaxConstructorInit ParseConstructorInit()
    {
        var exprValue = ParseExpr(ExprParseContext.Default);
        return new(exprValue.Location, exprValue);
    }

    private SyntaxNode ParsePrimaryExprContinuation(SyntaxNode primary)
    {
        switch (CurrentToken.Kind)
        {
            default: return primary;

            // mut applies to any type any time
            case TokenKind.Mut when primary.CanBeType:
            // `type**`, `type*&`, `type&&` and `type&*` are always pointer/reference types
            case TokenKind.Star or TokenKind.Ampersand when PeekAt(1, TokenKind.Star, TokenKind.Ampersand) && primary.CanBeType:
            // `type* mut` and `type& mut` are always pointer/reference types
            case TokenKind.Star or TokenKind.Ampersand when PeekAt(1, TokenKind.Mut) && primary.CanBeType:
            // `type[*]` and `type[*:` are always buffer types
            case TokenKind.OpenBracket when Peek(1).Kind == TokenKind.Star && Peek(2).Kind is TokenKind.CloseBracket or TokenKind.Colon && primary.CanBeType:
                return ParseTypeContinuation(primary);
                
            // while `type[]` is always a slice type, it could also be an array/dynamic index missing its indices
            // so just in case, we return the slice type without falling back to the type-only parser.
            // if sema encounters a slice type in an expression-only context, it should know what to report.
            case TokenKind.OpenBracket when Peek(1).Kind == TokenKind.CloseBracket && primary.CanBeType:
            {
                for (int i = 0; i < 2; i++) Advance();
                return ParsePrimaryExprContinuation(new SyntaxTypeSlice(primary));
            }
            
            case TokenKind.OpenParen:
            {
                var tokenOpenParen = CurrentToken;
                Advance();

                var indices = ParseDelimited(() => ParseExpr(ExprParseContext.Default), TokenKind.Comma, "an expression", false, TokenKind.CloseParen, TokenKind.SemiColon);

                Expect(TokenKind.CloseParen, "')'", out var tokenCloseParen);
                return ParsePrimaryExprContinuation(new SyntaxCall(primary, tokenOpenParen, indices, tokenCloseParen));
            }
            
            case TokenKind.OpenBracket:
            {
                var tokenOpenBracket = CurrentToken;
                Advance();

                var indices = ParseDelimited(() => ParseExpr(ExprParseContext.Default), TokenKind.Comma, "an expression", false, TokenKind.CloseBracket, TokenKind.SemiColon);

                Expect(TokenKind.CloseBracket, "']'", out var tokenCloseBracket);
                return ParsePrimaryExprContinuation(new SyntaxIndex(primary, tokenOpenBracket, indices, tokenCloseBracket));
            }

            case TokenKind.Dot:
            {
                Advance();
                ExpectIdentifier(out var fieldName);
                return ParsePrimaryExprContinuation(new SyntaxExprField(primary, fieldName));
            }
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

            case TokenKind.SemiColon:
            {
                var tokenSemiColon = CurrentToken;
                Advance();
                return new SyntaxExprEmpty(tokenSemiColon);
            }

            case TokenKind.OpenParen:
            {
                Advance();
                var innerExpr = ParseExpr(ExprParseContext.Default);
                Expect(TokenKind.CloseParen, "')'");
                return new SyntaxGrouped(innerExpr);
            }

            case TokenKind.Global:
            case TokenKind.ColonColon:
            case TokenKind.Identifier:
            {
                var nameref = ParseNamerefWithTemplateArgumentCheck();
                return ParsePrimaryExprContinuation(nameref);
            }

            case TokenKind.True:
            case TokenKind.False:
            case TokenKind.LiteralString:
            case TokenKind.LiteralRune:
            case TokenKind.LiteralInteger:
            case TokenKind.LiteralFloat:
            {
                Advance();
                return currentToken;
            }

            case TokenKind.Cast:
            {
                var tokenCast = CurrentToken;
                Advance();

                Expect(TokenKind.OpenParen, "'('");

                SyntaxNode? targetType = null;
                if (!At(TokenKind.CloseParen))
                    targetType = ParseType();

                Expect(TokenKind.CloseParen, "')'");

                var expr = ParsePrimaryExpr();
                return new SyntaxExprCast(tokenCast, targetType, expr);
            }

            case TokenKind.Plus:
            case TokenKind.Minus:
            case TokenKind.Star:
            case TokenKind.Ampersand:
            case TokenKind.Tilde:
            case TokenKind.Not:
            {
                var tokenOperator = CurrentToken;
                Advance();
                var expr = ParsePrimaryExpr();
                return new SyntaxExprUnary(tokenOperator, expr);
            }

            case TokenKind.If:
            {
                var exprIf = ParseIf();
                if (exprIf.ElseBody is null)
                    Context.Diag.Error(exprIf.Location, "`if` in expression context requires `else` with no condition");
                return exprIf;
            }

            case TokenKind.Do when PeekAt(1, TokenKind.OpenBrace):
            {
                Advance();
                return ParseCompound();
            }

            case TokenKind.OpenBrace:
            {
                var tokenOpenBrace = CurrentToken;
                Advance();

                var inits = ParseDelimited(ParseConstructorInit, TokenKind.Comma, "initializer", true, TokenKind.CloseBrace, TokenKind.SemiColon);

                Expect(TokenKind.CloseBrace, "'}'", out var tokenCloseBrace);
                return new SyntaxExprConstructor(tokenOpenBrace, inits, tokenCloseBrace);
            }

            default:
            {
                Context.Diag.Error(CurrentLocation, "expected an expression");
                return new SyntaxToken(TokenKind.Missing, new Location(CurrentLocation.Offset, 1, SourceFile.FileId));
            }
        }
    }

    private SyntaxNode ParseBinaryExpr(ExprParseContext parseContext, SyntaxNode lhs, int precedence = 0)
    {
        // if we're parsing template arguments, ensure we never allow arguments with
        // precedence less than or equal to `<` and `>`.
        if (parseContext == ExprParseContext.TemplateArguments)
            precedence = Math.Max(precedence, TokenKind.Plus.GetBinaryOperatorPrecedence());

        while (CurrentToken.Kind.CanBeBinaryOperator() && CurrentToken.Kind.GetBinaryOperatorPrecedence() >= precedence)
        {
            var tokenOperator = CurrentToken;
            Advance();

            SyntaxNode? rhs = null;
            if (parseContext == ExprParseContext.CheckForDeclarations &&
                tokenOperator.Kind is TokenKind.Star or TokenKind.Ampersand &&
                At(TokenKind.Identifier) &&
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

                    if (IsDefinitelyTypeStart(Peek(2).Kind))
                    {
                        // this is a function declaration/definition, since the next token
                        // within the open paren *must* start a type.
                        SyntaxNode returnType = CreateTypeNodeFromOperator(lhs, tokenOperator);
                        return ParseFunctionDeclStartingAtName(returnType);
                    }

                    Consume(TokenKind.Identifier, out var tokenIdent);
                    Consume(TokenKind.OpenParen, out var tokenOpenParen);

                    var firstParamOrArg = ParseExpr(ExprParseContext.Default);
                    if (firstParamOrArg.CanBeType && At(TokenKind.Identifier))
                    {
                        // this should be a parameter declaration
                        SyntaxNode returnType = CreateTypeNodeFromOperator(lhs, tokenOperator);
                        return ParseFunctionDeclStartingWithinParameters(returnType, tokenIdent, firstParamOrArg);
                    }

                    // otherwise we give up, it's an invocation on the RHS
                    var syntaxCallee = SyntaxNameref.Create(tokenIdent);
                    rhs = ParseExprCallFromFirstArg(syntaxCallee, firstParamOrArg);
                }
            }
            
            rhs ??= ParsePrimaryExpr();

            bool isTokenOperatorRightAssoc = tokenOperator.Kind.IsRightAssociativeBinaryOperator();
            while (CurrentToken.Kind.CanBeBinaryOperator() && (
                    CurrentToken.Kind.GetBinaryOperatorPrecedence() > tokenOperator.Kind.GetBinaryOperatorPrecedence() ||
                    (isTokenOperatorRightAssoc && CurrentToken.Kind.GetBinaryOperatorPrecedence() == tokenOperator.Kind.GetBinaryOperatorPrecedence())
                ))
            {
                rhs = ParseBinaryExpr(ExprParseContext.Default, rhs, CurrentToken.Kind.GetBinaryOperatorPrecedence());
            }
                
            lhs = new SyntaxExprBinary(lhs, rhs, tokenOperator);
        }

        return lhs;

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
