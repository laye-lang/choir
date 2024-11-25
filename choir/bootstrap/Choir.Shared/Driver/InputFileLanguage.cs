namespace Choir.Driver;

[Flags]
public enum InputFileLanguage
{
    Default = 0,

    LayeSource = 1 << 0,
    LayeModule = 1 << 1,
    Choir = 1 << 2,
    C = 1 << 3,
    CXX = 1 << 4,
    ObjC = 1 << 5,
    ObjCXX = 1 << 6,
    Assembler = 1 << 7,
    Object = 1 << 8,

    Header = 1 << 24,
    NoPreprocess = 1 << 25,
}
