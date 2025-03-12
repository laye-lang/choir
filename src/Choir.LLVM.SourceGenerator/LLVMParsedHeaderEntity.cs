namespace Choir.LibLLVM.SourceGenerator;

public abstract class LLVMParsedHeaderEntity(int lineNumber, string[] docs)
{
    public int LineNumber { get; } = lineNumber;
    public string[] Docs { get; } = docs;
}

public sealed class LLVMParsedGroupEmpty(int lineNumber, string[] docs, string description)
    : LLVMParsedHeaderEntity(lineNumber, docs)
{
    public string Description { get; } = description;
}

public sealed class LLVMParsedGroupBegin(int lineNumber, string[] docs, string description)
    : LLVMParsedHeaderEntity(lineNumber, docs)
{
    public string Description { get; } = description;
}

public sealed class LLVMParsedGroupEnd(int lineNumber)
    : LLVMParsedHeaderEntity(lineNumber, [])
{
}

public sealed class LLVMParsedEnum(int lineNumber, string[] docs, string name, (string, string?, string[])[] variants)
    : LLVMParsedHeaderEntity(lineNumber, docs)
{
    public string Name { get; } = name;
    public (string Name, string? Value, string[] Docs)[] Variants { get; } = variants;
}

public sealed class LLVMParsedTypedef(int lineNumber, string[] docs, string name)
    : LLVMParsedHeaderEntity(lineNumber, docs)
{
    public string Name { get; } = name;
}
