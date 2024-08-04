using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Choir.Front.Laye.Syntax;

public sealed class Lexer(SourceFile sourceFile)
{
    public static void ReadTokens(SyntaxModule module)
    {
        if (module.Tokens.Any())
            throw new InvalidOperationException("Can't repeatedly read tokens into a module which already had tokens read into it.");
        
        var lexer = new Lexer(module.SourceFile);

        SyntaxToken token;
        do
        {
            token = lexer.ReadToken();
            module.AddToken(token);
        } while (token.Kind != SyntaxKind.TokenEndOfFile);
    }

    private static readonly Dictionary<string, SyntaxKind> _keywordTokensKinds = new()
    {
        {"var", SyntaxKind.TokenVar},
        {"void", SyntaxKind.TokenVoid},
        {"noreturn", SyntaxKind.TokenNoReturn},
        {"bool", SyntaxKind.TokenBool},
        {"int", SyntaxKind.TokenInt},
        {"true", SyntaxKind.TokenTrue},
        {"false", SyntaxKind.TokenFalse},
        {"nil", SyntaxKind.TokenNil},
        {"if", SyntaxKind.TokenIf},
        {"else", SyntaxKind.TokenElse},
        {"for", SyntaxKind.TokenFor},
        {"while", SyntaxKind.TokenWhile},
        {"do", SyntaxKind.TokenDo},
        {"switch", SyntaxKind.TokenSwitch},
        {"case", SyntaxKind.TokenCase},
        {"default", SyntaxKind.TokenDefault},
        {"return", SyntaxKind.TokenReturn},
        {"break", SyntaxKind.TokenBreak},
        {"continue", SyntaxKind.TokenContinue},
        {"fallthrough", SyntaxKind.TokenFallthrough},
        {"yield", SyntaxKind.TokenYield},
        {"unreachable", SyntaxKind.TokenUnreachable},
        {"defer", SyntaxKind.TokenDefer},
        {"discard", SyntaxKind.TokenDiscard},
        {"goto", SyntaxKind.TokenGoto},
        {"xyzzy", SyntaxKind.TokenXyzzy},
        {"assert", SyntaxKind.TokenAssert},
        {"try", SyntaxKind.TokenTry},
        {"catch", SyntaxKind.TokenCatch},
        {"struct", SyntaxKind.TokenStruct},
        {"variant", SyntaxKind.TokenVariant},
        {"enum", SyntaxKind.TokenEnum},
        {"alias", SyntaxKind.TokenAlias},
        {"template", SyntaxKind.TokenTemplate},
        {"test", SyntaxKind.TokenTest},
        {"import", SyntaxKind.TokenImport},
        {"export", SyntaxKind.TokenExport},
        {"operator", SyntaxKind.TokenOperator},
        {"mut", SyntaxKind.TokenMut},
        {"new", SyntaxKind.TokenNew},
        {"delete", SyntaxKind.TokenDelete},
        {"cast", SyntaxKind.TokenCast},
        {"is", SyntaxKind.TokenIs},
        {"sizeof", SyntaxKind.TokenSizeof},
        {"alignof", SyntaxKind.TokenAlignof},
        {"offsetof", SyntaxKind.TokenOffsetof},
        {"not", SyntaxKind.TokenNot},
        {"and", SyntaxKind.TokenAnd},
        {"or", SyntaxKind.TokenOr},
        {"xor", SyntaxKind.TokenXor},
        {"varargs", SyntaxKind.TokenVarargs},
        {"const", SyntaxKind.TokenConst},
        {"foreign", SyntaxKind.TokenForeign},
        {"inline", SyntaxKind.TokenInline},
        {"callconv", SyntaxKind.TokenCallconv},
        {"pure", SyntaxKind.TokenPure},
        {"discardable", SyntaxKind.TokenDiscardable},
    };

    public SourceFile SourceFile { get; } = sourceFile;
    public ChoirContext Context { get; } = sourceFile.Context;

    private readonly int _fileId = sourceFile.FileId;
    private readonly string _text = sourceFile.Text;
    private readonly int _textLength = sourceFile.Text.Length;

    private struct TokenInfo
    {
        public SyntaxKind Kind;
        public int Position;
        public int Length;
        public string TextValue = "";
        public BigInteger IntegerValue = BigInteger.Zero;

        public TokenInfo()
        {
        }
    }

    private enum ScanNumberFlags
    {
        NotNumber,
        VanillaInteger,
        RadixInteger,
        VanillaFloat,
        RadixFloat,
    }

    private class LexerResetPoint(Lexer lexer) : IDisposable
    {
        private readonly Lexer _lexer = lexer;
        private readonly int _position = lexer._position;

        public void Dispose()
        {
            _lexer._position = _position;
        }
    }

    private int _position;
    private readonly StringBuilder _stringBuilder = new();

    private bool IsAtEnd => _position >= _textLength;
    private char CurrentCharacter => Peek(0);
    private Location CurrentLocation => new(_position, 1, _fileId);

    private char Peek(int ahead)
    {
        Debug.Assert(ahead >= 0);

        int peekPosition = _position + ahead;
        if (peekPosition >= _textLength)
            return '\0';

        return _text[peekPosition];
    }

    private void Advance()
    {
        _position++;
    }

    private bool TryAdvance(char c)
    {
        if (CurrentCharacter != c)
            return false;

        Advance();
        return true;
    }

    public SyntaxToken ReadToken()
    {
        TokenInfo tokenInfo = default;
        ReadTokenInfo(ref tokenInfo);
        return new SyntaxToken(tokenInfo.Kind, new Location(tokenInfo.Position, tokenInfo.Length, _fileId))
        {
            TextValue = tokenInfo.TextValue,
            IntegerValue = tokenInfo.IntegerValue,
        };
    }

    private void ReadTokenInfo(ref TokenInfo tokenInfo)
    {
        SkipTrivia();

        tokenInfo.Kind = SyntaxKind.Invalid;
        tokenInfo.Position = _position;

        if (IsAtEnd)
            tokenInfo.Kind = SyntaxKind.TokenEndOfFile;
        else switch (CurrentCharacter)
        {
            case '_':
            case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'):
            {
                ReadIdentifier(ref tokenInfo);
                if (_keywordTokensKinds.TryGetValue(tokenInfo.TextValue, out var keywordKind))
                    tokenInfo.Kind = keywordKind;
            } break;

            case >= '0' and <= '9':
            {
                switch (ScanNumber())
                {
                    default: throw new UnreachableException();
                    case ScanNumberFlags.NotNumber: ReadIdentifier(ref tokenInfo); break;
                    case ScanNumberFlags.VanillaInteger: ReadInteger(ref tokenInfo); break;
                    case ScanNumberFlags.RadixInteger: ReadIntegerWithRadix(ref tokenInfo); break;
                    case ScanNumberFlags.VanillaFloat: ReadFloat(ref tokenInfo); break;
                    case ScanNumberFlags.RadixFloat: ReadFloatWithRadix(ref tokenInfo); break;
                }
            } break;

            case '@' when Peek(1) == '"':
            {
                Advance();
                ReadString(ref tokenInfo);
                tokenInfo.Kind = SyntaxKind.TokenIdentifier;
            } break;

            case '@' when Peek(1) is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_':
            {
                Advance();
                // we just don't transform it into a keyword here
                ReadIdentifier(ref tokenInfo);
            } break;

            case '@' when SyntaxFacts.IsIdentifierStartCharacter(Peek(1)):
            {
                Advance();
                // we just don't transform it into a keyword here
                ReadIdentifier(ref tokenInfo);
            } break;

            case '"': ReadString(ref tokenInfo); break;
            case '\'': ReadRune(ref tokenInfo); break;

            case '(': Advance(); tokenInfo.Kind = SyntaxKind.TokenOpenParen; break;
            case ')': Advance(); tokenInfo.Kind = SyntaxKind.TokenCloseParen; break;
            case '[': Advance(); tokenInfo.Kind = SyntaxKind.TokenOpenBracket; break;
            case ']': Advance(); tokenInfo.Kind = SyntaxKind.TokenCloseBracket; break;
            case '{': Advance(); tokenInfo.Kind = SyntaxKind.TokenOpenBrace; break;
            case '}': Advance(); tokenInfo.Kind = SyntaxKind.TokenCloseBrace; break;
            case ';': Advance(); tokenInfo.Kind = SyntaxKind.TokenSemiColon; break;
            case ',': Advance(); tokenInfo.Kind = SyntaxKind.TokenComma; break;
            case '.': Advance(); tokenInfo.Kind = SyntaxKind.TokenDot; break;

            case '~':
            {
                Advance();
                tokenInfo.Kind = TryAdvance('=') ? SyntaxKind.TokenTildeEqual : SyntaxKind.TokenTilde;
            } break;

            case '!':
            {
                Advance();
                tokenInfo.Kind = TryAdvance('=') ? SyntaxKind.TokenBangEqual : SyntaxKind.TokenBang;
            } break;

            case '%':
            {
                Advance();
                tokenInfo.Kind
                    = TryAdvance('=') ? SyntaxKind.TokenPercentEqual
                    : TryAdvance(':')
                        ? TryAdvance('=') ? SyntaxKind.TokenPercentColonEqual : SyntaxKind.TokenPercentColon
                    : SyntaxKind.TokenPercent;
            } break;

            case '&':
            {
                Advance();
                tokenInfo.Kind = TryAdvance('=') ? SyntaxKind.TokenAmpersandEqual : SyntaxKind.TokenAmpersand;
            } break;

            case '*':
            {
                Advance();
                tokenInfo.Kind = TryAdvance('=') ? SyntaxKind.TokenStarEqual : SyntaxKind.TokenStar;
            } break;

            case '-':
            {
                Advance();
                tokenInfo.Kind
                    = TryAdvance('=') ? SyntaxKind.TokenMinusEqual
                    : TryAdvance('-') ? SyntaxKind.TokenMinusMinus
                    : TryAdvance('|')
                        ? TryAdvance('=') ? SyntaxKind.TokenMinusPipeEqual : SyntaxKind.TokenMinusPipe
                    : TryAdvance('%')
                        ? TryAdvance('=') ? SyntaxKind.TokenMinusPercentEqual : SyntaxKind.TokenMinusPercent
                    : SyntaxKind.TokenMinus;
            } break;

            case '=':
            {
                Advance();
                tokenInfo.Kind = TryAdvance('=') ? SyntaxKind.TokenEqualEqual
                               : TryAdvance('>') ? SyntaxKind.TokenEqualGreater : SyntaxKind.TokenEqual;
            } break;

            case '+':
            {
                Advance();
                tokenInfo.Kind
                    = TryAdvance('=') ? SyntaxKind.TokenPlusEqual
                    : TryAdvance('+') ? SyntaxKind.TokenPlusPlus
                    : TryAdvance('|')
                        ? TryAdvance('=') ? SyntaxKind.TokenPlusPipeEqual : SyntaxKind.TokenPlusPipe
                    : TryAdvance('%')
                        ? TryAdvance('=') ? SyntaxKind.TokenPlusPercentEqual : SyntaxKind.TokenPlusPercent
                    : SyntaxKind.TokenPlus;
            } break;

            case '|':
            {
                Advance();
                tokenInfo.Kind = TryAdvance('=') ? SyntaxKind.TokenPipeEqual : SyntaxKind.TokenPipe;
            } break;

            case ':':
            {
                Advance();
                tokenInfo.Kind
                    = TryAdvance(':') ? SyntaxKind.TokenColonColon
                    : TryAdvance('>')
                        ? TryAdvance('=') ? SyntaxKind.TokenColonGreaterEqual : SyntaxKind.TokenColonGreater
                    : SyntaxKind.TokenColon;
            } break;

            case '<':
            {
                Advance();
                tokenInfo.Kind
                    = TryAdvance(':') ? SyntaxKind.TokenLessColon
                    : TryAdvance('<')
                        ? TryAdvance('=') ? SyntaxKind.TokenLessLessEqual : SyntaxKind.TokenLessLess
                    : TryAdvance('=')
                        ? TryAdvance(':') ? SyntaxKind.TokenLessEqualColon : SyntaxKind.TokenLessEqual
                    : SyntaxKind.TokenLess;
            } break;

            case '>':
            {
                Advance();
                tokenInfo.Kind
                    = TryAdvance('=') ? SyntaxKind.TokenGreaterEqual
                    : TryAdvance('>')
                        ? TryAdvance('>')
                            ? TryAdvance('=') ? SyntaxKind.TokenGreaterGreaterGreaterEqual : SyntaxKind.TokenGreaterGreaterGreater
                        : TryAdvance('=') ? SyntaxKind.TokenGreaterGreaterEqual : SyntaxKind.TokenGreaterGreater
                    : SyntaxKind.TokenGreater;
            } break;

            case '/':
            {
                Advance();
                tokenInfo.Kind
                    = TryAdvance('=') ? SyntaxKind.TokenSlashEqual
                    : TryAdvance(':')
                        ? TryAdvance('=') ? SyntaxKind.TokenSlashColonEqual : SyntaxKind.TokenSlashColon
                    : SyntaxKind.TokenSlash;
            } break;

            case '?':
            {
                Advance();
                tokenInfo.Kind
                    = TryAdvance('?')
                        ? TryAdvance('=') ? SyntaxKind.TokenQuestionQuestionEqual : SyntaxKind.TokenQuestionQuestion
                    : SyntaxKind.TokenQuestion;
            } break;

            case '^':
            {
                Advance();
                tokenInfo.Kind = TryAdvance('=') ? SyntaxKind.TokenCaretEqual : SyntaxKind.TokenCaret;
            } break;

            default:
            {
                Debug.Assert(!IsAtEnd);
                Debug.Assert(CurrentCharacter != 0);

                if (SyntaxFacts.IsIdentifierStartCharacter(CurrentCharacter))
                    ReadIdentifier(ref tokenInfo);
                else
                {
                    tokenInfo.Kind = SyntaxKind.Invalid;
                    Context.Diag.Error(CurrentLocation, $"invalid character '{CurrentCharacter}' in Laye source");
                    Advance();
                }
            } break;
        }

        tokenInfo.Length = Math.Max(1, _position - tokenInfo.Position);
        SkipTrivia();
    }

    private void SkipTrivia()
    {
        while (!IsAtEnd)
        {
            while (SyntaxFacts.IsWhiteSpaceCharacter(CurrentCharacter))
                Advance();

            switch (CurrentCharacter)
            {
                default: return;

                case '/' when Peek(1) == '/':
                    {
                        Advance();
                        Advance();
                        while (!IsAtEnd && CurrentCharacter != '\n')
                            Advance();
                    }
                    break;

                case '/' when Peek(1) == '*':
                    {
                        var location = CurrentLocation;

                        Advance();
                        Advance();

                        int nesting = 1;
                        while (!IsAtEnd && nesting > 0)
                        {
                            if (CurrentCharacter == '/' && Peek(1) == '*')
                            {
                                Advance();
                                Advance();
                                nesting++;
                            }
                            else if (CurrentCharacter == '*' && Peek(1) == '/')
                            {
                                Advance();
                                Advance();
                                nesting--;
                            }
                            else Advance();
                        }

                        if (nesting > 0)
                            Context.Diag.Error(location, "comment unclosed at end of file");
                    }
                    break;
            }
        }
    }

    private void ReadIdentifier(ref TokenInfo tokenInfo)
    {
        Debug.Assert(SyntaxFacts.IsIdentifierStartCharacter(CurrentCharacter));

        tokenInfo.Kind = SyntaxKind.TokenIdentifier;
        while (!IsAtEnd)
        {
            if (CurrentCharacter is '_' or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
                Advance();
            else
            {
                if (SyntaxFacts.IsIdentifierPartCharacter(CurrentCharacter))
                    Advance();
                else break;
            }
        }

        tokenInfo.TextValue = _text.Substring(tokenInfo.Position, _position - tokenInfo.Position);
    }

    private void ReadInteger(ref TokenInfo tokenInfo)
    {
        Debug.Assert(CurrentCharacter is >= '0' and <= '9');

        // when reading *just* an integer, we should be able to assert that
        // there are no identifier characters within or at its end.
        // the scanning process would trivially discard this case.
        
        tokenInfo.Kind = SyntaxKind.TokenLiteralInteger;

        _stringBuilder.Clear();
        while (!IsAtEnd && CurrentCharacter is (>= '0' and <= '9') or '_')
        {
            if (CurrentCharacter != '_')
                _stringBuilder.Append(CurrentCharacter);
            Advance();
        }

        Debug.Assert(CurrentCharacter is not ((>= '0' and <= '9') or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_') && !SyntaxFacts.IsIdentifierPartCharacter(CurrentCharacter));
        
        tokenInfo.IntegerValue = BigInteger.Parse(_stringBuilder.ToString());
    }

    private void ReadIntegerWithRadix(ref TokenInfo tokenInfo)
    {
        Debug.Assert(CurrentCharacter is >= '0' and <= '9');

        var startLocation = CurrentLocation;
        tokenInfo.Kind = SyntaxKind.TokenLiteralInteger;

        int radix;
        if (Peek(1) == '#')
        {
            radix = CurrentCharacter - '0';
            if (radix < 2)
            {
                Context.Diag.Error(startLocation, "integer base must be in the range [2, 36]");
                radix = 2;
            }

            Advance();
        }
        else if (Peek(1) is >= '0' and <= '9' && Peek(2) == '#')
        {
            radix = 10 * (CurrentCharacter - '0') + (Peek(1) - '0');
            if (radix > 36)
            {
                Context.Diag.Error(startLocation, "integer base must be in the range [2, 36]");
                radix = 36;
            }

            Advance();
            Advance();
        }
        else
        {
            Context.Diag.Error(startLocation, "integer base must be in the range [2, 36]");
            radix = 36;
            
            bool reportedIllegalUnderscore = false;
            while (CurrentCharacter != '#')
            {
                if (CurrentCharacter == '_' && !reportedIllegalUnderscore)
                {
                    Context.Diag.Error(CurrentLocation, "integer base must not contain '_' separators");
                    reportedIllegalUnderscore = true;
                }

                Advance();
            }
        }

        Debug.Assert(!IsAtEnd && CurrentCharacter == '#');
        Debug.Assert(radix is >= 2 and <= 36);
        Advance();

        _stringBuilder.Clear();
        
        bool wasLastCharacterUnderscore = false;
        bool parseResult = true;

        while (!IsAtEnd && CurrentCharacter is (>= '0' and <= '9') or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' || SyntaxFacts.IsIdentifierPartCharacter(CurrentCharacter))
        {
            if (CurrentCharacter == '_')
            {
                while (CurrentCharacter == '_') Advance();
                wasLastCharacterUnderscore = true;
                continue;
            }
            else if (SyntaxFacts.IsIdentifierPartCharacter(CurrentCharacter) && CurrentCharacter is not ((>= '0' and <= '9') or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_'))
            {
                Context.Diag.Error(CurrentLocation, $"invalid digit '{CurrentCharacter}' in integer literal");
                while (SyntaxFacts.IsIdentifierPartCharacter(CurrentCharacter)) Advance();
                return;
            }

            if (!SyntaxFacts.IsNumericLiteralDigitInRadix(CurrentCharacter, radix))
            {
                Context.Diag.Error(CurrentLocation, $"invalid digit '{CurrentCharacter}' in base {radix} integer literal");
                parseResult = false;
                _stringBuilder.Clear();
            }
            
            wasLastCharacterUnderscore = false;
            if (parseResult) _stringBuilder.Append(CurrentCharacter);
            Advance();
        }

        if (wasLastCharacterUnderscore)
            Context.Diag.Error(startLocation, "integer literal cannot end with an '_' separator");

        if (parseResult)
        {
            Debug.Assert(_stringBuilder.Length > 0);
            tokenInfo.IntegerValue = ParseBigInteger(_stringBuilder.ToString(), radix);
        }
        else
        {
            Debug.Assert(_stringBuilder.Length == 0);
        }

        static BigInteger ParseBigInteger(string value, int radix)
        {
            if (radix == 16) return BigInteger.Parse("0" + value, System.Globalization.NumberStyles.HexNumber);
            var result = BigInteger.Zero;
            for (int i = 0; i < value.Length; i++)
                result = result * radix + SyntaxFacts.NumericLiteralDigitValueInRadix(value[i], radix);
            return result;
        }
    }

    private void ReadFloat(ref TokenInfo tokenInfo)
    {
        Context.Diag.Fatal("float literal tokens are currently not supported :(");
        throw new UnreachableException();
    }

    private void ReadFloatWithRadix(ref TokenInfo tokenInfo)
    {
        Context.Diag.Fatal("float literal tokens are currently not supported :(");
        throw new UnreachableException();
    }

    private int ReadEscapeSequenceAsSurrogatePair(out char highSurrogate, out char lowSurrogate)
    {
        highSurrogate = '\0';
        lowSurrogate = '\0';

        Debug.Assert(CurrentCharacter == '\\');

        var startLocation = CurrentLocation;
        Advance();

        switch (CurrentCharacter)
        {
            case 'a': Advance(); lowSurrogate = '\a'; return 1;
            case 'b': Advance(); lowSurrogate = '\b'; return 1;
            case 'f': Advance(); lowSurrogate = '\f'; return 1;
            case 'n': Advance(); lowSurrogate = '\n'; return 1;
            case 'r': Advance(); lowSurrogate = '\r'; return 1;
            case 't': Advance(); lowSurrogate = '\t'; return 1;
            case 'v': Advance(); lowSurrogate = '\v'; return 1;
            case '\\': Advance(); lowSurrogate = '\\'; return 1;
            case '\'': Advance(); lowSurrogate = '\''; return 1;
            case '"': Advance(); lowSurrogate = '"'; return 1;

            case 'x' when SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(1), 16)
                       && SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(2), 16):
            {
                Advance();
                lowSurrogate = (char)int.Parse(_text.AsSpan().Slice(_position, 2), System.Globalization.NumberStyles.HexNumber);
                for (int i = 0; i < 2; i++) Advance();
                return 1;
            }

            case 'u' when SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(1), 16)
                       && SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(2), 16)
                       && SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(3), 16)
                       && SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(4), 16):
            {
                Advance();
                lowSurrogate = (char)int.Parse(_text.AsSpan().Slice(_position, 4), System.Globalization.NumberStyles.HexNumber);
                for (int i = 0; i < 4; i++) Advance();
                return 1;
            }

            case 'U' when SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(1), 16)
                       && SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(2), 16)
                       && SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(3), 16)
                       && SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(4), 16)
                       && SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(5), 16)
                       && SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(6), 16)
                       && SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(7), 16)
                       && SyntaxFacts.IsNumericLiteralDigitInRadix(Peek(8), 16):
            {
                Advance();
                int value = int.Parse(_text.AsSpan().Slice(_position, 8), System.Globalization.NumberStyles.HexNumber);
                unsafe
                {
                    char* pair = stackalloc char[2];
                    new Rune(value).EncodeToUtf16(new Span<char>(pair, 2));
                    highSurrogate = pair[0];
                    lowSurrogate = pair[1];
                }
                for (int i = 0; i < 8; i++) Advance();
                return 2;
            }

            default:
            {
                Context.Diag.Error(startLocation, "unrecognized escape sequence");
                lowSurrogate = CurrentCharacter;
                Advance();
                return 1;
            }
        }
    }

    private void ReadString(ref TokenInfo tokenInfo)
    {
        Debug.Assert(CurrentCharacter == '"');

        var startLocation = CurrentLocation;
        tokenInfo.Kind = SyntaxKind.TokenLiteralString;
        Advance();

        _stringBuilder.Clear();
        while (!IsAtEnd && CurrentCharacter != '"' && CurrentCharacter != '\n')
        {
            if (CurrentCharacter == '\\')
            {
                int nchars = ReadEscapeSequenceAsSurrogatePair(out char higher, out char lower);
                Debug.Assert(nchars == 1 || nchars == 2);
                _stringBuilder.Append(lower);
                if (nchars == 2) _stringBuilder.Append(higher);
            }
            else
            {
                _stringBuilder.Append(CurrentCharacter);
                Advance();
            }
        }

        tokenInfo.TextValue = _stringBuilder.ToString();
        if (TryAdvance('"')) return;

        if (IsAtEnd)
        {
            Context.Diag.Error(startLocation, "end of file reached in string literal");
            return;
        }

        if (CurrentCharacter != '"')
        {
            Context.Diag.Error(startLocation, "newline in string literal");
            return;
        }
    }

    private void ReadRune(ref TokenInfo tokenInfo)
    {
        Debug.Assert(CurrentCharacter == '\'');
        
        var startLocation = CurrentLocation;
        tokenInfo.Kind = SyntaxKind.TokenLiteralRune;
        Advance();
        
        if (CurrentCharacter == '\\')
        {
            int nchars = ReadEscapeSequenceAsSurrogatePair(out char higher, out char lower);
            tokenInfo.IntegerValue = new BigInteger(nchars == 1 ? lower : char.ConvertToUtf32(higher, lower));
        }
        else
        {
            tokenInfo.IntegerValue = new BigInteger(CurrentCharacter);
            Advance();
        }
        
        if (TryAdvance('\'')) return;

        if (IsAtEnd)
        {
            Context.Diag.Error(startLocation, "end of file reached in rune literal");
            return;
        }

        if (CurrentCharacter != '\'')
        {
            while (!IsAtEnd && CurrentCharacter != '\n') Advance();
            Context.Diag.Error(startLocation, "too many characters in rune literal");
            return;
        }
    }

    private ScanNumberFlags ScanNumber()
    {
        Debug.Assert(SyntaxFacts.IsNumericLiteralDigit(CurrentCharacter));

        using var resetPoint = new LexerResetPoint(this);
        while (!IsAtEnd)
        {
            switch (CurrentCharacter)
            {
                case >= '0' and <= '9': Advance(); break;
                case '_' when IsSequenceOfUnderscoresFollowedByDigit():
                {
                    while (CurrentCharacter == '_') Advance();
                    Advance(); // the digit
                } break;

                // an underscore with no following digit, or any letters, means this is
                // definitively not going to be a number.
                case '_':
                case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'):
                    return ScanNumberFlags.NotNumber;

                case '.': return ScanNumberFlags.VanillaFloat;
                case '#': return ScanNumberWithRadix();
                default: return SyntaxFacts.IsIdentifierPartCharacter(CurrentCharacter) ? ScanNumberFlags.NotNumber : ScanNumberFlags.VanillaInteger;
            }
        }

        return ScanNumberFlags.VanillaInteger;

        bool IsSequenceOfUnderscoresFollowedByDigit()
        {
            for (int i = 0; i < 10; i++)
            {
                if (Peek(i) != '_') return false;
                if (Peek(i + 1) is >= '0' and <= '9') return true;
            }

            return false;
        }

        ScanNumberFlags ScanNumberWithRadix()
        {
            Debug.Assert(CurrentCharacter == '#');
            Advance();

            while (!IsAtEnd)
            {
                // at this point we're beyond the point of no return for identifier fallbacks
                if (CurrentCharacter is (>= '0' and <= '9') or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') || SyntaxFacts.IsIdentifierPartCharacter(CurrentCharacter))
                    Advance();
                else if (CurrentCharacter == '.')
                    return ScanNumberFlags.RadixFloat;
                else break;
            }

            return ScanNumberFlags.RadixInteger;
        }
    }
}
