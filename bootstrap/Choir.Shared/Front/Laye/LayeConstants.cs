namespace Choir.Front.Laye;

public static class LayeConstants
{
    public const string EntryFunctionName = "__laye_main";

    public const string ProgramModuleName = ".program";

    public const string OperatorNamePrefix = ".__laye_operator";

    public const string ModuleSectionNamePrefix = ".__laye_module_description";

    public static string GetModuleDescriptionSectionName(string? moduleName)
    {
        if (moduleName is null)
            return ModuleSectionNamePrefix;
        else return $"{ModuleSectionNamePrefix}.{moduleName}";
    }

#if false
    public static string GetOperatorName(SyntaxOperatorName operatorName)
    {
        switch (operatorName)
        {
            case SyntaxOperatorSimple simple: return simple.TokenOperator.Kind switch
                {
                    TokenKind.LessEqualGreater => $"{OperatorNamePrefix}.less_equal_greater",
                    TokenKind.EqualEqual => $"{OperatorNamePrefix}.equal_equal",
                    TokenKind.BangEqual => $"{OperatorNamePrefix}.bang_equal",
                    TokenKind.Less => $"{OperatorNamePrefix}.less",
                    TokenKind.LessEqual => $"{OperatorNamePrefix}.less_equal",
                    TokenKind.Greater => $"{OperatorNamePrefix}.greater",
                    TokenKind.GreaterEqual => $"{OperatorNamePrefix}.greater_equal",
                    TokenKind.LessColon => $"{OperatorNamePrefix}.less_colon",
                    TokenKind.LessEqualColon => $"{OperatorNamePrefix}.less_equal_colon",
                    TokenKind.ColonGreater => $"{OperatorNamePrefix}.colon_greater",
                    TokenKind.ColonGreaterEqual => $"{OperatorNamePrefix}.colon_greater_equal",
                    TokenKind.Ampersand => $"{OperatorNamePrefix}.ampersand",
                    TokenKind.Pipe => $"{OperatorNamePrefix}.pipe",
                    TokenKind.Tilde => $"{OperatorNamePrefix}.tilde",
                    TokenKind.LessLess => $"{OperatorNamePrefix}.less_less",
                    TokenKind.GreaterGreater => $"{OperatorNamePrefix}.greater_greater",
                    TokenKind.GreaterGreaterGreater => $"{OperatorNamePrefix}.greater_greater_greater",
                    TokenKind.Plus => $"{OperatorNamePrefix}.plus",
                    TokenKind.Minus => $"{OperatorNamePrefix}.minus",
                    TokenKind.PlusPipe => $"{OperatorNamePrefix}.plus_pipe",
                    TokenKind.MinusPipe => $"{OperatorNamePrefix}.minus_pipe",
                    TokenKind.PlusPercent => $"{OperatorNamePrefix}.plus_percent",
                    TokenKind.MinusPercent => $"{OperatorNamePrefix}.minus_percent",
                    TokenKind.Star => $"{OperatorNamePrefix}.star",
                    TokenKind.Slash => $"{OperatorNamePrefix}.slash",
                    TokenKind.Percent => $"{OperatorNamePrefix}.percent",
                    TokenKind.SlashColon => $"{OperatorNamePrefix}.slash_colon",
                    TokenKind.PercentColon => $"{OperatorNamePrefix}.percent_colon",
                    _ => throw new NotImplementedException(),
                };

            case SyntaxOperatorCast operatorCast:
            {
                var mangler = new LayeNameMangler();
            }
        }
    }
#endif
}
