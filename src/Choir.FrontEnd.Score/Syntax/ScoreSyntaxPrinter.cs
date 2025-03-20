namespace Choir.FrontEnd.Score.Syntax;

public sealed class ScoreSyntaxPrinter(bool useColor)
    : BaseTreePrinter<ScoreSyntaxNode>(useColor)
{
    public void PrintTokens(IEnumerable<ScoreToken> tokens)
    {
        SetColor(ColorBase);
        Console.WriteLine("Tokens");
        PrintChildren(tokens);
    }

    protected override void Print(ScoreSyntaxNode node)
    {
        Console.Write($"{node.GetType().Name} ");

        if (node is ScoreToken token)
            PrintToken(token);
        else if (node is ScoreTriviaList triviaList)
            PrintTriviaList(triviaList);

        Console.ResetColor();
        Console.WriteLine();

        PrintChildren(node.Children);
    }

    private void PrintToken(ScoreToken token)
    {
        SetColor(ColorProperty);
        Console.Write(token.Kind);
    }

    private void PrintTriviaList(ScoreTriviaList triviaList)
    {
        SetColor(ColorProperty);
        Console.Write(triviaList.IsLeading ? "Leading" : "Trailing");
    }
}
