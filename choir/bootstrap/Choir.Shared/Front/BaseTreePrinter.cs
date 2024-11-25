using System.Text;

using Choir.CommandLine;

namespace Choir.Front;

public abstract class BaseTreePrinter<TNode>(bool useColor)
{
    protected readonly StringBuilder _leadingText = new(128);
    protected readonly Colors C = new(useColor);

    protected Color ColorBase = Color.White;
    protected Color ColorMisc = Color.Grey;
    protected Color ColorLocation = Color.Magenta;
    protected Color ColorName = Color.White;
    protected Color ColorValue = Color.Yellow;
    protected Color ColorKeyword = Color.Cyan;

    protected abstract void Print(TNode node);
    protected virtual void PrintChildren(IEnumerable<TNode> children)
    {
        if (!children.Any()) return;

        int leadingLength = _leadingText.Length;
        string currentLeading = _leadingText.ToString();

        _leadingText.Append("│ ");
        foreach (var child in children.Take(children.Count() - 1))
        {
            Console.Write($"{C[ColorBase]}{currentLeading}├─");
            Print(child);
        }

        _leadingText.Length = leadingLength;
        Console.Write($"{C[ColorBase]}{_leadingText}└─");

        _leadingText.Append("  ");
        Print(children.Last());

        _leadingText.Length = leadingLength;
    }
}
