﻿using Choir.Source;

namespace Choir.FrontEnd.Score.Syntax;

public sealed class ScoreSyntaxPrinter(SourceText source, bool useColor)
    : BaseTreePrinter<ScoreSyntaxNode>(useColor)
{
    public void PrintTokens(IEnumerable<ScoreToken> tokens)
    {
        SetColor(ColorBase);
        Console.WriteLine("Tokens");
        PrintChildren(tokens);
    }

    public void PrintSyntaxUnit(ScoreSyntaxUnit unit)
    {
        SetColor(ColorMisc);
        Console.WriteLine($"// Score Syntax Unit \"{unit.Source.Name}\"");
        Print(unit);
    }

    protected override void Print(ScoreSyntaxNode node)
    {
        SetColor(ColorBase);
        Console.Write($"{node.GetType().Name} ");

        if (node is ScoreToken token)
            PrintToken(token);
        else if (node is ScoreTriviaList triviaList)
            PrintTriviaList(triviaList);
        else if (node is ScoreTrivia trivia)
            PrintTrivia(trivia);

        Console.ResetColor();
        Console.WriteLine();

        PrintChildren(node.Children);
    }

    private void PrintToken(ScoreToken token)
    {
        SetColor(ColorProperty);
        Console.Write(token.Kind);

        if (token.Range.Length <= 64)
        {
            string image = source.GetTextInRange(token.Range);
            if (!image.Contains('\r') && !image.Contains('\n'))
            {
                SetColor(ColorMisc);
                Console.Write(' ');
                Console.Write(image);
            }
        }
    }

    private void PrintTrivia(ScoreTrivia trivia)
    {
        if (trivia.Range.Length <= 64)
        {
            string image = source.GetTextInRange(trivia.Range);
            if (!image.Contains('\r') && !image.Contains('\n'))
            {
                SetColor(ColorMisc);
                if (string.IsNullOrWhiteSpace(image))
                    Console.Write($"'{image}'");
                else Console.Write(image);
            }
        }
    }

    private void PrintTriviaList(ScoreTriviaList triviaList)
    {
        SetColor(ColorProperty);
        Console.Write(triviaList.IsLeading ? "Leading" : "Trailing");
    }
}
