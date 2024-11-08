namespace Choir.CommandLine;

public enum Color
{
    Reset,
    Default,
    Bold,
    Red,
    Green,
    Yellow,
    Blue,
    Magenta,
    Cyan,
    Grey,
    White,
}

public class Colors(bool useColor)
{
    public static readonly Colors On = new(true);
    public static readonly Colors Off = new(false);

    public string Reset { get; } = useColor ? "\x1b[0m" : "";
    public string Default { get; } = useColor ? "\x1b[39m" : "";
    public string Bold { get; } = useColor ? "\x1b[1m" : "";
    public string Red { get; } = useColor ? "\x1b[91m" : "";
    public string Green { get; } = useColor ? "\x1b[92m" : "";
    public string Yellow { get; } = useColor ? "\x1b[93m" : "";
    public string Blue { get; } = useColor ? "\x1b[94m" : "";
    public string Magenta { get; } = useColor ? "\x1b[95m" : "";
    public string Cyan { get; } = useColor ? "\x1b[96m" : "";
    public string Grey { get; } = useColor ? "\x1b[97m" : "";
    public string White { get; } = useColor ? "\x1b[1m\x1b[97m" : "";

    public string this[Color color] => Get(color);
    public string Get(Color color) => color switch
    {
        Color.Red => Red,
        Color.Default => Default,
        Color.Bold => Bold,
        Color.Green => Green,
        Color.Yellow => Yellow,
        Color.Blue => Blue,
        Color.Magenta => Magenta,
        Color.Cyan => Cyan,
        Color.Grey => Grey,
        Color.White => White,
        _ => Reset,
    };
}

public static class ColorsExtensions
{
    public static string ForDiagnostic(this Colors colors, DiagnosticKind kind) => kind switch
    {
        DiagnosticKind.Note => colors.Green,
        DiagnosticKind.Warning => colors.Magenta,
        DiagnosticKind.Error => colors.Red,
        DiagnosticKind.ICE => colors.Cyan,
        _ => colors.White,
    };
}
