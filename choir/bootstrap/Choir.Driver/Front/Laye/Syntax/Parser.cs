using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Choir.Front.Laye.Syntax;

public partial class Parser(Module module)
{
    public static void ParseSyntax(Module module)
    {
        if (module.TopLevelSyntax.Any())
            throw new InvalidOperationException("Can't repeatedly parse syntax into a module which already had syntax read into it.");
        
        var parser = new Parser(module);

        while (parser.ParseTopLevelSyntax() is {} topLevelNode)
            module.AddTopLevelSyntax(topLevelNode);
    }

    private static bool IsImportDeclForCHeader(SyntaxImport importDecl)
    {
        return importDecl. ImportKind == ImportKind.FilePath && importDecl.ModuleNameText.EndsWith(".h", StringComparison.InvariantCultureIgnoreCase);
    }

    public Module Module { get; } = module;
    public SourceFile SourceFile { get; } = module.SourceFile;
    public ChoirContext Context { get; } = module.Context;

    private readonly SyntaxToken[] _tokens = module.Tokens.ToArray();

    private int _position = 0;
    private bool _hasOnlyReadImports = true;

    private readonly List<SyntaxImport> _cHeaderImports = [];
    
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

    private IReadOnlyList<T> ParseDelimited<T>(Func<T> parser, TokenKind tokenDelimiter)
        where T : SyntaxNode
    {
        var results = new List<T>();
        do
        {
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
                Context.Diag.ICE(CurrentLocation, "parser is not finished please don't do that");
                throw new UnreachableException();
            }
        }
    }

    private IReadOnlyList<SyntaxImportQuery> ParseImportQueries() => ParseDelimited(ParseImportQuery, TokenKind.Comma);
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

    public SyntaxImport ParseImportDeclaration()
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
            return new SyntaxImport(tokenImport)
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
        return new SyntaxImport(tokenImport)
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

        var names = ParseDelimited(() => { ExpectIdentifier(out var tokenName); return tokenName; }, TokenKind.ColonColon);
        return SyntaxNameref.Create(names[names.Count - 1].Location, kind, names);
    }
}
