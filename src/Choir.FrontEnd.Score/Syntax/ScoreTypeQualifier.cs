namespace Choir.FrontEnd.Score.Syntax;

[Flags]
public enum ScoreTypeQualifier
    : byte
{
    None = 0,
    Readonly = 1 << 0,
    Writeonly = 1 << 1,
}
