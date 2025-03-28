﻿using System.Diagnostics;
using System.Text;

using Choir.Front.Laye.Sema;

using LLVMSharp.Interop;

namespace Choir.Front.Laye;

public sealed class LayeModule(ChoirContext context, IEnumerable<SourceFile> sourceFiles, IEnumerable<LayeModule> dependencies)
{
    private readonly List<SemaDeclNamed> _declarations = [];
    private readonly List<string> _linkLibraries = [];

    public ChoirContext Context { get; } = context;
    public IReadOnlyList<SourceFile> SourceFiles = [.. sourceFiles];
    public IReadOnlyList<LayeModule> Dependencies = [.. dependencies];

    public string ModuleName { get; set; } = LayeConstants.ProgramModuleName;

    public Scope ModuleScope { get; } = new();
    public Scope ExportScope { get; } = new();

    public IReadOnlyList<SemaDeclNamed> Declarations => _declarations;
    public IReadOnlyList<SemaDeclNamed> ExportedDeclarations => [.. ExportScope.SelectMany(s => s.Symbols).Select(s => s)];
    public IReadOnlyList<string> LinkLibraries => _linkLibraries;

    public void AddDecl(SemaDeclNamed decl)
    {
        _declarations.Add(decl);
    }

    public void AddLinkLibrary(string libraryFileName)
    {
        _linkLibraries.Add(libraryFileName);
    }

    public byte[] Serialize() => ModuleSerializer.SerializeToBytes(Context, this);
    public void SerializeToStream(Stream stream) => ModuleSerializer.SerializeToStream(Context, this, stream);

    private static UnmanagedMemoryStream GetModuleDataStreamFromObjectFile(ChoirContext context, FileInfo objectFileInfo)
    {
        unsafe
        {
            byte[] objectFilePathBytes = Encoding.UTF8.GetBytes(objectFileInfo.FullName + '\0');

            LLVMObjectFileRef objectFile;
            fixed (byte* pathBytes = objectFilePathBytes)
            {
                LLVMOpaqueMemoryBuffer* memoryBufferOpaque;
                sbyte* outMessage;
                int result = LLVM.CreateMemoryBufferWithContentsOfFile((sbyte*)pathBytes, &memoryBufferOpaque, &outMessage);
                if (0 != result)
                {
                    context.Assert(false, new string(outMessage));
                    throw new UnreachableException();
                }

                objectFile = LLVM.CreateObjectFile(memoryBufferOpaque);
            }

            var sectionIterator = LLVM.GetSections(objectFile);
            while (1 != LLVM.IsSectionIteratorAtEnd(objectFile, sectionIterator))
            {
                string sectionName = new(LLVM.GetSectionName(sectionIterator));
                if (!sectionName.StartsWith(LayeConstants.ModuleSectionNamePrefix))
                {
                    LLVM.MoveToNextSection(sectionIterator);
                    continue;
                }

                string? expectedModuleName = null;
                if (sectionName != LayeConstants.ModuleSectionNamePrefix)
                    expectedModuleName = sectionName[(LayeConstants.ModuleSectionNamePrefix.Length + 1)..];

                sbyte* sectionContentsPtr = LLVM.GetSectionContents(sectionIterator);
                ulong sectionContentsLength = LLVM.GetSectionSize(sectionIterator);

                return new UnmanagedMemoryStream((byte*)sectionContentsPtr, (long)sectionContentsLength);
            }
        }

        context.Assert(false, $"Could not find valid Laye module section in object file '{objectFileInfo.FullName}'");
        throw new UnreachableException();
    }

    public static SerializedModuleHeader DeserializeHeaderFromObject(ChoirContext context, FileInfo objectFileInfo)
    {
        using var stream = GetModuleDataStreamFromObjectFile(context, objectFileInfo);
        return ModuleDeserializer.DeserializeHeaderFromStream(context, stream);
    }

    public static LayeModule DeserializeFromObject(ChoirContext context, LayeModule[] dependencies, FileInfo objectFileInfo)
    {
        using var stream = GetModuleDataStreamFromObjectFile(context, objectFileInfo);
        return ModuleDeserializer.DeserializeFromStream(context, dependencies, stream);
    }
}
