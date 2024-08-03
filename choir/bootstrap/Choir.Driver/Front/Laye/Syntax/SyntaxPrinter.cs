namespace Choir.Front.Laye.Syntax;

public class SyntaxPrinter : BaseTreePrinter<SyntaxNode>
{
    public ChoirContext Context { get; }

    public SyntaxPrinter(ChoirContext context)
        : base(context.UseColor)
    {
        Context = context;
        ColorBase = CommandLine.Color.Green;
    }

    public void PrintModuleHeader(SyntaxModule module)
    {
        Console.WriteLine($"{C[ColorMisc]}// Laye Module '{module.SourceFile.FileInfo.FullName}'");
    }

    public void PrintModuleTokens(SyntaxModule module)
    {
        PrintModuleHeader(module);
        foreach (var token in module.Tokens)
            Print(token);
    }
    
    public void PrintModuleSyntax(SyntaxModule module)
    {
        PrintModuleHeader(module);
        foreach (var node in module.TopLevelNodes)
            Print(node);
    }

    protected virtual void PrintSyntaxNodeHeader(SyntaxNode node)
    {
        Console.Write($"{C[ColorBase]}{node.Kind} {C[ColorLocation]}<{node.Location.Offset}> ");
    }

    protected override void Print(SyntaxNode node)
    {
        PrintSyntaxNodeHeader(node);
        switch (node)
        {
            default: break;
            case SyntaxToken token:
            {
                switch (token.Kind)
                {
                    default: Console.Write($"{C[ColorBase]}{token.Location.Span(Context)}"); break;

                    case SyntaxKind.TokenIdentifier:
                    {
                        Console.Write($"{C[ColorName]}{token.TextValue}");
                    } break;

                    case SyntaxKind.TokenLiteralString:
                    {
                        Console.Write($"{C[ColorValue]}\"{token.TextValue}\"");
                    } break;

                    case SyntaxKind.TokenLiteralRune:
                    {
                        Console.Write($"{C[ColorValue]}");
                        if (token.IntegerValue < 256)
                            Console.Write($"\'{(char)token.IntegerValue}\'");
                        else Console.Write($"\'\\U{token.IntegerValue:X8}\'");
                    } break;

                    case SyntaxKind.TokenLiteralInteger:
                    {
                        Console.Write($"{C[ColorValue]}{token.IntegerValue}");
                    } break;
                }
            } break;
        }
        
        Console.WriteLine(C.Reset);
    }
}
