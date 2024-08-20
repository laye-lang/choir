using Choir.Front.Laye.Syntax;

namespace Choir.Front.Laye.Sema;

public partial class Sema
{
    public static void Analyse(TranslationUnit tu)
    {
        // copy out all modules, since we're going to be modifying the translation unit
        var modules = tu.Modules.ToArray();
        foreach (var module in modules)
            Analyse(module);
    }

    public static void Analyse(Module module)
    {
        module.Context.Assert(module.TranslationUnit is not null, "module is not part of a translation unit");
        if (module.HasSemaDecls) return;

        var sema = new Sema(module);
        sema.ResolveModuleImports();
    }

    private static bool IsImportDeclForCHeader(SyntaxDeclImport importDecl)
    {
        return importDecl.ImportKind == ImportKind.FilePath && importDecl.ModuleNameText.EndsWith(".h", StringComparison.InvariantCultureIgnoreCase);
    }

    public Module Module { get; }
    public ChoirContext Context { get; }
    public TranslationUnit TranslationUnit { get; }

    private Sema(Module module)
    {
        Module = module;
        Context = module.Context;
        TranslationUnit = module.TranslationUnit!;
    }

    private void ResolveModuleImports()
    {
        foreach (var topLevelSyntax in Module.TopLevelSyntax)
            ProcessTopLevelSyntax(topLevelSyntax);

        void ProcessTopLevelSyntax(SyntaxNode topLevelSyntax)
        {
            if (topLevelSyntax is SyntaxDeclImport importDecl)
                ResolveModuleImport(importDecl);
        }

        void ResolveModuleImport(SyntaxDeclImport importDecl)
        {
            if (IsImportDeclForCHeader(importDecl))
            {
                ParseCHeaderFromImport(importDecl);
                return;
            }

            if (importDecl.IsLibraryModule)
            {
                Context.Assert(false, importDecl.TokenModuleName.Location, "Library imports are currently not supported.");
                return;
            }

            var importedFileInfo = Context.LookupFile(importDecl.ModuleNameText, Module.SourceFile.FileInfo.Directory, FileLookupLocations.IncludeDirectories);
            if (importedFileInfo is null)
            {
                Context.Diag.Error(importDecl.TokenModuleName.Location, $"could not find Laye module '{importDecl.ModuleNameText}'");
                return;
            }

            var importedFile = Context.GetSourceFile(importedFileInfo);
            if (TranslationUnit.FindModuleBySourceFile(importedFile) is not {} importedModule)
            {
                importedModule = new Module(importedFile);
                TranslationUnit.AddModule(importedModule);
                Lexer.ReadTokens(importedModule);
                Parser.ParseSyntax(importedModule);
            }
            
            if (Context.HasIssuedError) return;
            Analyse(importedModule);
        }
    }
}
