using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public sealed class ScoreParser
{
    public static ScoreSyntaxUnit ParseSyntaxUnit(ScoreContext context, SourceText source)
    {
        var tokens = ScoreLexer.ReadTokens(context, source);
        var unit = new ScoreSyntaxUnit(source, tokens);

        if (tokens.Count == 0)
            return unit;

        context.Assert(tokens[^1].Kind == ScoreTokenKind.EndOfFile, $"The end of the token list was not an EOF token.");
        var parser = new ScoreParser(context, source, tokens);

        while (true)
        {
            var beginLocation = parser.CurrentLocation;

            var node = parser.ParseTopLevel();
            unit.TopLevelNodes.Add(node);

            if (parser.IsAtEnd)
                break;

            context.Assert(parser.CurrentLocation > beginLocation, "Parser did not consume any tokens and the end of file was not reached.");
        }
        
        context.Diag.Flush();
        return unit;
    }

    private readonly ScoreContext _context;
    private readonly SourceText _source;
    private readonly List<ScoreToken> _tokens;

    private int _readPosition;

    private bool IsAtEnd => _readPosition >= _tokens.Count - 1;
    private ScoreToken CurrentToken => PeekToken();
    private SourceRange CurrentRange => CurrentToken.Range;
    private SourceLocation CurrentLocation => CurrentRange.Begin;

    private ScoreParser(ScoreContext context, SourceText source, List<ScoreToken> tokens)
    {
        _context = context;
        _source = source;
        _tokens = tokens;
    }

    private ScoreToken Consume()
    {
        if (IsAtEnd) return _tokens[^1];
        var result = _tokens[_readPosition];
        _readPosition++;
        return result;
    }

    private bool TryConsume(ScoreTokenKind kind, [NotNullWhen(true)] out ScoreToken? token)
    {
        token = null;
        if (CurrentToken.Kind != kind)
            return false;

        token = Consume();
        return true;
    }

    private ScoreToken PeekToken(int ahead = 0)
    {
        _context.Assert(ahead >= 0, $"Parameter {nameof(ahead)} to function {nameof(ScoreToken)}::{nameof(PeekToken)} must be non-negative; the parser should never rely on token look-back.");

        int peekIndex = _readPosition + ahead;
        if (peekIndex >= _tokens.Count)
        {
            var result = _tokens[^1];
            _context.Assert(result.Kind == ScoreTokenKind.EndOfFile, _source, result.Range.Begin, $"The end of the parser's token list was not an EOF token.");
            return result;
        }

        return _tokens[peekIndex];
    }

    private bool At(ScoreTokenKind kind)
    {
        return CurrentToken.Kind == kind;
    }

    private ScoreSyntaxNode ParseTopLevel()
    {
        switch (CurrentToken.Kind)
        {
            case ScoreTokenKind.EndOfFile:
            {
                // this is the end of the unit, there's no point in advancing the token stream.
                return new ScoreSyntaxEndOfUnit(CurrentToken);
            }

            case ScoreTokenKind.Func: return ParseDeclFunc();

            default:
            {
                _context.Todo(_source, CurrentLocation, "What do we do when we encounter a random token at the top level?");
                throw new UnreachableException();
            }
        }
    }

    #region Declarations

    private ScoreSyntaxDeclFunc ParseDeclFunc()
    {
        _context.Assert(At(ScoreTokenKind.Func), "Parser should be at the 'func' token to parse a func declaration.");

        var funcKeywordToken = Consume();
        var nameToken = ExpectIdentifier();

        _context.Todo(_source, CurrentLocation, "Parse a function declaration.");
        throw new UnreachableException();
    }

    #endregion
}
