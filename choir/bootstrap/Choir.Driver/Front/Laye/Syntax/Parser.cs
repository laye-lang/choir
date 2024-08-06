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

    private static bool IsImportDeclForCHeader(SyntaxNode.Import importDecl)
    {
        return importDecl. ImportKind == ImportKind.FilePath && importDecl.ModuleNameText.EndsWith(".h", StringComparison.InvariantCultureIgnoreCase);
    }

    public Module Module { get; } = module;
    public SourceFile SourceFile { get; } = module.SourceFile;
    public ChoirContext Context { get; } = module.Context;

    private readonly SyntaxToken[] _tokens = module.Tokens.ToArray();

    private int _position = 0;
    private bool _hasOnlyReadImports = true;

    private readonly List<SyntaxNode.Import> _cHeaderImports = [];
    
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
        if (CurrentToken.Kind == TokenKind.Identifier && MemoryExtensions.SequenceEqual(CurrentToken.Location.Span(Context), keywordText))
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

    private void ExpectSemiColon()
    {
        if (!Consume(TokenKind.SemiColon))
            Context.Diag.Error(CurrentLocation, "expected ';'");
    }

    private void ExpectSemiColon(out SyntaxToken token)
    {
        if (!Consume(TokenKind.SemiColon, out token))
            Context.Diag.Error(CurrentLocation, "expected ';'");
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

    public SyntaxNode.Import ParseImportDeclaration()
    {
        Debug.Assert(CurrentToken.Kind == TokenKind.Import);
        Advance(out var tokenImport);

        if (TryAdvance(TokenKind.Star, out var tokenStar))
        {
            if (!Consume("from", TokenKind.From, out var tokenFrom))
                Context.Diag.Error(CurrentToken.Location, "expected 'from'");
            
            bool hasPath = Consume(TokenKind.LiteralString, out var tokenPath);
            if (!hasPath) Context.Diag.Error(tokenPath.Location, "expected import path name");

            ExpectSemiColon(out var tokenSemiColon);
            return new SyntaxNode.Import(tokenImport)
            {
                ImportKind = hasPath ? ImportKind.FilePath : ImportKind.Invalid,
                Queries = [new SyntaxNode.ImportQueryWildcard(tokenStar)],
                TokenFrom = tokenFrom,
                TokenModuleName = tokenPath,
                TokenSemiColon = tokenSemiColon,
            };
        }

        Context.Diag.ICE(CurrentLocation, "currently, only `import * from` is supported");
        throw new UnreachableException();
    }
}
