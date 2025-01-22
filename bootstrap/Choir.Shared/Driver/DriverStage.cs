namespace Choir.Driver;

public enum DriverStage
{
    Preprocess,
    Lex,
    Parse,
    Sema,
    /// <summary>
    /// Generates code (in-memory) from the results of semantic analysis.
    /// </summary>
    Codegen,
    /// <summary>
    /// The first stage which will have a file output by default.
    /// Compiling takes the generated code and emits it to a file of the appropriate output assembly format.
    /// </summary>
    Compile,
    /// <summary>
    /// Assembles the result of the previous compilation step into (an) object file(s).
    /// </summary>
    Assemble,
    /// <summary>
    /// Links the resulting object files, along with any dependencies, into an executable.
    /// </summary>
    Link,
}
