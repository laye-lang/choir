// This file is licensed to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Choir.Front.Laye.Syntax;

public static class SyntaxFacts
{
    public static bool IsIdentifierStartCharacter(char c)
    {
        return c == '_' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
    }
    
    public static bool IsIdentifierPartCharacter(char c)
    {
        return IsIdentifierStartCharacter(c);
    }

    public static bool IsWhiteSpaceCharacter(char c)
    {
        return c == ' ' || c == '\r' || c == '\n' || c == '\v' || c == '\t';
    }

    public static bool IsNumericLiteralDigit(char c)
    {
        return c >= '0' && c <= '9';
    }

    public static int NumericLiteralDigitValue(char c)
    {
        if (!IsNumericLiteralDigit(c))
            throw new ArgumentException($"{nameof(NumericLiteralDigitValue)} can only accept a character/c that is a valid numeric literal digit.", nameof(c));

        return c - '0';
    }

    public static bool IsNumericLiteralDigitInRadix(char c, int radix)
    {
        if (radix < 2 || radix > 36)
            throw new ArgumentException($"{nameof(IsNumericLiteralDigit)} can only accept a radix in the range [2, 36].", nameof(radix));

        if (radix <= 10)
            return c >= '0' && c <= ('0' + radix);
        
        return (c >= '0' && c <= '9') || (c >= 'a' && c <= 'a' + (radix - 10)) || (c >= 'A' && c <= 'A' + (radix - 10));
    }

    public static int NumericLiteralDigitValueInRadix(char c, int radix)
    {
        if (radix < 2 || radix > 36)
            throw new ArgumentException($"{nameof(NumericLiteralDigitValueInRadix)} can only accept a radix in the range [2, 36].", nameof(radix));

        if (!IsNumericLiteralDigitInRadix(c, radix))
            throw new ArgumentException($"{nameof(NumericLiteralDigitValue)} can only accept a character/c that is a valid numeric literal digit in base {radix}.", nameof(c));

        if (c >= '0' && c <= '9')
            return c - '0';
            
        if (c >= 'a' && c <= 'z')
            return c - 'a';

        Debug.Assert(c >= 'A' && c <= 'Z');
        return c - 'A';
    }
}
