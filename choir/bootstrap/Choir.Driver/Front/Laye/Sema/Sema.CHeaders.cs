//#define DEBUG_PRINT_TO_CONSOLE

using System.Text;

using Choir.Front.Laye.Syntax;

using ClangSharp;
using ClangSharp.Interop;

using static ClangSharp.Interop.CXTranslationUnit_Flags;

namespace Choir.Front.Laye.Sema;

public partial class Sema
{
    private readonly Dictionary<(string RelativeDirPath, string cHeaderFileName), Module> _parsedCHeaderModules = [];
    private readonly HashSet<CXCursor> _visitedCanonicalDecls = [];
    private readonly Dictionary<CXCursor, SemaDecl> _generatedCTypeDecls = [];

    private static void DebugPrint(string message)
    {
#if DEBUG_PRINT_TO_CONSOLE
        Console.WriteLine(message);
#endif
    }

    private SyntaxToken CreateTokenForCFile(SourceFile cFile, TokenKind kind = TokenKind.Missing)
    {
        return new SyntaxToken(kind, new Location(0, 0, cFile.FileId));
    }

    private Module ParseCHeaderFromImport(SyntaxDeclImport cHeaderImport)
    {
        var headerKey = (Module.SourceFile.FileInfo.Directory?.FullName ?? "", cHeaderImport.ModuleNameText);
        if (_parsedCHeaderModules.TryGetValue(headerKey, out var cImportModule))
            return cImportModule;

        var tuBuilder = new StringBuilder();
        
        var locInfo = cHeaderImport.Location.SeekLineColumn(Context)!.Value;
        //tuBuilder.AppendLine($"#line {locInfo.Line} \"{SourceFile.FileInfo.FullName}\"");
        tuBuilder.AppendLine($"#include <{cHeaderImport.ModuleNameText}>");

        string tuSource = tuBuilder.ToString();
        string tuFileName = Module.SourceFile.FileInfo.Name + ".generated.c";

        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var file = new FileInfo(Path.Combine(tempDir, tuFileName));
            File.WriteAllText(file.FullName, tuSource);

            var choirFile = Context.GetSourceFile(file);
            
            using var index = ClangSharp.Index.Create();

            var cflags = cHeaderImport.CFlags?.Flags.Select(f => f.TextValue).Distinct() ?? [];
            string[] clangArgs = [$"-I{Environment.CurrentDirectory}", ..(Context.IncludeDirectories.Select(i => $"-I{i}")), .. cflags];
            var translationUnit = CXTranslationUnit.Parse(index.Handle, file.FullName, clangArgs, [], CXTranslationUnit_None | CXTranslationUnit_SkipFunctionBodies | CXTranslationUnit_DetailedPreprocessingRecord | CXTranslationUnit_CacheCompletionResults);

            bool hasErrored = translationUnit.DiagnosticSet.Any(d => d.Severity >= CXDiagnosticSeverity.CXDiagnostic_Error);
            if (hasErrored)
            {
                foreach (var diag in translationUnit.DiagnosticSet)
                {
                    // diag.Location.GetFileLocation(out var diagFile, out _, out _, out _);
                    // var choirDiagFile = Context.GetSourceFile(new FileInfo(diagFile.Name.CString));
                    using var diagText = diag.Format(CXDiagnosticDisplayOptions.CXDiagnostic_DisplaySourceLocation);
                    Console.Error.WriteLine(diagText.CString);
                }

                //Context.Diag.ICE("we can't handle reporting Clang diagnostics right now, that needs to happen");
            }

            var clangFile = translationUnit.GetFile(file.FullName);

            cImportModule = new Module(choirFile);
            Module.TranslationUnit?.AddModule(cImportModule);
            _parsedCHeaderModules[headerKey] = cImportModule;

            using var tu = ClangSharp.TranslationUnit.GetOrCreate(translationUnit);

            #if false
            var macroDefinitions = tu.TranslationUnitDecl.CursorChildren
                .Where(cursor => cursor is MacroDefinitionRecord)
                .Cast<MacroDefinitionRecord>()
                .ToArray();
            
            foreach (var macro in macroDefinitions)
            {
                var location = GetLocation(macro);
                var definitionFile = Context.GetSourceFileById(location.FileId);
                if (definitionFile is null) continue;
                string text = definitionFile.Text;

                if (location.Seek(Context) is not {} locInfo) continue;

                int lineLength = locInfo.LineLength;
                while (locInfo.LineStart + lineLength < text.Length && text[locInfo.LineStart + lineLength] == '\n' && (locInfo.LineStart + lineLength == 0 || text[locInfo.LineStart + lineLength - 1] == '\\'))
                {
                    lineLength++;
                    while (locInfo.LineStart + lineLength < text.Length && text[locInfo.LineStart + lineLength] != '\n')
                        lineLength++;
                }

                string defineBodyText = text.Substring(locInfo.LineStart, lineLength);
                DebugPrint($"// Line {locInfo.Line}, Column {locInfo.Column} in '{definitionFile.FileInfo.FullName}'");
                DebugPrint(defineBodyText);
            }
            #endif
            
            foreach (var decl in tu.TranslationUnitDecl.Decls)
            {
                //DebugPrint(decl.CursorKindSpelling + " :: " + decl);
                CreateCBindingSema(cImportModule, decl);
            }

            return cImportModule;
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private Location GetLocation(Cursor cursor)
    {
        cursor.Location.GetFileLocation(out var cxFile, out uint line, out uint column, out uint offset);
        if (line == 0 && column == 0 && offset == 0) return Location.Nowhere;
        
        SourceFile cFile;
        using (var nameString = cxFile.Name)
        {
            string name = nameString.CString;
            if (string.IsNullOrEmpty(name) || !File.Exists(name))
                return Location.Nowhere;
            
            cFile = Context.GetSourceFile(new FileInfo(name));
        }
            
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
                DebugPrint($"Unhandled type: {typeKind} ({type})");
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
            case CXTypeKind.CXType_Int128:
            case CXTypeKind.CXType_UInt128: return cModule.Context.Types.LayeTypeIntSized(128).Qualified(Location.Nowhere, qualifiers);
            case CXTypeKind.CXType_Float16: return cModule.Context.Types.LayeTypeFloatSized(16).Qualified(Location.Nowhere, qualifiers);
            case CXTypeKind.CXType_Float: return cModule.Context.Types.LayeTypeFFIFloat.Qualified(Location.Nowhere, qualifiers);
            case CXTypeKind.CXType_Double: return cModule.Context.Types.LayeTypeFFIDouble.Qualified(Location.Nowhere, qualifiers);
            case CXTypeKind.CXType_LongDouble: return cModule.Context.Types.LayeTypeFFILongDouble.Qualified(Location.Nowhere, qualifiers);

            case CXTypeKind.CXType_ConstantArray:
            {
                var typeConstantArray = (ConstantArrayType)type;
                var elementType = CreateCBindingSema(cModule, typeConstantArray.ElementType);
                if (elementType is null) return null;
                return cModule.Context.Types.LayeArrayType(elementType, (int)typeConstantArray.Size).Qualified(Location.Nowhere, qualifiers);
            }

            case CXTypeKind.CXType_Pointer:
            {
                var typePointer = (PointerType)type;
                var elementType = CreateCBindingSema(cModule, typePointer.PointeeType);
                if (elementType is null) return null;
                return new SemaTypePointer(Context, elementType).Qualified(Location.Nowhere);
            }

            case CXTypeKind.CXType_Enum:
            {
                var typeEnum = (EnumType)type;
                // var underlyingIntegerType = typeEnum.Decl.IntegerType;
                //return new SemaTypeEnum();
                DebugPrint($"Unhandled enum type: {typeKind} ({type})");
                return null;
            }

            case CXTypeKind.CXType_Elaborated:
            {
                var typeElaborated = (ElaboratedType)type;
                //DebugPrint($"Unhandled elaborated: {typeElaborated.OwnedTagDecl} ({typeElaborated.NamedType})");
                return CreateCBindingSema(cModule, typeElaborated.NamedType);
            }

            case CXTypeKind.CXType_Typedef:
            {
                var typeTypedef = (TypedefType)type;
                if (LookupDecl(typeTypedef.Decl) is SemaDeclAlias decl)
                {
                    return new SemaTypeElaborated([typeTypedef.Decl.Name], decl.AliasedType).Qualified(Location.Nowhere, qualifiers);
                }
                
                DebugPrint($"Unhandled typedef type: {typeTypedef.Decl} {typeTypedef.Decl.Handle.Hash}");
                return null;
            }

            case CXTypeKind.CXType_Record:
            {
                var typeRecord = (RecordType)type;
                if (LookupDecl(typeRecord.Decl) is SemaDeclStruct structDecl)
                    return new SemaTypeStruct(structDecl).Qualified(Location.Nowhere, qualifiers);
                else if (typeRecord.Decl.Definition is null)
                    return cModule.Context.Types.LayeTypeFFIChar.Qualified(Location.Nowhere, qualifiers);

                DebugPrint($"Unhandled record type: {typeRecord} ({typeRecord.Decl}, {typeRecord.Decl.UnderlyingDecl.Handle.Hash})");
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
        //if (location.FileId == 0) return;

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

                generatedDecl = new SemaDeclAlias(location, declTypedef.Name)
                {
                    AliasedType = aliasedType,
                    Linkage = Linkage.Exported,
                };
                
                cModule.FileScope.AddDecl((SemaDeclNamed)generatedDecl);
                cModule.ExportScope.AddDecl((SemaDeclNamed)generatedDecl);
            } break;

            case CX_DeclKind.CX_DeclKind_Record:
            {
                var declRecord = (RecordDecl)decl;
                string declRecordName = $"struct_{declRecord.Name}";
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

                    generatedDecl = new SemaDeclStruct(location, declRecordName)
                    {
                        FieldDecls = fields,
                        Linkage = Linkage.Exported,
                    };
                }
                else
                {
                    generatedDecl = new SemaDeclAlias(location, declRecordName)
                    {
                        AliasedType = cModule.Context.Types.LayeTypeFFIChar.Qualified(location),
                        Linkage = Linkage.Exported,
                    };
                }
                
                cModule.FileScope.AddDecl((SemaDeclNamed)generatedDecl);
                cModule.ExportScope.AddDecl((SemaDeclNamed)generatedDecl);
            } break;

            case CX_DeclKind.CX_DeclKind_Function:
            {
                var declFunction = (FunctionDecl)decl;

                var returnType = declFunction.IsNoReturn
                    ? Context.Types.LayeTypeNoReturn.Qualified(Location.Nowhere)
                    : CreateCBindingSema(cModule, declFunction.ReturnType);
                if (returnType is null) break;

                var paramDecls = new SemaDeclParam[declFunction.NumParams];
                for (int i = 0; i < paramDecls.Length; i++)
                {
                    var paramType = CreateCBindingSema(cModule, declFunction.Parameters[i].Type);
                    if (paramType is null) goto end_generate;
                    string paramName = declFunction.Parameters[i].Name.Length == 0 ? $"param{i}" : declFunction.Parameters[i].Name;
                    paramDecls[i] = new(GetLocation(declFunction.Parameters[i]), paramName, paramType);
                }

                // TODO(local): variadic C functions
                generatedDecl = new SemaDeclFunction(location, declFunction.Name)
                {
                    ReturnType = returnType,
                    ParameterDecls = paramDecls,
                    Linkage = declFunction.IsGlobal ? Linkage.Exported : Linkage.Internal,
                    IsForeign = true,
                    ForeignSymbolName = declFunction.Name,
                    // TODO(local): get the calling convention from C functions
                    CallingConvention = CallingConvention.CDecl,
                };

                cModule.FileScope.AddDecl((SemaDeclNamed)generatedDecl);
                if (((SemaDeclFunction)generatedDecl).Linkage == Linkage.Exported)
                    cModule.ExportScope.AddDecl((SemaDeclNamed)generatedDecl);
            } break;

            case CX_DeclKind.CX_DeclKind_Var:
            {
                var declVar = (VarDecl)decl;

                var varType = CreateCBindingSema(cModule, declVar.Type);
                if (varType is null) break;

                generatedDecl = new SemaDeclBinding(location, declVar.Name)
                {
                    BindingType = varType,
                    Linkage = declVar.StorageClass == CX_StorageClass.CX_SC_Static ? Linkage.Internal : Linkage.Exported,
                    IsForeign = true,
                    ForeignSymbolName = declVar.Name,
                };

                cModule.FileScope.AddDecl((SemaDeclNamed)generatedDecl);
                if (((SemaDeclBinding)generatedDecl).Linkage == Linkage.Exported)
                    cModule.ExportScope.AddDecl((SemaDeclNamed)generatedDecl);
            } break;
        }

end_generate:;
        if (generatedDecl is not null)
        {
            // DebugPrint($"Handled decl: {decl.Handle.Hash} = {decl.Spelling}");
            _generatedCTypeDecls[decl.Handle] = generatedDecl;
            cModule.AddDecl(generatedDecl);
        }
        else
        {
            DebugPrint($"Unhandled decl: {declKind} {decl.Spelling}");
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
                    //DebugPrint($"unhandled return type: {declFunc.ReturnType} ({declFunc.ReturnType.Kind}, {declFunc.ReturnType.GetType().Name})");
                    //returnTypeSyntax = new SyntaxToken(SyntaxKind.TokenMissing, new Location(0, 0, clangFile.FileId));
                    return null;
                }

                return null;
            }
        }
    }
#endif
}
