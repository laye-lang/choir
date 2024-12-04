namespace Choir.Front.Laye.Syntax;

public class SyntaxPrinter : BaseTreePrinter<SyntaxNode>
{
    private readonly ScopePrinter _scopePrinter;
    private readonly bool _printScopes;

    public ChoirContext Context { get; }

    public SyntaxPrinter(ChoirContext context, bool printScopes)
        : base(context.UseColor)
    {
        _scopePrinter = new(context.UseColor);
        _printScopes = printScopes;

        Context = context;
        ColorBase = CommandLine.Color.Green;
    }

    public void PrintToken(SyntaxToken token)
    {
        Print(token);
    }

    public void PrintSyntax(SyntaxDeclModuleUnit unitSyntax)
    {
        Print(unitSyntax);
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

            case SyntaxTypeBuiltIn builtin:
            {
                Console.Write(builtin.Type.ToDebugString(C));
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
