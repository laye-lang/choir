
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Choir;

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
                sourceText = File.ReadAllText(canonicalPath, Encoding.UTF8);
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
