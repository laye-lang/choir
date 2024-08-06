using System.Diagnostics;
using System.Text;

using ClangSharp;
using ClangSharp.Interop;

using static ClangSharp.Interop.CXTranslationUnit_Flags;

namespace Choir.Front.Laye.Syntax;

public partial class Parser
{
    private readonly Dictionary<Decl, SyntaxNode> _generatedCTypeDecls = [];

    private SyntaxToken CreateTokenForCFile(SourceFile cFile, TokenKind kind = TokenKind.Missing)
    {
        return new SyntaxToken(kind, new Location(0, 0, cFile.FileId));
    }

    private void ParseImportedCHeaders()
    {
        if (_cHeaderImports.Count == 0) return;

        var tuBuilder = new StringBuilder();
        foreach (var cImport in _cHeaderImports)
        {
            var locInfo = cImport.Location.SeekLineColumn(Context)!.Value;
            //tuBuilder.AppendLine($"#line {locInfo.Line} \"{SourceFile.FileInfo.FullName}\"");
            tuBuilder.AppendLine($"#include <{cImport.ModuleNameText}>");
        }

        string tuSource = tuBuilder.ToString();
        string tuFileName = SourceFile.FileInfo.Name + ".generated.c";

        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var file = new FileInfo(Path.Combine(tempDir, tuFileName));
            File.WriteAllText(file.FullName, tuSource);

            var choirFile = Context.GetSourceFile(file);
            
            using var index = CXIndex.Create();

            string[] clangArgs = [$"-I{Environment.CurrentDirectory}"];
            var translationUnit = CXTranslationUnit.Parse(index, file.FullName, clangArgs, [], CXTranslationUnit_None);

            var hasErrored = translationUnit.DiagnosticSet.Any(d => d.Severity >= CXDiagnosticSeverity.CXDiagnostic_Error);
            if (hasErrored)
            {
                foreach (var diag in translationUnit.DiagnosticSet)
                {
                    // diag.Location.GetFileLocation(out var diagFile, out _, out _, out _);
                    // var choirDiagFile = Context.GetSourceFile(new FileInfo(diagFile.Name.CString));
                    using var diagText = diag.Format(CXDiagnosticDisplayOptions.CXDiagnostic_DisplaySourceLocation);
                    Console.Error.WriteLine(diagText.CString);
                }

                Context.Diag.ICE("we can't handle reporting Clang diagnostics right now, that needs to happen");
            }

            var clangFile = translationUnit.GetFile(file.FullName);

            var cImportModule = new Module(choirFile);
            Module.TranslationUnit?.AddModule(cImportModule);

            using var tu = TranslationUnit.GetOrCreate(translationUnit);
            foreach (var decl in tu.TranslationUnitDecl.Decls)
            {
                var node = CreateCBindingSyntax(cImportModule, decl);
                if (node is not null)
                    cImportModule.AddTopLevelSyntax(node);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private void EnsureRecordDeclGenerated(Module cModule, SourceFile cFile, RecordDecl declRecord)
    {
        if (_generatedCTypeDecls.ContainsKey(declRecord)) return;
    }

    private void EnsureAliasDeclGenerated(Module cModule, SourceFile cFile, TypedefDecl declTypedef)
    {
        if (_generatedCTypeDecls.ContainsKey(declTypedef)) return;
    }

    private SyntaxNode? CreateCBindingSyntax(Module cModule, SourceFile cFile, ClangSharp.Type type)
    {
        switch (type.Kind)
        {
            default: return null;

            case CXTypeKind.CXType_Void:
            {
                return CreateTokenForCFile(cFile, TokenKind.Void);
            }

            case CXTypeKind.CXType_Elaborated:
            {
                var typeElaborated = type.CastAs<ElaboratedType>();
                return CreateCBindingSyntax(cModule, cFile, typeElaborated.NamedType);
            }

            case CXTypeKind.CXType_Typedef:
            {
                var typeTypedef = type.CastAs<TypedefType>();

                var declTypedef = typeTypedef.Decl;
                return CreateCBindingSyntax(cModule, cFile, declTypedef.UnderlyingType);
            }

            case CXTypeKind.CXType_Record:
            {
                if (type.GetAs<RecordType>() is {} typeRecord)
                {
                    var declRecord = typeRecord.Decl;
                    EnsureRecordDeclGenerated(cModule, cFile, declRecord);
                    return new SyntaxToken(TokenKind.Identifier, new Location(0, 0, cFile.FileId))
                    {
                        TextValue = declRecord.Name,
                    };
                }

                return null;
            }
        }
    }

    private SyntaxNode? CreateCBindingSyntax(Module cModule, Decl decl)
    {
        decl.Location.GetFileLocation(out var cxFile, out var line, out var column, out var offset);
        if (line == 0 && column == 0 && offset == 0) return null;
        
        SourceFile cFile;
        using (var nameString = cxFile.Name)
            cFile = Context.GetSourceFile(new FileInfo(nameString.CString));

        switch (decl.Kind)
        {
            default: return null;

            case CX_DeclKind.CX_DeclKind_Function:
            {
                var declFunc = decl.AsFunction;
                var tokenDeclName = new SyntaxToken(TokenKind.Identifier, new Location((int)offset, 1, cFile.FileId))
                {
                    TextValue = declFunc.Name,
                };

                var returnTypeSyntax = CreateCBindingSyntax(cModule, cFile, declFunc.ReturnType);
                if (returnTypeSyntax is null)
                {
                    //Console.WriteLine($"unhandled return type: {declFunc.ReturnType} ({declFunc.ReturnType.Kind}, {declFunc.ReturnType.GetType().Name})");
                    //returnTypeSyntax = new SyntaxToken(SyntaxKind.TokenMissing, new Location(0, 0, clangFile.FileId));
                    return null;
                }

                return null;
            }
        }
    }
}
