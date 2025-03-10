namespace Choir.Formatting;

[Flags]
public enum MarkupStyle
    : byte
{
    None = 0,
    Bold = 1 << 0,
    Italic = 1 << 1,
    Underline = 1 << 2,
    Monospace = 1 << 3,
}
