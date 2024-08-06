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

    public void PrintModuleHeader(Module module)
    {
        Console.WriteLine($"{C[ColorMisc]}// Laye Module '{module.SourceFile.FileInfo.FullName}'");
    }

    public void PrintModuleTokens(Module module)
    {
        PrintModuleHeader(module);
        foreach (var token in module.Tokens)
            Print(token);
    }
    
    public void PrintModuleSyntax(Module module)
    {
        PrintModuleHeader(module);
        foreach (var node in module.TopLevelSyntax)
            Print(node);
    }

    protected virtual void PrintSyntaxNodeHeader(SyntaxNode node)
    {
        if (node is SyntaxToken token)
            Console.Write($"{C[ColorBase]}Token {token.Kind} {C[ColorLocation]}<{node.Location.Offset}> ");
        else Console.Write($"{C[ColorBase]}{node.GetType().Name} {C[ColorLocation]}<{node.Location.Offset}> ");
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

                    case TokenKind.Identifier:
                    {
                        Console.Write($"{C[ColorName]}{token.TextValue}");
                    } break;

                    case TokenKind.LiteralString:
                    {
                        Console.Write($"{C[ColorValue]}\"{token.TextValue}\"");
                    } break;

                    case TokenKind.LiteralRune:
                    {
                        Console.Write($"{C[ColorValue]}\'{char.ConvertFromUtf32((int)token.IntegerValue)}\'");
                    } break;

                    case TokenKind.LiteralInteger:
                    {
                        Console.Write($"{C[ColorValue]}{token.IntegerValue}");
                    } break;
                }
            } break;

            case SyntaxNameref nameref:
            {
                Console.Write($"{C[ColorBase]}{nameref.NamerefKind}");
            } break;
        }
        
        Console.WriteLine(C.Reset);
        PrintChildren(node.Children);
    }
}
