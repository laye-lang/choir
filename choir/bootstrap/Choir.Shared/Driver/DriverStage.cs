namespace Choir.Driver;

public enum DriverStage
{
    Preprocess,
    Lex,
    Parse,
    Sema,
    Codegen,
    Compile,
    Assemble,
    Link,
}
