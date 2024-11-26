using System.Diagnostics;

using Choir.Front.Laye.Sema;

namespace Choir.Front.Laye;

internal static class SerializerConstants
{
    public const uint Magic = (uint)(byte)'l' | ((byte)'a' << 8) | ((byte)'y' << 16) | ((byte)'e' << 24);
}

public sealed class DeclarationSerializer
{
    public static byte[] SerializeToBytes(ChoirContext context, LayeModule module)
    {
        using var memoryStream = new MemoryStream();
        SerializeToStream(context, module, memoryStream);
        memoryStream.Flush();
        return memoryStream.GetBuffer();
    }

    public static void SerializeToStream(ChoirContext context, LayeModule module, Stream stream)
    {
        using var writer = new BinaryWriter(stream);
        var s = new DeclarationSerializer(context, module, writer);
        s.Serialize();
    }

    private readonly ChoirContext _context;
    private readonly LayeModule _module;
    private readonly BinaryWriter _writer;

    private DeclarationSerializer(ChoirContext context, LayeModule module, BinaryWriter writer)
    {
        _context = context;
        _module = module;
        _writer = writer;
    }

    private void Serialize()
    {
        _writer.Write(SerializerConstants.Magic);
        _writer.Write(_module.ModuleName ?? "");
        _writer.Write(_module.Dependencies.Count);
        foreach (var dependency in _module.Dependencies)
            _writer.Write(dependency.ModuleName ?? "");
    }

    private int SerializeType(SemaTypeQual typeQual)
    {
        var type = typeQual.Type; // .CanonicalType
        switch (type)
        {
            default:
            {
                _context.Assert(false, $"Unimplemented type in serializer: {type.GetType().FullName}");
                throw new UnreachableException();
            }
        }
    }

    private void SerializeDecl(SemaDeclNamed decl)
    {
        switch (decl)
        {
            default:
            {
                _context.Assert(false, $"Unimplemented decl in serializer: {decl.GetType().FullName}");
                throw new UnreachableException();
            }

            case SemaDeclFunction declFunction: SerializeDeclFunction(declFunction); break;
        }
    }

    private void SerializeDeclFunction(SemaDeclFunction declFunction)
    {
        throw new NotImplementedException();
    }
}

public sealed class DeclarationDeserializer
{
    public static (string? ModuleName, string[] DependencyNames) DeserializeHeaderFromStream(ChoirContext context, Stream stream)
    {
        using var reader = new BinaryReader(stream);
        var d = new DeclarationDeserializer(context, [], reader);
        return d.ReadModuleHeader();
    }

    public static LayeModule DeserializeFromStream(ChoirContext context, LayeModule[] dependencies, Stream stream)
    {
        using var reader = new BinaryReader(stream);
        var d = new DeclarationDeserializer(context, dependencies, reader);
        return d.Deserialize();
    }

    private readonly ChoirContext _context;
    private readonly LayeModule[] _dependencies;
    private readonly BinaryReader _reader;

    private DeclarationDeserializer(ChoirContext context, LayeModule[] dependencies, BinaryReader reader)
    {
        _context = context;
        _dependencies = dependencies;
        _reader = reader;
    }

    private (string? ModuleName, string[] DependencyNames) ReadModuleHeader()
    {
        uint magic = _reader.ReadUInt32();
        _context.Assert(magic == SerializerConstants.Magic, "Invalid Laye module header.");

        string? moduleName = _reader.ReadString();
        if (moduleName.Length == 0)
            moduleName = null;

        int dependencyCount = _reader.ReadInt32();
        string[] dependencyNames = new string[dependencyCount];
        for (int i = 0; i < dependencyCount; i++)
            dependencyNames[i] = _reader.ReadString();

        return (moduleName, dependencyNames);
    }

    private LayeModule Deserialize()
    {
        var (moduleName, dependencyNames) = ReadModuleHeader();
        foreach (string dependencyName in dependencyNames)
        {
            _context.Assert(_dependencies.Any(d => d.ModuleName == dependencyName), $"No dependency provided matching the required dependency name '{dependencyName}'.");
        }

        var module = new LayeModule(_context, [], _dependencies);
        module.ModuleName = moduleName;

        return module;
    }

    private SemaTypeQual DeserializeType()
    {
        throw new NotImplementedException();
    }

    private SemaDeclNamed DeserializeDecl()
    {
        throw new NotImplementedException();
    }

    private SemaDeclFunction DeserializeDeclFunction()
    {
        throw new NotImplementedException();
    }
}
