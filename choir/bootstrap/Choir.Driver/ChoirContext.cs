using System.Diagnostics;
using System.Text;

using Choir.Front.Laye.Sema;

namespace Choir;

public sealed class TypeStorage
{
    private readonly Dictionary<int, SemaTypeBuiltIn> _layeBoolTypes = [];
    private readonly Dictionary<int, SemaTypeBuiltIn> _layeIntTypes = [];
    private readonly Dictionary<int, SemaTypeBuiltIn> _layeFloatTypes = [];
    
    private readonly Dictionary<SemaDeclStruct, SemaTypeStruct> _layeStructTypes = [];
    private readonly Dictionary<SemaDeclEnum, SemaTypeEnum> _layeEnumTypes = [];
    private readonly Dictionary<SemaDeclAlias, SemaTypeAlias> _layeAliasTypes = [];
    
    public SemaTypePoison LayeTypePoison { get; } = SemaTypePoison.Instance;

    public SemaTypeBuiltIn LayeTypeVoid { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.Void);
    public SemaTypeBuiltIn LayeTypeNoReturn { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.NoReturn);
    public SemaTypeBuiltIn LayeTypeBool { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.Bool);
    public SemaTypeBuiltIn LayeTypeInt { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.Int);
    public SemaTypeBuiltIn LayeTypeFFIBool { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.FFIBool);
    public SemaTypeBuiltIn LayeTypeFFIChar { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.FFIChar);
    public SemaTypeBuiltIn LayeTypeFFIShort { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.FFIShort);
    public SemaTypeBuiltIn LayeTypeFFIInt { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.FFIInt);
    public SemaTypeBuiltIn LayeTypeFFILong { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.FFILong);
    public SemaTypeBuiltIn LayeTypeFFILongLong { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.FFILongLong);
    public SemaTypeBuiltIn LayeTypeFFIFloat { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.FFIFloat);
    public SemaTypeBuiltIn LayeTypeFFIDouble { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.FFIDouble);
    public SemaTypeBuiltIn LayeTypeFFILongDouble { get; } = new SemaTypeBuiltIn(BuiltinTypeKind.FFILongDouble);

    public SemaTypeBuiltIn LayeTypeBoolSized(int bitWidth)
    {
        if (!_layeBoolTypes.TryGetValue(bitWidth, out var boolType))
            _layeBoolTypes[bitWidth] = boolType = new SemaTypeBuiltIn(BuiltinTypeKind.BoolSized, bitWidth);
        return boolType;
    }

    public SemaTypeBuiltIn LayeTypeIntSized(int bitWidth)
    {
        if (!_layeIntTypes.TryGetValue(bitWidth, out var intType))
            _layeIntTypes[bitWidth] = intType = new SemaTypeBuiltIn(BuiltinTypeKind.IntSized, bitWidth);
        return intType;
    }

    public SemaTypeBuiltIn LayeTypeFloatSized(int bitWidth)
    {
        if (!_layeFloatTypes.TryGetValue(bitWidth, out var floatType))
            _layeFloatTypes[bitWidth] = floatType = new SemaTypeBuiltIn(BuiltinTypeKind.FloatSized, bitWidth);
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

    public SemaTypeArray LayeArrayType(SemaTypeQual elementType, int length)
    {
        return new SemaTypeArray(elementType, [length]);
    }
}

public sealed class ChoirContext
{
    private DiagnosticWriter _diag = null!;

    private readonly List<SourceFile> _sourceFiles = [];
    private readonly Dictionary<string, SourceFile> _sourceFilesByCanonicalPath = [];

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
    public TypeStorage Types { get; } = new();

    public ChoirContext(bool useColor)
    {
        UseColor = useColor;
        Diag = new StreamingDiagnosticWriter(this, Console.Error);
    }

    private void OnDiagnosticIssue(DiagnosticKind kind)
    {
        if (kind >= DiagnosticKind.Error)
            HasIssuedError = true;
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
            }
            catch (Exception ex)
            {
                Diag.Fatal($"Could not read source file \"{canonicalPath}\": {ex.Message}");
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
}
