namespace Choir.LibLLVM.SourceGenerator;

public sealed class LLVMHeaderData
{
    public static LLVMHeaderData FromFile(string filePath)
    {
        using var reader = new StreamReader(File.OpenRead(filePath));
        return FromReader(reader);
    }

    public static LLVMHeaderData FromText(string text)
    {
        using var reader = new StringReader(text);
        return FromReader(reader);
    }

    public static LLVMHeaderData FromReader(TextReader reader)
    {
        var parser = new LLVMHeaderParser(reader);
        parser.SkipPreamble();

        var entities = new List<LLVMParsedHeaderEntity>();
        while (parser.ParseEntity() is { } parsedEntity)
        {
            entities.Add(parsedEntity);
            ;
        }

        throw new NotImplementedException();
    }
}
