
// https://github.com/dotnet/ClangSharp/blob/main/tests/ClangSharp.UnitTests/CXTranslationUnitTest.cs
using ClangSharp;
using ClangSharp.Interop;
using static ClangSharp.Interop.CXTranslationUnit_Flags;

class Test
{
    public static void TestTest()
    {
        var name = "basic";
        var dir = Path.GetRandomFileName();
        _ = Directory.CreateDirectory(dir);

        try
        {
            // Create a file with the right name
            var file = new FileInfo(Path.Combine(dir, name + ".c"));
            File.WriteAllText(file.FullName, "int main() { return 0; }");

            using var index = CXIndex.Create();
            var translationUnit = CXTranslationUnit.Parse(
                index, file.FullName, Array.Empty<string>(),
                Array.Empty<CXUnsavedFile>(), CXTranslationUnit_None);
            var clangFile = translationUnit.GetFile(file.FullName);

            using var tu = TranslationUnit.GetOrCreate(translationUnit);
            foreach (var decl in tu.TranslationUnitDecl.Decls)
            {
                if (decl is FunctionDecl funcDecl) {
                    Console.WriteLine($"function: {funcDecl.Name}");
                } else {
                    Console.WriteLine(decl.Spelling);
                }
            }
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
