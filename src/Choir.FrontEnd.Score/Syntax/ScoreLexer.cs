using Choir.FrontEnd.Score.Diagnostics;
using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public sealed class ScoreLexer
{
    public static List<ScoreToken> ReadTokens(ScoreContext context, SourceText source)
    {
        var lexer = new ScoreLexer(context, source);
        var tokens = new List<ScoreToken>();

        while (true)
        {
            var token = lexer.ReadToken();
            tokens.Add(token);

            if (token.Kind == ScoreTokenKind.EndOfFile)
                break;
        }

        return tokens;
    }

    private readonly ScoreContext _context;
    private readonly SourceText _source;

    private int _readPosition;

    private bool IsAtEnd => _readPosition >= _source.Text.Length;
    private char CurrentCharacter => PeekCharacter();
    private SourceLocation CurrentLocation => new(_readPosition);

    private ScoreLexer(ScoreContext context, SourceText source)
    {
        _context = context;
        _source = source;
    }

    private void Advance(int amount = 1)
    {
        _context.Assert(amount >= 1, $"Parameter {nameof(amount)} to function {nameof(ScoreLexer)}::{nameof(Advance)} must be positive; advancing the lexer must always move forward at least one character if possible.");
        if (IsAtEnd) return;
        _readPosition = Math.Min(_readPosition + amount, _source.Text.Length);
    }

    private char PeekCharacter(int ahead = 0)
    {
        _context.Assert(ahead >= 0, $"Parameter {nameof(ahead)} to function {nameof(ScoreLexer)}::{nameof(PeekCharacter)} must be non-negative; the lexer should never rely on character look-back.");

        int peekIndex = _readPosition + ahead;
        if (peekIndex >= _source.Text.Length)
            return '\0';

        return _source.Text[peekIndex];
    }

    private SourceRange GetRange(SourceLocation beginLocation) => new(beginLocation, CurrentLocation);

    private ScoreTriviaList ReadTrivia(bool isLeading)
    {
        if (IsAtEnd) return new([], isLeading);

        var trivia = new List<ScoreTrivia>(2);
        while (!IsAtEnd)
        {
            var beginLocation = CurrentLocation;
            switch (CurrentCharacter)
            {
                // both \r\n and \n\r pairings should be treated as a single newline trivia.
                case '\r' when PeekCharacter(1) == '\n':
                case '\n' when PeekCharacter(1) == '\r':
                {
                    Advance(2);
                    trivia.Add(new ScoreTriviaNewLine(GetRange(beginLocation)));
                    // Trailing trivia always ends with a newline if encountered.
                    if (!isLeading) return new(trivia, isLeading);
                } break;

                // otherwise, both lone \r and lone \n are also single newline trivia.
                case '\r' or '\n':
                {
                    Advance();
                    trivia.Add(new ScoreTriviaNewLine(GetRange(beginLocation)));
                    // Trailing trivia always ends with a newline if encountered.
                    if (!isLeading) return new(trivia, isLeading);
                } break;

                case ' ' or '\t' or '\v':
                {
                    Advance();
                    while (!IsAtEnd && CurrentCharacter is ' ' or '\t' or '\v')
                        Advance();
                    trivia.Add(new ScoreTriviaWhiteSpace(GetRange(beginLocation)));
                } break;

                case '/' when PeekCharacter(1) == '/':
                {
                    Advance(2);
                    while (!IsAtEnd && CurrentCharacter is not '\r' or '\n')
                        Advance();
                    trivia.Add(new ScoreTriviaLineComment(GetRange(beginLocation)));
                } break;

                case '/' when PeekCharacter(1) == '*':
                {
                    Advance(2);

                    int depth = 1;
                    while (depth > 0 && !IsAtEnd)
                    {
                        if (CurrentCharacter == '*' && PeekCharacter(1) == '/')
                        {
                            Advance(2);
                            depth--;
                        }
                        else if (CurrentCharacter == '/' && PeekCharacter(1) == '*')
                        {
                            Advance(2);
                            depth++;
                        }
                        else Advance();
                    }

                    if (depth > 0)
                        _context.ErrorUnclosedComment(_source, beginLocation);

                    trivia.Add(new ScoreTriviaDelimitedComment(GetRange(beginLocation)));
                } break;

                // A shebang, `#!`, at the very start of the file is treated as a line comment.
                // This allows running Score files as scripts on Unix systems without also making `#` or `#!` line comment sequences anywhere else.
                case '#' when _readPosition == 0 && PeekCharacter(1) == '!':
                {
                    Advance(2);
                    while (!IsAtEnd && CurrentCharacter is not ('\r' or '\n'))
                        Advance();
                    trivia.Add(new ScoreTriviaShebangComment(GetRange(beginLocation)));
                } break;

                // when nothing matches, there is no trivia to read; simply return what we currently have.
                default: return new(trivia, isLeading);
            }
        }

        // end of file broke us out of the loop; simply return what we read.
        return new(trivia, isLeading);
    }

    public ScoreToken ReadToken()
    {
        var leadingTrivia = ReadTrivia(isLeading: true);
        var beginLocation = CurrentLocation;

        if (IsAtEnd)
            return new(ScoreTokenKind.EndOfFile, GetRange(beginLocation), leadingTrivia, new([], false));

        var tokenKind = ScoreTokenKind.Invalid;

        char c = CurrentCharacter;
        switch (c)
        {
            default:
            {
                _context.ErrorUnexpectedCharacter(_source, beginLocation);
                tokenKind = ScoreTokenKind.UnexpectedCharacter;
                Advance();
            } break;
        }

        var tokenRange = GetRange(beginLocation);
        _context.Assert(_readPosition > beginLocation.Offset, $"{nameof(ScoreLexer)}::{nameof(ReadToken)} failed to consume any non-trivia characters from the source text and did not return an EOF token.");
        _context.Assert(tokenKind != ScoreTokenKind.Invalid, $"{nameof(ScoreLexer)}::{nameof(ReadToken)} failed to assign a non-invalid kind to the read token.");

        var trailingTrivia = ReadTrivia(isLeading: false);
        return new(tokenKind, tokenRange, leadingTrivia, trailingTrivia);
    }
}
