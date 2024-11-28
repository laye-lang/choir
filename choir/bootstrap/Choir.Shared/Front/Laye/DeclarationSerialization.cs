using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

using Choir.Front.Laye.Sema;

namespace Choir.Front.Laye;

internal static class SerializerConstants
{
    // The magic number is "LAYEMOD1" -- this is it as little endian ASCII bytes.
    public const ulong ModuleMagicNumber = 0x31444F4D4559414CUL;

    public const char FunctionSigil = 'F';

    public const char QualifiedTypeSigil = 'Q';
    public const char VoidTypeSigil = 'v';
    public const char NoReturnTypeSigil = 'V';
    public const char BoolTypeSigil = 'b';
    public const char BoolSizedTypeSigil = 'B';
    public const char IntTypeSigil = 'i';
    public const char IntSizedTypeSigil = 'I';
    public const char Float32TypeSigil = 'f';
    public const char Float64TypeSigil = 'd';
    public const char FFIPrefixTypeSigil = 'C';
    public const char FFIBoolTypeSigil = 'b';
    public const char FFICharTypeSigil = 'c';
    public const char FFIShortTypeSigil = 's';
    public const char FFIIntTypeSigil = 'i';
    public const char FFILongTypeSigil = 'l';
    public const char FFILongLongTypeSigil = 'L';
    public const char FFIFloatTypeSigil = 'f';
    public const char FFIDoubleTypeSigil = 'd';
    public const char FFILongDoubleTypeSigil = 'D';

    public const char PointerTypeSigil = '*';
    public const char BufferTypeSigil = ']';
    public const char SliceTypeSigil = ':';
    public const char ArrayTypeSigil = '[';

    public const byte MutableFlag = 1 << 0;
}

public sealed class DeclarationSerializer : IDisposable
{
    public static byte[] SerializeToBytes(ChoirContext context, LayeModule module)
    {
        using var memoryStream = new MemoryStream();
        SerializeToStream(context, module, memoryStream);
        memoryStream.Flush();
        return memoryStream.ToArray();
    }

    public static void SerializeToStream(ChoirContext context, LayeModule module, Stream stream)
    {
        using var writer = new BinaryWriter(stream);
        using var s = new DeclarationSerializer(context, module);
        s.Serialize(writer);
    }

    private readonly ChoirContext _context;
    private readonly LayeModule _module;

    private readonly Dictionary<string, BinaryWriter> _chunkWriters = [];

    private readonly List<string> _atoms = [];
    private readonly Dictionary<string, uint> _atomIndices = [];

    private DeclarationSerializer(ChoirContext context, LayeModule module)
    {
        _context = context;
        _module = module;
    }

    public void Dispose()
    {
        foreach (var (_, chunkWriter) in _chunkWriters)
            chunkWriter.Dispose();
    }

    private void WriteString8(BinaryWriter writer, string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        _context.Assert(bytes.Length < byte.MaxValue, "String is too long to be written with an 8-bit length.");

        writer.Write((byte)bytes.Length);
        writer.Write(bytes);
    }

    private void WritePadding(BinaryWriter writer, ulong size)
    {
        int numPaddingBytes = (int)Align.AlignPadding(size, 4);
        _context.Assert(numPaddingBytes < 4, $"Writing too many padding bytes: Expected to write less than 4 padding bytes, but writing {numPaddingBytes}.");
        for (int i = 0; i < numPaddingBytes; i++)
            writer.Write((byte)0);
    }

    private uint GetAtomIndex(string atom)
    {
        if (!_atomIndices.TryGetValue(atom, out uint index))
        {
            _atomIndices[atom] = index = (uint)_atoms.Count;
            _atoms.Add(atom);
        }

        return index;
    }

    private BinaryWriter CreateChunkWriter(string chunkName)
    {
        _context.Assert(chunkName.Length == 4, "Chunk names bust be 4 characters long.");
        _context.Assert(!_chunkWriters.ContainsKey(chunkName), $"A chunk writer already exists for chunk '{chunkName}'.");
        var chunkWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, false);
        return _chunkWriters[chunkName] = chunkWriter;
    }

    private void WriteChunkToModuleData(BinaryWriter writer, string chunkName)
    {
        //ModuleChunk = <<
        //    ChunkName:4/unit:8, % "TypT", "DecT", "StrT", "LitT", ...
        //    ChunkSize:32/little,
        //    ChunkData:ChunkSize/unit:8,
        //    Padding4:0..3/unit:8
        //>>

        _context.Assert(chunkName.Length == 4, "Chunk names bust be 4 characters long.");
        _context.Assert(_chunkWriters.ContainsKey(chunkName), $"There is no chunk data for the requested chunk '{chunkName}'.");
        var chunkMemoryStream = (MemoryStream)_chunkWriters[chunkName].BaseStream;
        chunkMemoryStream.Flush();

        _context.Assert(chunkMemoryStream.Length > 0, "The size of a chunk must be non-negative. How did this happen?");
        _context.Assert(chunkMemoryStream.Length <= uint.MaxValue, "The size of a chunk must be representable by a u32.");
        uint chunkSize = (uint)chunkMemoryStream.Length;

        byte[] chunkNameBytes = Encoding.UTF8.GetBytes(chunkName);
        _context.Assert(chunkNameBytes.Length == 4, $"Expected 4 bytes from chunk name, got {chunkNameBytes.Length}.");
        writer.Write(chunkNameBytes);
        writer.Write(chunkSize);
        writer.Write(chunkMemoryStream.ToArray());

        WritePadding(writer, chunkSize);
    }

    private ulong GetChunkTotalSize(string chunkName)
    {
        _context.Assert(chunkName.Length == 4, "Chunk names bust be 4 characters long.");
        _context.Assert(_chunkWriters.ContainsKey(chunkName), $"There is no chunk data for the requested chunk '{chunkName}'.");
        var chunkMemoryStream = (MemoryStream)_chunkWriters[chunkName].BaseStream;
        chunkMemoryStream.Flush();

        _context.Assert(chunkMemoryStream.Length > 0, "The size of a chunk must be non-negative. How did this happen?");
        _context.Assert(chunkMemoryStream.Length <= uint.MaxValue, "The size of a chunk must be representable by a u32.");
        uint chunkSize = (uint)chunkMemoryStream.Length;

        return 4 + 4 + Align.AlignTo(chunkSize, 4);
    }

    private void WriteModuleHeader(BinaryWriter writer, ulong size)
    {
        //ModuleHeader = <<
        //    MagicNumber:8/unit:8 = "LAYEMOD1", % Includes a version, the '1', if ever it's needed :)
        //    Size:64/little, % how many more bytes are there (after this number)
        //>>

        writer.Write(SerializerConstants.ModuleMagicNumber);
        writer.Write(size);
    }

    private byte[] GenerateModuleDescription()
    {
        //ModuleDescription = <<
        //    ModuleDescriptionSize:32/little,
        //    NumberOfDependencies:32/little,
        //    <<ModuleNameLength:8, ModuleName:ModuleNameLength/unit:8>>,
        //    [<<DependencyLength:8, DependencyName:DependencyLength/unit:8>> || repeat NumberOfDependencies],
        //    Padding4:0..3/unit:8
        //>>

        using var descWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, false);
        descWriter.Write((uint)_module.Dependencies.Count);
        WriteString8(descWriter, _module.ModuleName);
        foreach (var moduleDep in _module.Dependencies)
            WriteString8(descWriter, moduleDep.ModuleName);

        descWriter.Flush();
        return ((MemoryStream)descWriter.BaseStream).ToArray();
    }

    private void Serialize(BinaryWriter writer)
    {
        byte[] moduleDescData = GenerateModuleDescription();

        // ... do serialization

        // Populate the "Atom" chunk now that core serialization is done.
        var atomWriter = CreateChunkWriter("Atom");
        atomWriter.Write((uint)_atoms.Count);
        for (int i = 0; i < _atoms.Count; i++)
            atomWriter.Write(_atoms[i]);

        // Calculate the total size, after the header preamble, of the serialized data.
        ulong calculatedRemainingSize = 0;
        calculatedRemainingSize += sizeof(uint) + (uint)moduleDescData.Length;
        calculatedRemainingSize += Align.AlignPadding(calculatedRemainingSize, 4);
        // Add the total size (including the preamble) of each chunk, including its padding.
        foreach (string chunkName in _chunkWriters.Keys)
            calculatedRemainingSize += GetChunkTotalSize(chunkName);

        // Finally, write the bytes to the actual output.
        WriteModuleHeader(writer, calculatedRemainingSize);
        writer.Write((uint)moduleDescData.Length);
        writer.Write(moduleDescData);
        WritePadding(writer, (ulong)moduleDescData.Length);
        foreach (string chunkName in _chunkWriters.Keys)
            WriteChunkToModuleData(writer, chunkName);
    }

    private int SerializeType(SemaTypeQual typeQual)
    {
        throw new NotImplementedException();
    }

    private int SerializeType(SemaType type)
    {
        throw new NotImplementedException();
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

public sealed class DeclarationDeserializer : IDisposable
{
    public static (string ModuleName, string[] DependencyNames) DeserializeHeaderFromStream(ChoirContext context, Stream stream)
    {
        using var reader = new BinaryReader(stream);
        var d = new DeclarationDeserializer(context, [], reader);
        ulong _ = d.ReadModuleHeader();
        return d.ReadModuleDescription();
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

    private readonly Dictionary<string, BinaryReader> _chunkReaders = [];

    private DeclarationDeserializer(ChoirContext context, LayeModule[] dependencies, BinaryReader reader)
    {
        _context = context;
        _dependencies = dependencies;
        _reader = reader;
    }

    public void Dispose()
    {
        foreach (var (_, chunkReader) in _chunkReaders)
            chunkReader.Dispose();
    }

    private string ReadString8(ref uint nRead)
    {
        byte stringLength = _reader.ReadByte();
        nRead += 1u + stringLength;
        return Encoding.UTF8.GetString(_reader.ReadBytes(stringLength));
    }

    private void ReadPadding(ulong nRead)
    {
        int paddingCount = (int)Align.AlignPadding(nRead, 4);
        if (paddingCount > 0) _reader.ReadBytes(paddingCount);
    }

    private bool TryGetChunkReader(string chunkName, [NotNullWhen(true)] out BinaryReader? chunkReader)
    {
        _context.Assert(chunkName.Length == 4, "Chunk names bust be 4 characters long.");
        return _chunkReaders.TryGetValue(chunkName, out chunkReader);
    }

    private ulong ReadModuleHeader()
    {
        ulong magic = _reader.ReadUInt64();
        _context.Assert(magic == SerializerConstants.ModuleMagicNumber, "Invalid Laye module data.");

        ulong moduleSize = _reader.ReadUInt64();
        return moduleSize;
    }

    private (string ModuleName, string[] DependencyNames) ReadModuleDescription()
    {
        uint moduleDescSize = _reader.ReadUInt32();
        uint numDependencies = _reader.ReadUInt32();

        uint nRead = sizeof(uint);

        string moduleName = ReadString8(ref nRead);
        string[] dependencyNames = new string[numDependencies];
        for (uint i = 0; i < numDependencies; i++)
            dependencyNames[i] = ReadString8(ref nRead);

        _context.Assert(moduleDescSize < int.MaxValue, "There's no way the module description is that big.");
        _context.Assert(nRead == moduleDescSize, "Read too much from a module description");

        ReadPadding(nRead);
        return (moduleName, [.. dependencyNames]);
    }

    private LayeModule Deserialize()
    {
        ulong moduleSize = ReadModuleHeader();
        var (moduleName, dependencyNames) = ReadModuleDescription();

        var dependencies = dependencyNames
            .Select(n => _dependencies.Single(d => d.ModuleName == n)).ToArray();
        var module = new LayeModule(_context, [], dependencies)
        {
            ModuleName = moduleName,
        };

        byte[] chunkNameData;
        while ((chunkNameData = _reader.ReadBytes(4)).Length == 4)
        {
            _context.Assert(chunkNameData.Length == 4, "How?");

            string chunkName = Encoding.UTF8.GetString(chunkNameData);
            _context.Assert(chunkName.Length == 4, $"Chunk name '{chunkName}' was expected to be 4 characters long.");

            uint chunkSize = _reader.ReadUInt32();
            byte[] chunkData = _reader.ReadBytes((int)chunkSize);
            _context.Assert(chunkSize == chunkData.Length, $"Malformed module data: Expected to read {chunkSize} bytes for chunk '{chunkName}', but got {chunkData.Length}.");

            var chunkReader = new BinaryReader(new MemoryStream(chunkData), Encoding.UTF8, false);
            _chunkReaders[chunkName] = chunkReader;
        }

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
