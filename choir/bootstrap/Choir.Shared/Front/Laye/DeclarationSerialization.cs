using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Linq;

using Choir.Front.Laye.Sema;

namespace Choir.Front.Laye;

internal static class SerializerConstants
{
    // The magic number is "LAYEMOD1" -- this is it as little endian ASCII bytes.
    public const ulong ModuleMagicNumber = 0x31444F4D4559414CUL;

    public const long Alignment = 4;

    public const string AtomChunkName = "Atom";
    public const string FileChunkName = "File";
    public const string TypeChunkName = "Type";
    public const string DeclChunkName = "Decl";
    public const string TextChunkName = "Text";

    public const char FunctionSigil = 'F';
    public const char StructSigil = 'S';
    public const char EnumSigil = 'E';
    public const char AliasSigil = 'A';

    public const char DeclNameSimpleSigil = 'n';

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

    public const byte AttribExtensionFlag = 1 << 7;
    public const byte Attrib1CallingConventionMask = 0x0F;
    public const byte Attrib1CallingConventionCDecl = 0x00;
    public const byte Attrib1CallingConventionLaye = 0x01;
    public const byte Attrib1CallingConventionStdCall = 0x02;
    public const byte Attrib1CallingConventionFastCall = 0x03;
    public const byte Attrib1ForeignFlag = 1 << 4;
    public const byte Attrib1InlineFlag = 1 << 5;
    public const byte Attrib1DiscardableFlag = 1 << 6;
}

public enum SerializedDeclKind : byte
{
    Invalid = 0,
    Function = (byte)SerializerConstants.FunctionSigil,
}

public enum SerializedTypeKind : byte
{
    Invalid = 0,
    Qualified = (byte)SerializerConstants.QualifiedTypeSigil,
    Void = (byte)SerializerConstants.VoidTypeSigil,
    NoReturn = (byte)SerializerConstants.NoReturnTypeSigil,
    Bool = (byte)SerializerConstants.BoolTypeSigil,
    BoolSized = (byte)SerializerConstants.BoolSizedTypeSigil,
    Int = (byte)SerializerConstants.IntTypeSigil,
    IntSized = (byte)SerializerConstants.IntSizedTypeSigil,
    Float32 = (byte)SerializerConstants.Float32TypeSigil,
    Float64 = (byte)SerializerConstants.Float64TypeSigil,
    FFI = (byte)SerializerConstants.FFIPrefixTypeSigil,
    Pointer = (byte)SerializerConstants.PointerTypeSigil,
    Buffer = (byte)SerializerConstants.BufferTypeSigil,
    Slice = (byte)SerializerConstants.SliceTypeSigil,
    Array = (byte)SerializerConstants.ArrayTypeSigil,
}

public readonly record struct SerializedModuleHeader(
    ulong Magic, string ModuleName, IReadOnlyList<string> DependencyNames, long DataSize);

public sealed class ModuleSerializer : IDisposable
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
        using var writer = new BinaryWriter(stream, Encoding.UTF8, false);
        using var s = new ModuleSerializer(context, module);
        s.Serialize(writer);
    }

    public ChoirContext Context { get; }
    public LayeModule Module { get; }

    private readonly Dictionary<string, BinaryWriter> _chunkWriters = [];

    private readonly Dictionary<int, long> _sourceFileIndices;

    private readonly List<string> _atoms = [];
    private readonly Dictionary<string, long> _atomIndices = [];

    private readonly List<SemaType> _types = [];
    private readonly Dictionary<SemaType, (long Index, BinaryWriter Writer)> _typeWriters = [];

    private readonly List<SemaDeclNamed> _decls = [];
    private readonly Dictionary<SemaDeclNamed, (long Index, BinaryWriter Writer)> _declWriters = [];

    private ModuleSerializer(ChoirContext context, LayeModule module)
    {
        Context = context;
        Module = module;

        _sourceFileIndices = module.SourceFiles.Select((sf, i) => (sf, i))
            .ToDictionary(pair => pair.sf.FileId, pair => (long)pair.i);
    }

    public void Dispose()
    {
    }

    public void WriteAtom(BinaryWriter writer, string? atom)
    {
        if (atom is null)
        {
            writer.Write((byte)0);
            return;
        }

        if (!_atomIndices.TryGetValue(atom, out long index))
        {
            _atomIndices[atom] = index = _atoms.Count + 1;
            _atoms.Add(atom);
        }

        writer.Write7BitEncodedInt64(index);
    }

    public void WriteTypeQual(BinaryWriter writer, SemaTypeQual typeQual)
    {
        long typeIndex = SerializeType(typeQual.Type);

        #region Type & Location

        writer.Write7BitEncodedInt64(typeIndex);
        WriteLocation(writer, typeQual.Location);

        #endregion

        #region Flags

        byte flags = 0;

        if (typeQual.Qualifiers.HasFlag(TypeQualifiers.Mutable))
            flags |= SerializerConstants.MutableFlag;

        writer.Write(flags);

        #endregion
    }

    public void WriteDeclRef(BinaryWriter writer, SemaDeclNamed decl)
    {
        long declIndex = SerializeDecl(decl);
        writer.Write7BitEncodedInt64(declIndex);
    }

    public void WriteDeclName(BinaryWriter writer, string name)
    {
        writer.Write(SerializerConstants.DeclNameSimpleSigil);
        WriteAtom(writer, name);
    }

    public void WriteLocation(BinaryWriter writer, Location location)
    {
        Context.Assert(_sourceFileIndices.ContainsKey(location.FileId), "This location does not exist within the current module.");
        long fileIndex = _sourceFileIndices[location.FileId];
        writer.Write7BitEncodedInt64(fileIndex);
        writer.Write7BitEncodedInt(location.Offset);
        writer.Write7BitEncodedInt(location.Length);
    }

    private void WritePadding(BinaryWriter writer, long size)
    {
        long numPaddingBytes = Align.AlignPadding(size, SerializerConstants.Alignment);
        Context.Assert(numPaddingBytes < SerializerConstants.Alignment, $"Writing too many padding bytes: Expected to write less than {SerializerConstants.Alignment} padding bytes, but writing {numPaddingBytes}.");
        for (long i = 0; i < numPaddingBytes; i++)
            writer.Write((byte)0);
    }

    private BinaryWriter GetOrCreateChunkWriter(string chunkName)
    {
        Context.Assert(chunkName.Length == 4, "Chunk names bust be 4 characters long.");
        if (!_chunkWriters.TryGetValue(chunkName, out var chunkWriter))
            _chunkWriters[chunkName] = chunkWriter = new BinaryWriter(new MemoryStream(1024), Encoding.UTF8, false);
        return chunkWriter;
    }

    private void WriteChunkToModuleData(BinaryWriter writer, string chunkName)
    {
        Context.Assert(chunkName.Length == 4, "Chunk names bust be 4 characters long.");
        Context.Assert(_chunkWriters.ContainsKey(chunkName), $"There is no chunk data for the requested chunk '{chunkName}'.");
        var chunkMemoryStream = (MemoryStream)_chunkWriters[chunkName].BaseStream;
        Context.Assert(chunkMemoryStream.Length >= 0, "The size of a chunk must be non-negative. How did this happen?");
        long chunkSize = chunkMemoryStream.Length;

        byte[] chunkNameBytes = Encoding.UTF8.GetBytes(chunkName);
        Context.Assert(chunkNameBytes.Length == 4, $"Expected 4 bytes from chunk name, got {chunkNameBytes.Length}.");
        writer.Write(chunkNameBytes);
        writer.Write(chunkSize);
        writer.Write(chunkMemoryStream.ToArray());
        WritePadding(writer, chunkSize);
    }

    private long GetChunkTotalSize(string chunkName)
    {
        Context.Assert(chunkName.Length == 4, "Chunk names bust be 4 characters long.");
        Context.Assert(_chunkWriters.ContainsKey(chunkName), $"There is no chunk data for the requested chunk '{chunkName}'.");
        var chunkMemoryStream = (MemoryStream)_chunkWriters[chunkName].BaseStream;

        Context.Assert(chunkMemoryStream.Length >= 0, "The size of a chunk must be non-negative. How did this happen?");
        long chunkSize = chunkMemoryStream.Length;

        return (4 * sizeof(byte)) + sizeof(long) + Align.AlignTo(chunkSize, SerializerConstants.Alignment);
    }

    private void WriteModuleHeader(BinaryWriter writer, long size)
    {
        writer.Write(SerializerConstants.ModuleMagicNumber);
        writer.Write7BitEncodedInt(Module.Dependencies.Count);
        writer.Write(Module.ModuleName);
        foreach (var moduleDep in Module.Dependencies)
            writer.Write(moduleDep.ModuleName);
        WritePadding(writer, writer.BaseStream.Position);
        writer.Write(size);
    }

    private void Serialize(BinaryWriter writer)
    {
        #region Generate File chunk

        var fileChunk = GetOrCreateChunkWriter(SerializerConstants.FileChunkName);
        fileChunk.Write7BitEncodedInt(Module.SourceFiles.Count);
        for (int i = 0; i < Module.SourceFiles.Count; i++)
        {
            var sourceFile = Module.SourceFiles[i];
            fileChunk.Write(sourceFile.FilePath);
            if (sourceFile.IsTextless || Context.OmitSourceTextInModuleBinary)
                fileChunk.Write((byte)0); // ReadString should see this as an empty string, I assume
            else fileChunk.Write(sourceFile.Text);
        }

        #endregion

        #region Serialize the AST

        foreach (var decl in Module.ExportedDeclarations)
            SerializeDecl(decl);

        #endregion

        #region Generate Atom chunk

        var atomChunk = GetOrCreateChunkWriter(SerializerConstants.AtomChunkName);
        atomChunk.Write7BitEncodedInt(_atoms.Count);
        for (int i = 0; i < _atoms.Count; i++)
            atomChunk.Write(_atoms[i]);

        #endregion

        #region Generate the Type chunk

        var typeChunk = GetOrCreateChunkWriter(SerializerConstants.TypeChunkName);
        typeChunk.Write7BitEncodedInt(_types.Count);
        for (int i = 0; i < _types.Count; i++)
        {
            var type = _types[i];
            var typeData = _typeWriters[type];
            Context.Assert(i == typeData.Index, "Type indices did not match when serializing module AST.");
            var typeMemory = (MemoryStream)typeData.Writer.BaseStream;
            //typeChunk.Write7BitEncodedInt64(typeMemory.Length);
            typeChunk.Write(typeMemory.GetBuffer(), 0, (int)typeMemory.Length);
        }

        #endregion

        #region Generate the Decl chunk

        var declChunk = GetOrCreateChunkWriter(SerializerConstants.DeclChunkName);
        declChunk.Write7BitEncodedInt(_decls.Count);
        for (int i = 0; i < _decls.Count; i++)
        {
            var decl = _decls[i];
            var declData = _declWriters[decl];
            Context.Assert(i == declData.Index, "Declaration indices did not match when serializing module AST.");
            var declMemory = (MemoryStream)declData.Writer.BaseStream;
            //declChunk.Write7BitEncodedInt64(declMemory.Length);
            declChunk.Write(declMemory.GetBuffer(), 0, (int)declMemory.Length);
        }

        #endregion

        #region Finally, write the bytes to the actual output.

        var dataStream = new MemoryStream(64 * 1024);
        var dataWriter = new BinaryWriter(dataStream, Encoding.UTF8);

        foreach (string chunkName in _chunkWriters.Keys)
            WriteChunkToModuleData(dataWriter, chunkName);

        var comp = new ZstdSharp.Compressor();
        Span<byte> compressedModuleData = comp.Wrap(dataStream.GetBuffer().AsSpan()[..(int)dataStream.Length]);

        WriteModuleHeader(writer, compressedModuleData.Length);
        writer.Write(compressedModuleData);

        #endregion
    }

    private long SerializeType(SemaType type)
    {
        if (_typeWriters.TryGetValue(type, out var data))
            return data.Index;

        var writer = new BinaryWriter(new MemoryStream(64), Encoding.UTF8, false);
        writer.Write((byte)type.SerializedTypeKind);
        type.Serialize(this, writer);

        long index = _types.Count;
        _types.Add(type);
        _typeWriters[type] = (index, writer);

        return index;
    }

    private long SerializeDecl(SemaDeclNamed decl)
    {
        if (_declWriters.TryGetValue(decl, out var data))
            return data.Index;

        long index = _decls.Count;
        _decls.Add(decl);

        var writer = new BinaryWriter(new MemoryStream(64), Encoding.UTF8, false);
        _declWriters[decl] = (index, writer);

        writer.Write((byte)decl.SerializedDeclKind);
        WriteDeclName(writer, decl.Name);
        WriteLocation(writer, decl.Location);
        writer.Write((ushort)0);

        long declDataStartPosition = writer.BaseStream.Position;
        decl.Serialize(this, writer);

        long declDataSize = writer.BaseStream.Position - declDataStartPosition;
        writer.BaseStream.Seek(declDataStartPosition - sizeof(short), SeekOrigin.Begin);
        writer.Write((short)declDataSize);

        return index;
    }
}

public sealed class ModuleDeserializer : IDisposable
{
    public static SerializedModuleHeader DeserializeHeaderFromStream(ChoirContext context, Stream stream)
    {
        using var reader = new BinaryReader(stream);
        var d = new ModuleDeserializer(context, []);
        return d.ReadModuleHeader(reader);
    }

    public static LayeModule DeserializeFromStream(ChoirContext context, LayeModule[] dependencies, Stream stream)
    {
        using var reader = new BinaryReader(stream);
        var d = new ModuleDeserializer(context, dependencies);
        return d.Deserialize(reader);
    }

    public ChoirContext Context { get; }
    private readonly LayeModule[] _dependencies;

    private readonly Dictionary<string, MemoryStream> _chunkMemory = [];

    private SourceFile[] _files = [];
    private string[] _atoms = [];
    private SemaDeclNamed[] _decls = [];
    private SemaType[] _types = [];

    private ModuleDeserializer(ChoirContext context, LayeModule[] dependencies)
    {
        Context = context;
        _dependencies = dependencies;
    }

    public void Dispose()
    {
    }

    public string? ReadAtom(BinaryReader reader)
    {
        long atomIndex = reader.Read7BitEncodedInt64();
        if (atomIndex == 0) return null;
        return _atoms[atomIndex - 1];
    }

    public string ReadDeclName(BinaryReader reader)
    {
        char declNameSigil = reader.ReadChar();
        switch (declNameSigil)
        {
            default:
            {
                Context.Assert(false, $"Unrecognized or unsupported declaration sigil '{declNameSigil}' ({(int)declNameSigil}).");
                throw new UnreachableException();
            }

            case SerializerConstants.DeclNameSimpleSigil: return ReadAtom(reader);
        }
    }

    public Location ReadLocation(BinaryReader reader)
    {
        long fileIndex = reader.Read7BitEncodedInt64();
        int offset = reader.Read7BitEncodedInt();
        int length = reader.Read7BitEncodedInt();
        Context.Assert(fileIndex < _files.Length, "This location does not exist within the current module.");
        int fileId = _files[fileIndex].FileId;
        return new(offset, length, fileId);
    }

    public SemaTypeQual ReadTypeQual(BinaryReader reader)
    {
        #region Type & Location

        long typeIndex = reader.Read7BitEncodedInt64();
        var type = _types[typeIndex];
        var location = ReadLocation(reader);

        #endregion

        #region Flags

        byte flags = reader.ReadByte();

        var q = TypeQualifiers.None;
        if (0 != (flags & SerializerConstants.MutableFlag))
            q |= TypeQualifiers.Mutable;

        #endregion

        return new SemaTypeQual(type, location, q);
    }

    private LayeModule Deserialize(BinaryReader reader)
    {
        var header = ReadModuleHeader(reader);

        //ReadOnlySpan<byte> compressedDataBytes = ((MemoryStream)reader.BaseStream).GetBuffer().AsSpan().Slice((int)reader.BaseStream.Position, (int)header.DataSize);
        ReadOnlySpan<byte> compressedDataBytes = reader.ReadBytes((int)header.DataSize);

        ulong decompressedDataSize = ZstdSharp.Decompressor.GetDecompressedSize(compressedDataBytes);
        byte[] decompressedData = new byte[decompressedDataSize];

        var decompressor = new ZstdSharp.Decompressor();
        int _ = decompressor.Unwrap(compressedDataBytes, decompressedData.AsSpan());

        var dataChunkMemory = new MemoryStream(decompressedData);
        var dataChunkReader = new BinaryReader(dataChunkMemory, Encoding.UTF8, false);

        byte[] chunkNameBytes = new byte[4];
        while (4 == dataChunkReader.Read(chunkNameBytes, 0, 4))
        {
            string chunkName = Encoding.UTF8.GetString(chunkNameBytes);
            long chunkSize = dataChunkReader.ReadInt64();

            long chunkStartPosition = dataChunkReader.BaseStream.Position;
            var chunkMemory = new MemoryStream(decompressedData, (int)chunkStartPosition, (int)chunkSize);
            _chunkMemory[chunkName] = chunkMemory;

            dataChunkReader.BaseStream.Position += chunkSize;
            ReadPadding(dataChunkReader, dataChunkReader.BaseStream.Position);
        }

        PopulateFileTable();
        PopulateAtomTable();
        ForwardDeclareDecls();

        DeserializeTypes();
        DeserializeDecls();

        var module = new LayeModule(Context, _files, _dependencies);
        module.ModuleName = header.ModuleName;
        foreach (var decl in _decls)
            module.ExportScope.AddDecl(decl);

        return module;
    }

    private static void ReadPadding(BinaryReader reader, long nRead)
    {
        int paddingCount = (int)Align.AlignPadding(nRead, SerializerConstants.Alignment);
        if (paddingCount > 0) reader.ReadBytes(paddingCount);
    }

    private SerializedModuleHeader ReadModuleHeader(BinaryReader reader)
    {
        ulong magic = reader.ReadUInt64();
        Context.Assert(magic == SerializerConstants.ModuleMagicNumber, "Only the LAYEMOD1 serialized module version is supported.");

        int dependencyCount = reader.Read7BitEncodedInt();
        string moduleName = reader.ReadString();

        string[] dependencyNames = new string[dependencyCount];
        for (int i = 0; i < dependencyCount; i++)
            dependencyNames[i] = reader.ReadString();

        ReadPadding(reader, reader.BaseStream.Position);

        long dataSize = reader.ReadInt64();

        return new(magic, moduleName, dependencyNames, dataSize);
    }

    private void PopulateFileTable()
    {
        if (!_chunkMemory.TryGetValue(SerializerConstants.FileChunkName, out var fileMemory))
            return;

        var fileReader = new BinaryReader(fileMemory, Encoding.UTF8, false);

        int fileCount = fileReader.Read7BitEncodedInt();
        _files = new SourceFile[fileCount];

        for (int i = 0; i < fileCount; i++)
        {
            string filePath = fileReader.ReadString();
            string fileText = fileReader.ReadString();

            if (fileText.IsNullOrEmpty())
                _files[i] = Context.GetSourceFileRef(filePath, null);
            else _files[i] = Context.GetSourceFileRef(filePath, fileText);
        }
    }

    private void PopulateAtomTable()
    {
        if (!_chunkMemory.TryGetValue(SerializerConstants.AtomChunkName, out var atomMemory))
            return;

        var atomReader = new BinaryReader(atomMemory, Encoding.UTF8, false);
        
        int atomCount = atomReader.Read7BitEncodedInt();
        _atoms = new string[atomCount];

        for (int i = 0; i < atomCount; i++)
            _atoms[i] = atomReader.ReadString();
    }

    private void ForwardDeclareDecls()
    {
        if (!_chunkMemory.TryGetValue(SerializerConstants.DeclChunkName, out var declMemory))
            return;

        var declReader = new BinaryReader(declMemory, Encoding.UTF8, false);

        int declCount = declReader.Read7BitEncodedInt();
        _decls = new SemaDeclNamed[declCount];

        for (int i = 0; i < declCount; i++)
        {
            var declKind = (SerializedDeclKind)declReader.ReadByte();

            string declName = ReadDeclName(declReader);
            var location = ReadLocation(declReader);

            SemaDeclNamed forwardDecl;
            switch (declKind)
            {
                default:
                {
                    Context.Diag.ICE($"Attempt to deserialize named declaration of type {declKind}, which is currently not supported.");
                    throw new UnreachableException();
                }

                case SerializedDeclKind.Function: forwardDecl = new SemaDeclFunction(location, declName); break;
            }

            _decls[i] = forwardDecl;

            int declDataSize = declReader.ReadUInt16();
            declReader.BaseStream.Position += declDataSize;
        }

        declMemory.Position = 0;
    }

    private void DeserializeTypes()
    {
        if (!_chunkMemory.TryGetValue(SerializerConstants.TypeChunkName, out var typeMemory))
            return;

        var typeReader = new BinaryReader(typeMemory, Encoding.UTF8, false);

        int typeCount = typeReader.Read7BitEncodedInt();
        _types = new SemaType[typeCount];

        for (int i = 0; i < typeCount; i++)
        {
            var typeKind = (SerializedTypeKind)typeReader.ReadByte();
            _types[i] = SemaType.Deserialize(this, typeKind, typeReader);
        }
    }

    private void DeserializeDecls()
    {
        if (!_chunkMemory.TryGetValue(SerializerConstants.DeclChunkName, out var declMemory))
            return;

        var declReader = new BinaryReader(declMemory, Encoding.UTF8, false);
        int declCount = declReader.Read7BitEncodedInt();
        Context.Assert(declCount == _decls.Length, "Number of decls changed somewhere");

        for (int i = 0; i < declCount; i++)
        {
            var declKind = (SerializedDeclKind)declReader.ReadByte();
            var decl = _decls[i];
            Context.Assert(decl.SerializedDeclKind == declKind, "Decl kind changed somewhere");

            string declName = ReadDeclName(declReader);
            Context.Assert(declName == decl.Name, "Decl name changed somewhere");
            var _ = ReadLocation(declReader); // less important to assert, and the other two should have caught weird bugs anyway.

            int declDataSize = declReader.ReadUInt16();
            long currentDeclDataPosition = declReader.BaseStream.Position;

            decl.Deserialize(this, declReader);
            Context.Assert(currentDeclDataPosition + declDataSize == declReader.BaseStream.Position, $"Expected to read {declDataSize} bytes during decl serialization, but read {declReader.BaseStream.Position - currentDeclDataPosition}.");
        }
    }
}

#if false // Old Ser

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

    private readonly List<SemaDeclNamed> _decls = [];
    private readonly Dictionary<SemaDeclNamed, uint> _declIndices = [];
    private readonly Dictionary<SemaDeclNamed, BinaryWriter> _declWriters = [];

    private readonly List<SemaType> _types = [];
    private readonly Dictionary<SemaType, uint> _typeIndices = [];
    private readonly Dictionary<SemaType, BinaryWriter> _typeWriters = [];

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

    private BinaryWriter GetOrCreateChunkWriter(string chunkName)
    {
        _context.Assert(chunkName.Length == 4, "Chunk names bust be 4 characters long.");
        if (!_chunkWriters.TryGetValue(chunkName, out var chunkWriter))
            _chunkWriters[chunkName] = chunkWriter = new BinaryWriter(new MemoryStream(), Encoding.UTF8, false);
        return chunkWriter;
    }

    private void WriteChunkToModuleData(BinaryWriter writer, string chunkName)
    {
        // ModuleChunk = <<
        //     ChunkName:4/unit:8, % "TypT", "DecT", "StrT", "LitT", ...
        //     ChunkSize:32/little,
        //     ChunkData:ChunkSize/unit:8,
        //     Padding4:0..3/unit:8
        // >>

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

        return 4 + 4 + Align.AlignTo((ulong)chunkSize, 4);
    }

    private void WriteModuleHeader(BinaryWriter writer, ulong size)
    {
        // ModuleHeader = <<
        //     MagicNumber:8/unit:8 = "LAYEMOD1", % Includes a version, the '1', if ever it's needed :)
        //     Size:64/little, % how many more bytes are there (after this number)
        // >>

        writer.Write(SerializerConstants.ModuleMagicNumber);
        writer.Write(size);
    }

    private byte[] GenerateModuleDescription()
    {
        // ModuleDescription = <<
        //     ModuleDescriptionSize:32/little,
        //     NumberOfDependencies:32/little,
        //     <<ModuleNameLength:8, ModuleName:ModuleNameLength/unit:8>>,
        //     [<<DependencyLength:8, DependencyName:DependencyLength/unit:8>> || repeat NumberOfDependencies],
        //     Padding4:0..3/unit:8
        // >>

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

        var atomWriter = GetOrCreateChunkWriter(SerializerConstants.AtomChunkName);
        var typeWriter = GetOrCreateChunkWriter(SerializerConstants.TypeChunkName);
        var declWriter = GetOrCreateChunkWriter(SerializerConstants.DeclChunkName);

        // prime the "Type" chunk with a dummy type count.
        typeWriter.Write((uint)0);

        // start by serializing decls, storing atom and type information to be serialized later.
        var declsToSerialize = _module.ExportedDeclarations.ToArray();
        declWriter.Write((uint)declsToSerialize.Length);
        foreach (var decl in declsToSerialize)
            SerializeDecl(decl);

        // Populate the "Atom" chunk now that decl serialization is done.
        atomWriter.Write((uint)_atoms.Count);
        for (int i = 0; i < _atoms.Count; i++)
            atomWriter.Write(_atoms[i]);

        // Re-write the type count to the "Type" chunk.
        typeWriter.Flush();
        typeWriter.Seek(0, SeekOrigin.Begin);
        typeWriter.Write((uint)_types.Count);

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

    private uint SerializeAtom(BinaryWriter writer, string? atomText)
    {
        if (atomText is null)
            return uint.MaxValue;

        if (!_atomIndices.TryGetValue(atomText, out uint atomIndex))
        {
            _atomIndices[atomText] = atomIndex = (uint)_atoms.Count;
            _atoms.Add(atomText);
        }

        writer.Write(atomIndex);
        return atomIndex;
    }

    private void SerializeDecl(SemaDeclNamed decl)
    {
        _context.LogVerbose($"Serializing decl '{decl.Name}'[{decl.Id}] from module '{_module.ModuleName}'.");
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
        var declWriter = GetOrCreateChunkWriter(SerializerConstants.DeclChunkName);

        declWriter.Write(SerializerConstants.FunctionSigil);

        declWriter.Write(SerializerConstants.DeclNameSimpleSigil);
        //WriteString8(declWriter, declFunction.Name);
        SerializeAtom(declWriter, declFunction.Name);

        ushort flags = 0;
        switch (declFunction.CallingConvention)
        {
            default:
            {
                _context.Unreachable($"Unhandled calling convention in serializer: {declFunction.CallingConvention}.");
                throw new UnreachableException();
            }

            case CallingConvention.CDecl: flags |= SerializerConstants.Attrib1CallingConventionCDecl; break;
            case CallingConvention.Laye: flags |= SerializerConstants.Attrib1CallingConventionLaye; break;
            case CallingConvention.StdCall: flags |= SerializerConstants.Attrib1CallingConventionStdCall; break;
            case CallingConvention.FastCall: flags |= SerializerConstants.Attrib1CallingConventionFastCall; break;
        }

        if (declFunction.IsForeign) flags |= SerializerConstants.Attrib1ForeignFlag;
        if (declFunction.IsInline) flags |= SerializerConstants.Attrib1InlineFlag;
        if (declFunction.IsDiscardable) flags |= SerializerConstants.Attrib1DiscardableFlag;

        declWriter.Write(flags);

        if (declFunction.IsForeign)
            SerializeAtom(declWriter, declFunction.ForeignSymbolName);

        // TODO(local): serialize template parameters
        if (declFunction.TemplateParameters is { } templateParams)
        {
            _context.Todo("Serialize function template parameters");
            throw new UnreachableException();
        } else declWriter.Write((ushort)0);

        SerializeTypeQual(declWriter, declFunction.ReturnType);

        declWriter.Write((ushort)declFunction.ParameterDecls.Count);
        for (int i = 0; i < declFunction.ParameterDecls.Count; i++)
        {
            var param = declFunction.ParameterDecls[i];
            //WriteString8(declWriter, param.Name);
            SerializeAtom(declWriter, param.Name);
            SerializeTypeQual(declWriter, param.ParamType);
        }
    }

    private void SerializeTypeQual(BinaryWriter writer, SemaTypeQual typeQual)
    {
        // TypeQual = <<
        //     TypeIndex:32/little,
        //     QualFlags:8
        // >>

        var type = typeQual.Type;
        uint typeIndex = SerializeType(type);
        writer.Write(typeIndex);

        byte flags = 0;
        if (typeQual.Qualifiers.HasFlag(TypeQualifiers.Mutable))
            flags |= SerializerConstants.MutableFlag;
        writer.Write(flags);
    }

    private uint SerializeType(SemaType type)
    {
        if (_typeIndices.TryGetValue(type, out uint typeIndex))
            return typeIndex;

        _context.Assert(type is not SemaTypeStruct, "Serializing structs is going to be a problem so please just don't for right now, thanks.");

        switch (type)
        {
            default: break;
            case SemaTypePointer typePointer: SerializeType(typePointer.ElementType.Type); break;
            case SemaTypeBuffer typeBuffer: SerializeType(typeBuffer.ElementType.Type); break;
            case SemaTypeSlice typeSlice: SerializeType(typeSlice.ElementType.Type); break;
            case SemaTypeArray typeArray: SerializeType(typeArray.ElementType.Type); break;
        }

        _typeIndices[type] = typeIndex = (uint)_types.Count;
        _types.Add(type);

        _context.LogVerbose($"Serializing type '{type.ToDebugString(Colors.Off)}'[{type.Id}] from module '{_module.ModuleName}'.");
        var typeWriter = GetOrCreateChunkWriter(SerializerConstants.TypeChunkName);

        switch (type)
        {
            default:
            {
                _context.Assert(false, $"Unimplemented type in serializer: {type.GetType().FullName}");
                throw new UnreachableException();
            }

            case SemaTypeBuiltIn typeBuiltIn: SerializeTypeBuiltIn(typeBuiltIn); break;
            case SemaTypePointer typePointer:
            {
                typeWriter.Write(SerializerConstants.PointerTypeSigil);
                SerializeTypeQual(typeWriter, typePointer.ElementType);
            } break;
            case SemaTypeBuffer typeBuffer:
            {
                typeWriter.Write(SerializerConstants.BufferTypeSigil);
                SerializeTypeQual(typeWriter, typeBuffer.ElementType);
            } break;
            case SemaTypeSlice typeSlice:
            {
                typeWriter.Write(SerializerConstants.SliceTypeSigil);
                SerializeTypeQual(typeWriter, typeSlice.ElementType);
            } break;
            case SemaTypeArray typeArray:
            {
                typeWriter.Write(SerializerConstants.ArrayTypeSigil);
                SerializeTypeQual(typeWriter, typeArray.ElementType);
            } break;
        }

        return typeIndex;
    }

    private void SerializeTypeBuiltIn(SemaTypeBuiltIn type)
    {
        var typeWriter = GetOrCreateChunkWriter(SerializerConstants.TypeChunkName);
        switch (type.Kind)
        {
            default:
            {
                _context.Unreachable($"Unhandled built-in type kind in serializer: {type.Kind}.");
                throw new UnreachableException();
            }

            case BuiltinTypeKind.Void: typeWriter.Write(SerializerConstants.VoidTypeSigil); break;
            case BuiltinTypeKind.NoReturn: typeWriter.Write(SerializerConstants.NoReturnTypeSigil); break;
            case BuiltinTypeKind.Bool: typeWriter.Write(SerializerConstants.BoolTypeSigil); break;
            case BuiltinTypeKind.BoolSized:
            {
                typeWriter.Write(SerializerConstants.BoolSizedTypeSigil);
                typeWriter.Write((ushort)type.Size.Bits);
            } break;
            case BuiltinTypeKind.Int: typeWriter.Write(SerializerConstants.IntTypeSigil); break;
            case BuiltinTypeKind.IntSized:
            {
                typeWriter.Write(SerializerConstants.IntSizedTypeSigil);
                typeWriter.Write((ushort)type.Size.Bits);
            } break;
            case BuiltinTypeKind.FloatSized:
            {
                switch (type.Size.Bits)
                {
                    default:
                    {
                        _context.Unreachable($"Invalid float bit width in serializer: {type.Size.Bits}.");
                        throw new UnreachableException();
                    }

                    case 32: typeWriter.Write(SerializerConstants.Float32TypeSigil); break;
                    case 64: typeWriter.Write(SerializerConstants.Float64TypeSigil); break;
                }
            } break;

            case BuiltinTypeKind.FFIBool:
            {
                typeWriter.Write(SerializerConstants.FFIPrefixTypeSigil);
                typeWriter.Write(SerializerConstants.FFIBoolTypeSigil);
            } break;
            case BuiltinTypeKind.FFIChar:
            {
                typeWriter.Write(SerializerConstants.FFIPrefixTypeSigil);
                typeWriter.Write(SerializerConstants.FFICharTypeSigil);
            } break;
            case BuiltinTypeKind.FFIShort:
            {
                typeWriter.Write(SerializerConstants.FFIPrefixTypeSigil);
                typeWriter.Write(SerializerConstants.FFIShortTypeSigil);
            } break;
            case BuiltinTypeKind.FFIInt:
            {
                typeWriter.Write(SerializerConstants.FFIPrefixTypeSigil);
                typeWriter.Write(SerializerConstants.FFIIntTypeSigil);
            } break;
            case BuiltinTypeKind.FFILong:
            {
                typeWriter.Write(SerializerConstants.FFIPrefixTypeSigil);
                typeWriter.Write(SerializerConstants.FFILongTypeSigil);
            } break;
            case BuiltinTypeKind.FFILongLong:
            {
                typeWriter.Write(SerializerConstants.FFIPrefixTypeSigil);
                typeWriter.Write(SerializerConstants.FFILongLongTypeSigil);
            } break;
            case BuiltinTypeKind.FFIFloat:
            {
                typeWriter.Write(SerializerConstants.FFIPrefixTypeSigil);
                typeWriter.Write(SerializerConstants.FFIFloatTypeSigil);
            } break;
            case BuiltinTypeKind.FFIDouble:
            {
                typeWriter.Write(SerializerConstants.FFIPrefixTypeSigil);
                typeWriter.Write(SerializerConstants.FFIDoubleTypeSigil);
            } break;
            case BuiltinTypeKind.FFILongDouble:
            {
                typeWriter.Write(SerializerConstants.FFIPrefixTypeSigil);
                typeWriter.Write(SerializerConstants.FFILongDoubleTypeSigil);
            } break;
        }
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

    private string[] _atoms = [];
    private SemaType[] _types = [];
    private SemaDeclNamed[] _decls = [];

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
            ReadPadding(chunkSize);

            var chunkReader = new BinaryReader(new MemoryStream(chunkData), Encoding.UTF8, false);
            _chunkReaders[chunkName] = chunkReader;
        }

        DeserializeAtoms();
        DeserializeTypes();
        DeserializeDecls();

        foreach (var decl in _decls)
            module.ExportScope.AddDecl(decl);

        return module;
    }

    private string? GetAtom(uint index)
    {
        if (index == uint.MaxValue)
            return null;

        return _atoms[index];
    }

    private void DeserializeAtoms()
    {
        if (!TryGetChunkReader(SerializerConstants.AtomChunkName, out var atomChunk))
        {
            // TODO(local): this should probably be an error in the deserialization process
            return;
        }

        uint atomCount = atomChunk.ReadUInt32();
        _atoms = new string[atomCount];

        for (uint i = 0; i < atomCount; i++)
        {
            string atom = atomChunk.ReadString();
            _atoms[i] = atom;
        }
    }

    private void DeserializeTypes()
    {
        if (!TryGetChunkReader(SerializerConstants.TypeChunkName, out var typeChunk))
        {
            // TODO(local): this should probably be an error in the deserialization process
            return;
        }

        uint typeCount = typeChunk.ReadUInt32();
        _types = new SemaType[typeCount];

        for (int i = 0; i < typeCount; i++)
            _types[i] = DeserializeType();
    }

    private void DeserializeDecls()
    {
        if (!TryGetChunkReader(SerializerConstants.DeclChunkName, out var declChunk))
        {
            // TODO(local): this should probably be an error in the deserialization process
            return;
        }

        var decls = new List<SemaDeclNamed>();
        uint declCount = declChunk.ReadUInt32();

        int readValue;
        while ((readValue = declChunk.Read()) != -1)
        {
            char sigil = (char)readValue;
            var decl = DeserializeDecl(sigil);
            decl.Linkage = Linkage.Imported;
            decls.Add(decl);
        }

        _decls = decls.ToArray();
    }

    private SemaType DeserializeType()
    {
        if (!TryGetChunkReader(SerializerConstants.TypeChunkName, out var typeChunk))
        {
            // TODO(local): this should probably be an error in the deserialization process
            _context.Unreachable("Shouldn't get here if no type chunk exists.");
            throw new UnreachableException();
        }

        char sigil = typeChunk.ReadChar();
        switch (sigil)
        {
            default:
            {
                _context.Unreachable($"Unrecognized type sigil: '{sigil}' ({(int)sigil}).");
                throw new UnreachableException();
            }

            case SerializerConstants.VoidTypeSigil: return _context.Types.LayeTypeVoid;
            case SerializerConstants.NoReturnTypeSigil: return _context.Types.LayeTypeNoReturn;
            case SerializerConstants.BoolTypeSigil: return _context.Types.LayeTypeBool;
            case SerializerConstants.BoolSizedTypeSigil: return _context.Types.LayeTypeBoolSized(typeChunk.ReadUInt16());
            case SerializerConstants.IntTypeSigil: return _context.Types.LayeTypeInt;
            case SerializerConstants.IntSizedTypeSigil: return _context.Types.LayeTypeIntSized(typeChunk.ReadUInt16());
            case SerializerConstants.Float32TypeSigil: return _context.Types.LayeTypeFloatSized(32);
            case SerializerConstants.Float64TypeSigil: return _context.Types.LayeTypeFloatSized(64);
            case SerializerConstants.FFIPrefixTypeSigil:
            {
                char ffiSigil = typeChunk.ReadChar();
                switch (ffiSigil)
                {
                    default:
                    {
                        _context.Unreachable($"Unrecognized FFI type sigil: '{ffiSigil}' ({(int)ffiSigil}).");
                        throw new UnreachableException();
                    }

                    case SerializerConstants.FFIBoolTypeSigil: return _context.Types.LayeTypeFFIBool;
                    case SerializerConstants.FFICharTypeSigil: return _context.Types.LayeTypeFFIChar;
                    case SerializerConstants.FFIShortTypeSigil: return _context.Types.LayeTypeFFIShort;
                    case SerializerConstants.FFIIntTypeSigil: return _context.Types.LayeTypeFFIInt;
                    case SerializerConstants.FFILongTypeSigil: return _context.Types.LayeTypeFFILong;
                    case SerializerConstants.FFILongLongTypeSigil: return _context.Types.LayeTypeFFILongLong;
                    case SerializerConstants.FFIFloatTypeSigil: return _context.Types.LayeTypeFFIFloat;
                    case SerializerConstants.FFIDoubleTypeSigil: return _context.Types.LayeTypeFFIDouble;
                    case SerializerConstants.FFILongDoubleTypeSigil: return _context.Types.LayeTypeFFILongDouble;
                }
            }

            case SerializerConstants.BufferTypeSigil: return _context.Types.LayeTypeBuffer(DeserializeTypeQual(typeChunk));
        }
    }

    private SemaTypeQual DeserializeTypeQual(BinaryReader reader)
    {
        uint elementTypeIndex = reader.ReadUInt32();
        _context.Assert(elementTypeIndex < _types.Length, "Invalid type index.");

        byte flags = reader.ReadByte();
        var qualifiers = TypeQualifiers.None;

        if (0 != (flags & SerializerConstants.MutableFlag))
            qualifiers |= TypeQualifiers.Mutable;

        return new SemaTypeQual(_types[elementTypeIndex], Location.Nowhere, qualifiers);
    }

    private SemaDeclNamed DeserializeDecl(char sigil)
    {
        switch (sigil)
        {
            default:
            {
                _context.Unreachable($"Unrecognized decl sigil: '{sigil}' ({(int)sigil}).");
                throw new UnreachableException();
            }

            case SerializerConstants.FunctionSigil: return DeserializeDeclFunction();
        }
    }

    private SemaDeclFunction DeserializeDeclFunction()
    {
        if (!TryGetChunkReader(SerializerConstants.DeclChunkName, out var declChunk))
        {
            _context.Unreachable("Shouldn't get here if no decl chunk exists.");
            throw new UnreachableException();
        }

        char nameSigil = declChunk.ReadChar();
        _context.Assert(nameSigil == SerializerConstants.DeclNameSimpleSigil, "Need to support other decl name kinds");
        string functionName = GetAtom(declChunk.ReadUInt32())!;

        var decl = new SemaDeclFunction(Location.Nowhere, functionName);

        ushort flags = declChunk.ReadUInt16();
        switch (flags & SerializerConstants.Attrib1CallingConventionMask)
        {
            case SerializerConstants.Attrib1CallingConventionCDecl: decl.CallingConvention = CallingConvention.CDecl; break;
            case SerializerConstants.Attrib1CallingConventionLaye: decl.CallingConvention = CallingConvention.Laye; break;
            case SerializerConstants.Attrib1CallingConventionStdCall: decl.CallingConvention = CallingConvention.StdCall; break;
            case SerializerConstants.Attrib1CallingConventionFastCall: decl.CallingConvention = CallingConvention.FastCall; break;
        }

        if (0 != (flags & SerializerConstants.Attrib1ForeignFlag))
            decl.IsForeign = true;
        if (0 != (flags & SerializerConstants.Attrib1InlineFlag))
            decl.IsInline = true;
        if (0 != (flags & SerializerConstants.Attrib1DiscardableFlag))
            decl.IsDiscardable = true;

        if (0 != (flags & SerializerConstants.AttribExtensionFlag))
        {
            _context.Unreachable("No extended flags are currently supported.");
        }

        if (decl.IsForeign)
            decl.ForeignSymbolName = GetAtom(declChunk.ReadUInt32());

        ushort templateParamCount = declChunk.ReadUInt16();
        _context.Assert(templateParamCount == 0, "Template parameter deserialization is not yet supported.");

        decl.ReturnType = DeserializeTypeQual(declChunk);

        ushort paramCount = declChunk.ReadUInt16();
        var paramDecls = new SemaDeclParam[paramCount];
        for (int i = 0; i < paramCount; i++)
        {
            string paramName = GetAtom(declChunk.ReadUInt32())!;
            var paramType = DeserializeTypeQual(declChunk);
            paramDecls[i] = new SemaDeclParam(Location.Nowhere, paramName, paramType);
        }

        decl.ParameterDecls = paramDecls;
        return decl;
    }
}

#endif
