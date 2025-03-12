using System.Text;

namespace Choir.LibLLVM.SourceGenerator;

internal sealed class LLVMHeaderParser
{
    private TextReader Reader { get; }

    public bool IsAtEnd => CurrentLine is null;

    private string? CurrentLine { get; set; }
    private int LineNumber { get; set; }

    internal LLVMHeaderParser(TextReader reader)
    {
        Reader = reader;
        AdvanceLine();
    }

    private void AdvanceLine()
    {
        CurrentLine = Reader.ReadLine();
        LineNumber++;
    }

    private void ExpectLine(string lineText)
    {
        if (CurrentLine != lineText)
            throw new InvalidDataException($"Expected the line '{lineText}', but got '{(CurrentLine ?? "<eof>")}'.");
        AdvanceLine();
    }

    private void SkipBlankLines()
    {
        // also skip preprocessor directives and trivial non-.doc-comments
        // some other comments will need to be special cases.
        while (!IsAtEnd && (string.IsNullOrWhiteSpace(CurrentLine) || CurrentLine.StartsWith('#') || CurrentLine.StartsWith("///") || CurrentLine.StartsWith("  /*")))
            AdvanceLine();
    }

    private string[] ParseBlockComment(string? commentStart = null, bool trustMeBro = false)
    {
        if (IsAtEnd) return [];

        if (!trustMeBro)
        {
            string checkLine = CurrentLine!.TrimStart();
            if (!checkLine.StartsWith("/*"))
                throw new InvalidDataException("Expected a line to begin a block comment, but it didn't.");
        }

        var list = new List<string>();
        while (!IsAtEnd)
        {
            string currentLine = CurrentLine!;
            if (list.Count == 0)
            {
                if (commentStart is not null && currentLine.StartsWith(commentStart))
                    currentLine = currentLine.Substring(commentStart.Length);
            }

            string lineText = currentLine.TrimStart().TrimStart('/', '*').TrimEnd().TrimEnd('/', '*').Trim();
            AdvanceLine();

            if (list.Count != 0 || !string.IsNullOrWhiteSpace(lineText))
                list.Add(lineText);

            if (currentLine!.TrimEnd().EndsWith("*/"))
                break;
        }

        return [.. list.SkipWhile(string.IsNullOrWhiteSpace).Reverse().SkipWhile(string.IsNullOrWhiteSpace).Reverse()];
    }

    internal void SkipPreamble()
    {
        // just in case there's whitespace at the top of the file.
        SkipBlankLines();

        // skip the doc comment/license.
        // TODO(local): maybe parse out the second half of this comment so its description is available.
        while (!IsAtEnd && (CurrentLine!.StartsWith("/*") || CurrentLine!.StartsWith("|*") || CurrentLine!.StartsWith("\\*")))
            AdvanceLine();

        // skip the blank lines, including preprocessor directives.
        SkipBlankLines();

        ExpectLine("LLVM_C_EXTERN_C_BEGIN");
        SkipBlankLines();
    }

    internal LLVMParsedHeaderEntity? ParseEntity()
    {
        SkipBlankLines();

        if (IsAtEnd)
            throw new InvalidOperationException("Cannot parse an entity at the end of the document.");

        int lineNumber = LineNumber;

        string[] docs = [];
        if (CurrentLine!.Trim().StartsWith("/*"))
        {
            docs = ParseBlockComment();
            if (docs.Length == 0)
                throw new InvalidOperationException("There is no doc comment to start an entity.");
        }

        if (CurrentLine!.StartsWith("typedef enum") || CurrentLine.StartsWith("enum"))
            return ParseEnumEntity(docs);

        if (CurrentLine.TrimStart().StartsWith("typedef unsigned") && CurrentLine.TrimEnd().EndsWith(';'))
        {
            string lineText = CurrentLine.Trim();
            AdvanceLine();

            return new LLVMParsedTypedef(lineNumber, docs, lineText.Substring(16, lineText.Length - 17).Trim());
        }

        if (docs[0].StartsWith("@defgroup"))
        {
            string groupDescription = docs[0].Substring("@defgroup".Length).Trim();
            docs = [.. docs.Skip(1).SkipWhile(string.IsNullOrWhiteSpace)];

            if (docs.Length > 0 && docs[docs.Length - 1].Trim() == "@{")
            {
                docs = [.. docs.Reverse().Skip(1).SkipWhile(string.IsNullOrWhiteSpace).Reverse()];
                return new LLVMParsedGroupBegin(lineNumber, docs, groupDescription);
            }

            return new LLVMParsedGroupEmpty(lineNumber, docs, groupDescription);
        }

        if (docs[0] == "@}")
        {
            return new LLVMParsedGroupEnd(lineNumber);
        }

        if (CurrentLine!.Trim().StartsWith("typedef") && CurrentLine.Contains("(*") && CurrentLine.Contains(")("))
            return ParseFunctionTypedef(docs);

        return ParseFunctionDeclaration(docs);
    }

    internal LLVMParsedHeaderEntity ParseEnumEntity(string[] enumDocs)
    {
        bool hasName = CurrentLine!.Contains("typedef");

        int lineNumber = LineNumber;
        AdvanceLine();

        var variants = new List<(string, string?, string[])>();
        while (!IsAtEnd)
        {
            string[] variantDocs = [];

            if (CurrentLine!.TrimStart().StartsWith("/*"))
            {
                bool isDocs = CurrentLine!.TrimStart().StartsWith("/**");

                string[] contents = ParseBlockComment();
                if (isDocs)
                    variantDocs = contents;
            }

            SkipBlankLines();
            if (IsAtEnd || CurrentLine!.StartsWith('}')) break;

            int docStartIndex = CurrentLine.IndexOf("/**<");

            int variantEndIndex = CurrentLine.IndexOf(',');
            if (variantEndIndex < 0)
            {
                if (docStartIndex > 0)
                    variantEndIndex = docStartIndex;
                else variantEndIndex = CurrentLine.Length;
            }

            string variantText = CurrentLine.Substring(0, variantEndIndex).Trim();
            if (docStartIndex >= 0)
            {
                CurrentLine = CurrentLine.Substring(docStartIndex);
                variantDocs = ParseBlockComment();
            }
            else AdvanceLine();

            string variantName = variantText;
            string? variantValue = null;

            if (variantText.Contains('='))
            {
                string[] pieces = variantText.Split('=');
                variantName = pieces[0].Trim();
                variantValue = pieces[1].Trim();
            }

            variants.Add((variantName, variantValue, variantDocs));
        }

        SkipBlankLines();
        if (IsAtEnd)
            throw new InvalidOperationException("Missing typedef name.");

        if (!CurrentLine!.StartsWith("} ") && !CurrentLine.EndsWith(';'))
            throw new InvalidOperationException("Malformed enum close line.");

        string enumName = hasName ? CurrentLine.Substring(2).TrimEnd(';').Trim() : "";
        AdvanceLine();

        return new LLVMParsedEnum(lineNumber, enumDocs, enumName, [.. variants]);
    }

    internal LLVMParsedHeaderEntity ParseFunctionTypedef(string[] typedefDocs)
    {
        throw new NotImplementedException();
    }

    internal LLVMParsedHeaderEntity ParseFunctionDeclaration(string[] functionDocs)
    {
        throw new NotImplementedException();
    }
}
