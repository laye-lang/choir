namespace Choir.FrontEnd.Score.Syntax;

[Flags]
public enum LayeTypeQualifier
    : byte
{
    None = 0,
    Mutable = 1 << 0,
}
