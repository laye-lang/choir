using System.Globalization;

namespace Choir.FrontEnd.Score.Syntax;

public static class ScoreSyntaxFacts
{
    public const int PrimitiveTypeKeywordLowerBoundInclusive = 1;
    public const int PrimitiveTypeKeywordUpperBoundExclusive = 65536;

    public static bool CanStartIdentifier(char c)
    {
        if (c is '_' or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'))
            return true;

        var category = CharUnicodeInfo.GetUnicodeCategory(c);
        return category is UnicodeCategory.UppercaseLetter
            or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter;
    }

    public static bool CanContinueIdentifier(char c)
    {
        if (c is '_' or (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
            return true;

        var category = CharUnicodeInfo.GetUnicodeCategory(c);
        return category is UnicodeCategory.UppercaseLetter
            or UnicodeCategory.LowercaseLetter
            or UnicodeCategory.TitlecaseLetter
            or UnicodeCategory.ModifierLetter
            or UnicodeCategory.OtherLetter
            or UnicodeCategory.LetterNumber
            or UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.DecimalDigitNumber
            or UnicodeCategory.ConnectorPunctuation
            or UnicodeCategory.Format;
    }

    public static bool IsValidIdentifier(string s)
    {
        if (s.Length == 0)
            return false;

        if (!CanStartIdentifier(s[0]))
            return false;

        for (int i = 1; i < s.Length; i++)
        {
            if (!CanContinueIdentifier(s[i]))
                return false;
        }

        return true;
    }
}
