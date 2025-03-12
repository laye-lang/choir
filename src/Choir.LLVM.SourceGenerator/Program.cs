using System.Text;
using System.Windows.Markup;

using ClangSharp;
using ClangSharp.Interop;

namespace Choir.LibLLVM.SourceGenerator;

internal static class Program
{
    private static readonly string[] HeaderFileNames = [
        "Core.h"
    ];

    static void Main(string[] args)
    {
        //const string LLVMCAPIHeaderRootPath = @"\\wsl.localhost\Debian\home\nashiora\dev\llvm-project\llvm\include\llvm-c";

        if (args.Length != 2)
        {
            Console.Error.WriteLine($"usage: {Environment.ProcessPath} <llvm-c header path> <output root path>");
            Environment.Exit(1);
        }

        string llvmcHeaderRootPath = args[0];
        string outputRootPath = args[1];

        var generatedFiles = new Dictionary<string, string>();
        var enumFilePaths = new Dictionary<EnumDecl, string>();
        var delegateFilePaths = new Dictionary<ClangSharp.Type, string>();

        foreach (string headerName in HeaderFileNames)
        {
            //Console.Error.WriteLine($"Parsing LLVM C-API Header File: '{headerName}'");

            string headerFilePath = Path.Combine(llvmcHeaderRootPath, headerName);
            if (!File.Exists(headerFilePath))
                Console.Error.WriteLine("  Header file does not exist, skipping.");

            //var headerData = LLVMHeaderData.FromFile(headerFilePath);

            var index = ClangSharp.Index.Create(false, false);
            var tu = TranslationUnit.GetOrCreate(CXTranslationUnit.Parse(index.Handle, headerFilePath, [], [], CXTranslationUnit_Flags.CXTranslationUnit_None));

            var ffiBuilder = new StringBuilder();
            ffiBuilder.AppendLine($"/// This file was generated from 'llvm-c/{headerName}' in the LLVM C API.");
            ffiBuilder.AppendLine();

            ffiBuilder.AppendLine("using System.Runtime.InteropServices;");
            ffiBuilder.AppendLine();
            ffiBuilder.AppendLine("using LLVMBool = int;");
            ffiBuilder.AppendLine();

            ffiBuilder.AppendLine("namespace Choir.LibLLVM.Interop;");
            ffiBuilder.AppendLine();

            ffiBuilder.AppendLine("public static partial class LLVM");
            ffiBuilder.AppendLine("{");

            string libraryVarName = $"LLVM{Path.GetFileNameWithoutExtension(headerName)}";
            ffiBuilder.AppendLine($"    private const string {libraryVarName} = \"{libraryVarName}\";");

            EnumDecl? lastEnum = null;
            foreach (var decl in tu.TranslationUnitDecl.Decls)
            {
                decl.Location.GetSpellingLocation(out var file, out uint line, out uint column, out uint offset);

                if (file.Name.CString == headerFilePath)
                {
                    Generate(decl);

                    void Generate(Decl decl, string? overrideSpelling = null)
                    {
                        switch (decl)
                        {
                            case FunctionDecl fnDecl:
                            {
                                ffiBuilder.AppendLine();
                                ffiBuilder.AppendLine($"    [DllImport({libraryVarName}, EntryPoint = \"{fnDecl.Name}\", CallingConvention = CallingConvention.Cdecl)]");

                                if (fnDecl.Name == "LLVMDisposeMessage")
                                    ffiBuilder.AppendLine($"    public static extern void DisposeMessage(IntPtr Message);");
                                else
                                {
                                    string returnTypeString = GenerateTypeString(fnDecl.ReturnType, out string? returnAttributes, true);
                                    if (returnAttributes is not null)
                                        ffiBuilder.Append("    [return: ").Append(returnAttributes).AppendLine("]");

                                    ffiBuilder.Append($"    public static extern {returnTypeString} {fnDecl.Name[4..]}(");
                                    for (int i = 0; i < fnDecl.Parameters.Count; i++)
                                    {
                                        var param = fnDecl.Parameters[i];
                                        if (i > 0)
                                            ffiBuilder.Append(", ");

                                        string paramTypeString = GenerateTypeString(param.Type, out string? paramAttributes);
                                        if (paramAttributes is not null)
                                            ffiBuilder.Append('[').Append(paramAttributes).Append("] ");

                                        ffiBuilder.Append(paramTypeString);

                                        if (string.IsNullOrEmpty(param.Name))
                                            ffiBuilder.Append($" Param{i}");
                                        else ffiBuilder.Append($" {param.Name}");
                                    }

                                    ffiBuilder.AppendLine(");");
                                }
                            } break;

                            case EnumDecl enumDecl:
                            {
                                if (overrideSpelling is null && decl.Spelling.Contains("unnamed"))
                                {
                                    lastEnum = (EnumDecl)decl;
                                    return;
                                }

                                string spelling = overrideSpelling ?? decl.Spelling;

                                var enumBuilder = new StringBuilder();
                                enumBuilder.AppendLine($"/// This file was generated from 'llvm-c/{headerName}' in the LLVM C API.");
                                enumBuilder.AppendLine();

                                enumBuilder.AppendLine("namespace Choir.LibLLVM;");
                                enumBuilder.AppendLine();

                                enumBuilder.AppendLine($"public enum {spelling}");
                                enumBuilder.AppendLine("{");

                                foreach (var variant in enumDecl.Decls.Cast<EnumConstantDecl>())
                                {
                                    string variantName = variant.Name;
                                    if (variantName.StartsWith("LLVM"))
                                        variantName = variantName.Substring(4);

                                    enumBuilder.Append("    ");
                                    enumBuilder.Append(variantName);
                                    enumBuilder.Append(" = ");
                                    enumBuilder.Append(variant.InitVal);
                                    enumBuilder.AppendLine(",");
                                }

                                enumBuilder.AppendLine("}");

                                string enumFilePath = $"{spelling}.cs";
                                string enumSourceText = enumBuilder.ToString();

                                generatedFiles[enumFilePath] = enumSourceText;
                                enumFilePaths[enumDecl] = enumFilePath;
                            } break;

                            case TypedefDecl typedefDecl:
                            {
                                var underlyingType = typedefDecl.UnderlyingType;
                                if (underlyingType is ElaboratedType underlyingElaboratedType)
                                {
                                    if (underlyingElaboratedType.OwnedTagDecl is EnumDecl enumDecl)
                                    {
                                        if (enumFilePaths.ContainsKey(enumDecl))
                                            return; // don't re-generate the type, it's already handled

                                        Generate(underlyingElaboratedType.OwnedTagDecl!, typedefDecl.Name);
                                        return;
                                    }
                                }
                                else if (underlyingType is BuiltinType { Kind: CXTypeKind.CXType_UInt })
                                {
                                    if (lastEnum is not null)
                                    {
                                        Generate(lastEnum, typedefDecl.Name);
                                        lastEnum = null;
                                        return;
                                    }
                                }

                                Console.Error.WriteLine($"Unhandled Typedef underlying type: {underlyingType} ({underlyingType.GetType().FullName})");
                            } break;
                        }
                    }

                    string GenerateTypeString(ClangSharp.Type type, out string? attributes, bool isReturn = false)
                    {
                        attributes = null;

                        var canonicalType = type.CanonicalType;
                        bool isCanonical = type == canonicalType;

                        switch (type)
                        {
                            case BuiltinType { Kind: CXTypeKind.CXType_Void }: return "void";
                            case BuiltinType { Kind: CXTypeKind.CXType_Char_S or CXTypeKind.CXType_SChar }: return "sbyte";
                            case BuiltinType { Kind: CXTypeKind.CXType_Char_U or CXTypeKind.CXType_UChar }: return "byte";
                            case BuiltinType { Kind: CXTypeKind.CXType_Int }: return "int";
                            case BuiltinType { Kind: CXTypeKind.CXType_UInt }: return "uint";
                            case BuiltinType { Kind: CXTypeKind.CXType_Long or CXTypeKind.CXType_LongLong }: return "long";
                            case BuiltinType { Kind: CXTypeKind.CXType_ULong or CXTypeKind.CXType_ULongLong }: return "ulong";
                            case BuiltinType { Kind: CXTypeKind.CXType_Double }: return "double";

                            case PointerType { PointeeType: BuiltinType { Kind: CXTypeKind.CXType_Void } }:
                                return "IntPtr";

                            case PointerType { PointeeType: BuiltinType { Kind: CXTypeKind.CXType_Char_S or CXTypeKind.CXType_Char_U } }:
                            {
                                attributes = "MarshalAs(UnmanagedType.LPStr)";
                                return "string";
                            }

                            case ElaboratedType { AsString: "LLVMBool" }:
                                return "LLVMBool";

                            case ElaboratedType elaborated when elaborated.AsString.EndsWith("Ref"):
                                return "IntPtr";

                            case EnumType enumType:
                                return enumType.AsString;

                            case PointerType when isCanonical:
                                return "IntPtr";
                        }

                        if (!isCanonical)
                            return GenerateTypeString(canonicalType, out attributes, isReturn);

                        Console.Error.WriteLine($"Unhandled type in {nameof(GenerateTypeString)}: {type} ({type.GetType().FullName})");
                        return "IntPtr";
                    }
                }
                else if (file.Name.CString.Contains("llvm-c"))
                {
                }
            }

            ffiBuilder.AppendLine("}");

            string sourceText = ffiBuilder.ToString();
            generatedFiles[$"Interop/{libraryVarName}.cs"] = sourceText;

            if (!Directory.Exists(outputRootPath))
                Directory.CreateDirectory(outputRootPath);

            foreach (var (filePath, contents) in generatedFiles)
            {
                string fullPath = Path.Combine(outputRootPath, filePath);

                var directory = Directory.GetParent(fullPath)!;
                if (!directory.Exists)
                    directory.Create();

                File.WriteAllText(fullPath, contents);
            }

            //Console.Error.WriteLine(sourceText);
        }
    }
}
