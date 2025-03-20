﻿using System.Text;

namespace Choir;

public abstract class BaseTreePrinter<TNode>(bool useColor)
{
    protected readonly StringBuilder _leadingText = new(128);

    protected ConsoleColor ColorBase = ConsoleColor.Green;
    protected ConsoleColor ColorMisc = ConsoleColor.Gray;
    protected ConsoleColor ColorLocation = ConsoleColor.Magenta;
    protected ConsoleColor ColorName = ConsoleColor.White;
    protected ConsoleColor ColorProperty = ConsoleColor.Blue;
    protected ConsoleColor ColorValue = ConsoleColor.Yellow;
    protected ConsoleColor ColorKeyword = ConsoleColor.Cyan;

    protected void SetColor(ConsoleColor color)
    {
        if (useColor) Console.ForegroundColor = color;
    }

    protected abstract void Print(TNode node);

    protected virtual void PrintChildren(IEnumerable<TNode> children)
    {
        if (!children.Any()) return;

        int leadingLength = _leadingText.Length;
        string currentLeading = _leadingText.ToString();

        _leadingText.Append("│ ");
        foreach (var child in children.Take(children.Count() - 1))
        {
            SetColor(ColorBase);
            Console.Write($"{currentLeading}├─");
            Print(child);
        }

        _leadingText.Length = leadingLength;
        SetColor(ColorBase);
        Console.Write($"{_leadingText}└─");

        _leadingText.Append("  ");
        Print(children.Last());

        _leadingText.Length = leadingLength;
    }
}
