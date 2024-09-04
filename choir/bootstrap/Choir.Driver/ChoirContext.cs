using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

using Choir.Front.Laye.Sema;

namespace Choir;

[Flags]
public enum FileLookupLocations
{
    None = 0,
    IncludeDirectories = 1 << 0,
    LibraryDirectories = 1 << 1,
}

public sealed record class ChoirTarget
{
    public static readonly ChoirTarget X86_64 = new()
    {
        SizeOfPointer = Size.FromBits(64),
        AlignOfPointer = Align.ForBits(64),

        SizeOfCBool = Size.FromBits(8),
        SizeOfCChar = Size.FromBits(8),
        SizeOfCShort = Size.FromBits(16),
        SizeOfCInt = Size.FromBits(32),
        SizeOfCLong = Size.FromBits(64),
        SizeOfCLongLong = Size.FromBits(64),
        SizeOfCFloat = Size.FromBits(32),
        SizeOfCDouble = Size.FromBits(64),
        SizeOfCLongDouble = Size.FromBits(128),

        AlignOfCBool = Align.ForBits(8),
        AlignOfCChar = Align.ForBits(8),
        AlignOfCShort = Align.ForBits(16),
        AlignOfCInt = Align.ForBits(32),
        AlignOfCLong = Align.ForBits(64),
        AlignOfCLongLong = Align.ForBits(64),
        AlignOfCFloat = Align.ForBits(32),
        AlignOfCDouble = Align.ForBits(64),
        AlignOfCLongDouble = Align.ForBits(128),
    };

    public required Size SizeOfPointer { get; init; }
    public required Align AlignOfPointer { get; init; }

    public required Size SizeOfCBool { get; init; }
    public required Size SizeOfCChar { get; init; }
    public required Size SizeOfCShort { get; init; }
    public required Size SizeOfCInt { get; init; }
    public required Size SizeOfCLong { get; init; }
    public required Size SizeOfCLongLong { get; init; }
    public required Size SizeOfCFloat { get; init; }
    public required Size SizeOfCDouble { get; init; }
    public required Size SizeOfCLongDouble { get; init; }

    public required Align AlignOfCBool { get; init; }
    public required Align AlignOfCChar { get; init; }
    public required Align AlignOfCShort { get; init; }
    public required Align AlignOfCInt { get; init; }
    public required Align AlignOfCLong { get; init; }
    public required Align AlignOfCLongLong { get; init; }
    public required Align AlignOfCFloat { get; init; }
    public required Align AlignOfCDouble { get; init; }
    public required Align AlignOfCLongDouble { get; init; }
}

public sealed class TypeStorage(ChoirContext context)
{
    private readonly Dictionary<int, SemaTypeBuiltIn> _layeBoolTypes = [];
    private readonly Dictionary<int, SemaTypeBuiltIn> _layeIntTypes = [];
    private readonly Dictionary<int, SemaTypeBuiltIn> _layeFloatTypes = [];
    
    private readonly Dictionary<SemaDeclStruct, SemaTypeStruct> _layeStructTypes = [];
    private readonly Dictionary<SemaDeclEnum, SemaTypeEnum> _layeEnumTypes = [];
    private readonly Dictionary<SemaDeclAlias, SemaTypeAlias> _layeAliasTypes = [];

    public int MaxSupportedIntBitWidth { get; } = 256;

    public SemaTypePoison LayeTypePoison { get; } = SemaTypePoison.Instance;

    public SemaTypeBuiltIn LayeTypeVoid { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.Void);
    public SemaTypeBuiltIn LayeTypeNoReturn { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.NoReturn);
    public SemaTypeBuiltIn LayeTypeBool { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.Bool);
    public SemaTypeBuiltIn LayeTypeInt { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.Int);
    public SemaTypeBuiltIn LayeTypeFFIBool { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.FFIBool);
    public SemaTypeBuiltIn LayeTypeFFIChar { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.FFIChar);
    public SemaTypeBuiltIn LayeTypeFFIShort { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.FFIShort);
    public SemaTypeBuiltIn LayeTypeFFIInt { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.FFIInt);
    public SemaTypeBuiltIn LayeTypeFFILong { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.FFILong);
    public SemaTypeBuiltIn LayeTypeFFILongLong { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.FFILongLong);
    public SemaTypeBuiltIn LayeTypeFFIFloat { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.FFIFloat);
    public SemaTypeBuiltIn LayeTypeFFIDouble { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.FFIDouble);
    public SemaTypeBuiltIn LayeTypeFFILongDouble { get; } = new SemaTypeBuiltIn(context, BuiltinTypeKind.FFILongDouble);

    public SemaTypeBuiltIn LayeTypeBoolSized(int bitWidth)
    {
        if (!_layeBoolTypes.TryGetValue(bitWidth, out var boolType))
            _layeBoolTypes[bitWidth] = boolType = new SemaTypeBuiltIn(context, BuiltinTypeKind.BoolSized, bitWidth);
        return boolType;
    }

    public SemaTypeBuiltIn LayeTypeIntSized(int bitWidth)
    {
        if (!_layeIntTypes.TryGetValue(bitWidth, out var intType))
            _layeIntTypes[bitWidth] = intType = new SemaTypeBuiltIn(context, BuiltinTypeKind.IntSized, bitWidth);
        return intType;
    }

    public SemaTypeBuiltIn LayeTypeFloatSized(int bitWidth)
    {
        if (!_layeFloatTypes.TryGetValue(bitWidth, out var floatType))
            _layeFloatTypes[bitWidth] = floatType = new SemaTypeBuiltIn(context, BuiltinTypeKind.FloatSized, bitWidth);
        return floatType;
    }

    public SemaTypeStruct LayeTypeStruct(SemaDeclStruct declStruct)
    {
        if (!_layeStructTypes.TryGetValue(declStruct, out var structType))
            _layeStructTypes[declStruct] = structType = new SemaTypeStruct(declStruct);
        return structType;
    }

    public SemaTypeEnum LayeTypeEnum(SemaDeclEnum declEnum)
    {
        if (!_layeEnumTypes.TryGetValue(declEnum, out var enumType))
            _layeEnumTypes[declEnum] = enumType = new SemaTypeEnum(declEnum);
        return enumType;
    }

    public SemaTypeAlias LayeTypeAlias(SemaDeclAlias declAlias)
    {
        if (!_layeAliasTypes.TryGetValue(declAlias, out var aliasType))
            _layeAliasTypes[declAlias] = aliasType = new SemaTypeAlias(declAlias);
        return aliasType;
    }

    public SemaTypeArray LayeArrayType(SemaTypeQual elementType, long length)
    {
        return LayeArrayType(elementType, new SemaExprLiteralInteger(Location.Nowhere, length, context.Types.LayeTypeInt.Qualified(Location.Nowhere)));
    }

    public SemaTypeArray LayeArrayType(SemaTypeQual elementType, SemaExpr length)
    {
        return new SemaTypeArray(elementType, [length]);
    }
}

public sealed class ChoirContext
{
    private DiagnosticWriter _diag = null!;

    private readonly List<SourceFile> _sourceFiles = [];
    private readonly Dictionary<string, SourceFile> _sourceFilesByCanonicalPath = [];

    public ChoirTarget Target { get; }
    public bool UseColor { get; }
    public DiagnosticWriter Diag
    {
        get => _diag;
        set
        {
            if (_diag is not null)
                _diag.OnIssue -= OnDiagnosticIssue;

            _diag = value;
            value.OnIssue += OnDiagnosticIssue;
        }
    }

    public int ErrorLimit { get; set; } = 10;
    public bool HasIssuedError { get; private set; } = false;

    public DiagnosticLocationStyle DiagnosticLocationStyle { get; set; } = DiagnosticLocationStyle.LineColumn;

    public List<string> IncludeDirectories { get; set; } = [];
    public List<string> LibraryDirectories { get; set; } = [];
    public TypeStorage Types { get; }

    public ChoirContext(ChoirTarget target, bool useColor)
    {
        Target = target;
        UseColor = useColor;
        Diag = new StreamingDiagnosticWriter(this, Console.Error);
        Types = new(this);
    }

    private void OnDiagnosticIssue(DiagnosticKind kind)
    {
        if (kind >= DiagnosticKind.Error)
            HasIssuedError = true;
    }

    public FileInfo? LookupFile(string relativeSearchPath, DirectoryInfo? relativeTo, FileLookupLocations locations)
    {
        if (relativeTo is not null && LookupFileInDirectory(relativeTo) is {} relativeFileInfo)
            return relativeFileInfo;

        if (locations.HasFlag(FileLookupLocations.IncludeDirectories))
        {
            foreach (string includeDir in IncludeDirectories)
            {
                if (LookupFileInDirectory(new DirectoryInfo(includeDir)) is {} includeFileInfo)
                    return includeFileInfo;
            }
        }

        if (locations.HasFlag(FileLookupLocations.LibraryDirectories))
        {
            foreach (string libraryDir in LibraryDirectories)
            {
                if (LookupFileInDirectory(new DirectoryInfo(libraryDir)) is {} libraryFileInfo)
                    return libraryFileInfo;
            }
        }

        return null;

        FileInfo? LookupFileInDirectory(DirectoryInfo directory)
        {
            if (!directory.Exists) return null;

            var fileInfo = new FileInfo(Path.Combine(directory.FullName, relativeSearchPath));
            if (fileInfo.Exists)
                return fileInfo;
            
            return null;
        }
    }

    public SourceFile GetSourceFile(FileInfo fileInfo)
    {
        string canonicalPath = fileInfo.FullName;
        if (!_sourceFilesByCanonicalPath.TryGetValue(canonicalPath, out var sourceFile))
        {
            short fileId = (short)(_sourceFiles.Count + 1);

            string sourceText;
            try
            {
                sourceText = File.ReadAllText(canonicalPath, Encoding.UTF8).ReplaceLineEndings("\n");
                if (!sourceText.EndsWith('\n'))
                    sourceText += "\n"; // oh boy this is a lot of copying, forgive me
            }
            catch (Exception ex)
            {
                Diag.ICE($"Could not read source file \"{canonicalPath}\": {ex.Message}");
                throw new UnreachableException();
            }

            sourceFile = new SourceFile(this, fileInfo, fileId, sourceText);

            _sourceFiles.Add(sourceFile);
            _sourceFilesByCanonicalPath[canonicalPath] = sourceFile;
        }

        return sourceFile;
    }

    public SourceFile? GetSourceFileById(int fileId)
    {
        // NOTE(local): we return File IDs as their index + 1, so manually account for that shift in range
        if (fileId <= 0 || fileId > _sourceFiles.Count)
            return null;

        return _sourceFiles[fileId - 1];
    }
    
    [InterpolatedStringHandler]
    public readonly ref struct AssertInterpolatedStringHandler
    {
        private readonly StringBuilder builder;

        public AssertInterpolatedStringHandler(int literalLength, int formattedCount, bool condition, out bool shouldAppend)
        {
            builder = new(literalLength);
            shouldAppend = !condition;
        }

        public readonly void AppendLiteral(string s)
        {
            builder.Append(s);
        }

        public readonly void AppendFormatted<T>(T t)
        {
            builder.Append(t?.ToString());
        }

        internal readonly string GetFormattedText() => builder.ToString();
    }

    [DoesNotReturn]
    public void Unreachable()
    {
        Diag.ICE("reached unreachable code");
    }

    [DoesNotReturn]
    public void Unreachable(string message)
    {
        Diag.ICE($"reached unreachable code: {message}");
    }

    public void Assert([DoesNotReturnIf(false)] bool condition, [InterpolatedStringHandlerArgument("condition")] ref AssertInterpolatedStringHandler message)
    {
        if (condition) return;
        Diag.ICE(message.GetFormattedText());
    }

    public void Assert([DoesNotReturnIf(false)] bool condition, Location location, [InterpolatedStringHandlerArgument("condition")] ref AssertInterpolatedStringHandler message)
    {
        if (condition) return;
        Diag.ICE(location, message.GetFormattedText());
    }

    public void Assert([DoesNotReturnIf(false)] bool condition, string message)
    {
        if (condition) return;
        Diag.ICE(message);
    }

    public void Assert([DoesNotReturnIf(false)] bool condition, Location location, string message)
    {
        if (condition) return;
        Diag.ICE(location, message);
    }
}
