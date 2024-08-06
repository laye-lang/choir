using System.Text;

using Choir.Front.Laye.Sema;

using ClangSharp;
using ClangSharp.Interop;

using static ClangSharp.Interop.CXTranslationUnit_Flags;

namespace Choir.Front.Laye.Syntax;

public partial class Parser
{
    private readonly HashSet<CXCursor> _visitedCanonicalDecls = [];
    private readonly Dictionary<CXCursor, SemaDecl> _generatedCTypeDecls = [];

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

            using var tu = ClangSharp.TranslationUnit.GetOrCreate(translationUnit);
            foreach (var decl in tu.TranslationUnitDecl.Decls)
            {
                CreateCBindingSema(cImportModule, decl);
            }
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private Location GetLocation(Cursor cursor)
    {
        cursor.Location.GetFileLocation(out var cxFile, out var line, out var column, out var offset);
        if (line == 0 && column == 0 && offset == 0) return Location.Nowhere;
        
        SourceFile cFile;
        using (var nameString = cxFile.Name)
            cFile = Context.GetSourceFile(new FileInfo(nameString.CString));
            
        return new Location((int)offset, 1, cFile.FileId);
    }

    private SemaTypeQual? CreateCBindingSema(Module cModule, ClangSharp.Type type)
    {
        type = type.CanonicalType;

        var qualifiers = type.IsLocalConstQualified ? TypeQualifiers.None : TypeQualifiers.Mutable;
        

        var typeKind = type.Kind;
        if (typeKind == CXTypeKind.CXType_FirstBuiltin)
            typeKind = CXTypeKind.CXType_Void;

        switch (typeKind)
        {
            default:
            {
                Console.WriteLine($"Unhandled type: {typeKind} ({type})");
                return null;
            }

            case CXTypeKind.CXType_Void: return cModule.Context.Types.LayeTypeVoid.Qualified(Location.Nowhere, qualifiers);
            case CXTypeKind.CXType_Bool: return cModule.Context.Types.LayeTypeBool.Qualified(Location.Nowhere, qualifiers);
            case CXTypeKind.CXType_SChar:
            case CXTypeKind.CXType_Char_S:
            case CXTypeKind.CXType_Char_U:
            case CXTypeKind.CXType_UChar: return cModule.Context.Types.LayeTypeFFIChar.Qualified(Location.Nowhere, qualifiers);
            case CXTypeKind.CXType_Short:
            case CXTypeKind.CXType_UShort: return cModule.Context.Types.LayeTypeFFIShort.Qualified(Location.Nowhere, qualifiers);
            case CXTypeKind.CXType_Int:
            case CXTypeKind.CXType_UInt: return cModule.Context.Types.LayeTypeFFIInt.Qualified(Location.Nowhere, qualifiers);
            case CXTypeKind.CXType_Long:
            case CXTypeKind.CXType_ULong: return cModule.Context.Types.LayeTypeFFILong.Qualified(Location.Nowhere, qualifiers);
            case CXTypeKind.CXType_LongLong:
            case CXTypeKind.CXType_ULongLong: return cModule.Context.Types.LayeTypeFFILongLong.Qualified(Location.Nowhere, qualifiers);

            case CXTypeKind.CXType_Pointer:
            {
                var typePointer = (PointerType)type;
                var elementType = CreateCBindingSema(cModule, typePointer.PointeeType);
                if (elementType is null) return null;
                return new SemaTypePointer(elementType).Qualified(Location.Nowhere);
            }

            case CXTypeKind.CXType_Elaborated:
            {
                var typeElaborated = (ElaboratedType)type;
                //Console.WriteLine($"Unhandled elaborated: {typeElaborated.OwnedTagDecl} ({typeElaborated.NamedType})");
                return CreateCBindingSema(cModule, typeElaborated.NamedType);
            }

            case CXTypeKind.CXType_Typedef:
            {
                var typeTypedef = (TypedefType)type;
                if (LookupDecl(typeTypedef.Decl) is SemaDeclAlias decl)
                {
                    return new SemaTypeElaborated([typeTypedef.Decl.Name], decl.AliasedType).Qualified(Location.Nowhere, qualifiers);
                }
                
                Console.WriteLine($"Unhandled typedef type: {typeTypedef.Decl} {typeTypedef.Decl.Handle.Hash}");
                return null;
            }

            case CXTypeKind.CXType_Record:
            {
                var typeRecord = (RecordType)type;
                if (LookupDecl(typeRecord.Decl) is SemaDeclStruct structDecl)
                    return new SemaTypeStruct(structDecl).Qualified(Location.Nowhere, qualifiers);
                else if (typeRecord.Decl.Definition is null)
                    return cModule.Context.Types.LayeTypeVoid.Qualified(Location.Nowhere, qualifiers);

                Console.WriteLine($"Unhandled record type: {typeRecord} ({typeRecord.Decl}, {typeRecord.Decl.UnderlyingDecl.Handle.Hash})");
                return null;
            }
        }
    }

    private SemaDecl? LookupDecl(Decl decl)
    {
        if (_generatedCTypeDecls.TryGetValue(decl.CanonicalDecl.Handle, out var node))
            return node;
        
        return null;
    }

    private void CreateCBindingSema(Module cModule, Decl decl)
    {
        decl = decl.CanonicalDecl;
        if (_visitedCanonicalDecls.Contains(decl.Handle)) return;
        _visitedCanonicalDecls.Add(decl.Handle);
        if (decl.IsInvalidDecl) return;

        var location = GetLocation(decl);
        if (location.FileId == 0) return;

        SemaDecl? generatedDecl = null;

        var declKind = decl.Kind;
        if (declKind == CX_DeclKind.CX_DeclKind_FirstRecord)
            declKind = CX_DeclKind.CX_DeclKind_Record;
        else if (declKind == CX_DeclKind.CX_DeclKind_FirstFunction)
            declKind = CX_DeclKind.CX_DeclKind_Function;
        else if (declKind == CX_DeclKind.CX_DeclKind_FirstVar)
            declKind = CX_DeclKind.CX_DeclKind_Var;

        switch (declKind)
        {
            default: break;

            case CX_DeclKind.CX_DeclKind_Typedef:
            {
                var declTypedef = (TypedefNameDecl)decl;

                var aliasedType = CreateCBindingSema(cModule, declTypedef.UnderlyingType);
                if (aliasedType is null) break;

                generatedDecl = new SemaDeclAlias(location, declTypedef.Name, aliasedType);
            } break;

            case CX_DeclKind.CX_DeclKind_Record:
            {
                var declRecord = (RecordDecl)decl;
                if (declRecord.Definition is {} defRecord)
                {
                    var fields = new SemaDeclField[defRecord.Fields.Count];
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var fieldType = CreateCBindingSema(cModule, defRecord.Fields[i].Type);
                        if (fieldType is null) goto end_generate;
                        string fieldName = defRecord.Fields[i].Name;
                        fields[i] = new(Location.Nowhere, fieldName, fieldType);
                    }

                    generatedDecl = new SemaDeclStruct(location, $"struct_{declRecord.Name}", fields, []);
                }
                else
                {
                    generatedDecl = new SemaDeclAlias(location, $"struct_{declRecord.Name}", cModule.Context.Types.LayeTypeVoid.Qualified(location));
                }
            } break;

            case CX_DeclKind.CX_DeclKind_Function:
            {
                var declFunction = (FunctionDecl)decl;

                var returnType = declFunction.IsNoReturn
                    ? Context.Types.LayeTypeNoReturn.Qualified(Location.Nowhere)
                    : CreateCBindingSema(cModule, declFunction.ReturnType);
                if (returnType is null) break;

                var paramDecls = new SemaDeclParameter[declFunction.NumParams];
                for (int i = 0; i < paramDecls.Length; i++)
                {
                    var paramType = CreateCBindingSema(cModule, declFunction.Parameters[i].Type);
                    if (paramType is null) goto end_generate;
                    string paramName = declFunction.Parameters[i].Name.Length == 0 ? $"param{i}" : declFunction.Parameters[i].Name;
                    paramDecls[i] = new(GetLocation(declFunction.Parameters[i]), paramName, paramType);
                }

                generatedDecl = new SemaDeclFunction(location, declFunction.Name, returnType, paramDecls);
            } break;

            case CX_DeclKind.CX_DeclKind_Var:
            {
                var declVar = (VarDecl)decl;

                var varType = CreateCBindingSema(cModule, declVar.Type);
                if (varType is null) break;

                generatedDecl = new SemaDeclBinding(location, declVar.Name, varType, null);
            } break;
        }

end_generate:;
        if (generatedDecl is not null)
        {
            // Console.WriteLine($"Handled decl: {decl.Handle.Hash} = {decl.Spelling}");
            _generatedCTypeDecls[decl.Handle] = generatedDecl;
            cModule.AddDecl(generatedDecl);
        }
        else
        {
            Console.WriteLine($"Unhandled decl: {declKind} {decl.Spelling}");
        }
    }

#if false
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
#endif
}
