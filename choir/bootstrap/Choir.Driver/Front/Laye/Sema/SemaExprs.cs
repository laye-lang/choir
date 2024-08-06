namespace Choir.Front.Laye.Sema;

public enum CastKind
{
    Invalid,
    Dependent,
    BitCast,
    LValueBitCast,
    LValueToRValueBitCast,
    LValueToRValue,
    NoOp,
}
