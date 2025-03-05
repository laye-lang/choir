namespace Choir.FrontEnd.Laye.Semantics;

[Flags]
public enum LayeTypeQualifier
    : byte
{
    None = 0,
    Mutable = 1 << 0,
}
