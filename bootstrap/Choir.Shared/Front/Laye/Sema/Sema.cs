using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Choir.CommandLine;
using Choir.Front.Laye.Syntax;

namespace Choir.Front.Laye.Sema;

#pragma warning disable CA1822 // Mark members as static
public partial class Sema
{
    public static void AnalyseModule(LayeModule module, SyntaxDeclModuleUnit[] unitDecls)
    {
        var context = module.Context;

        var sema = new Sema(module);
        sema.ResolveModuleImports(unitDecls);

        foreach (var unitDecl in unitDecls)
        {
            context.Assert(sema._fileImports.ContainsKey(unitDecl.SourceFile), $"No file import table found for this unit's source file ('{unitDecl.SourceFile.FilePath}').");
            context.Assert(sema._fileScopes.ContainsKey(unitDecl.SourceFile), $"No file scope found for this unit's source file ('{unitDecl.SourceFile.FilePath}').");
            context.Assert(sema._scopeStack.Count == 1 && sema.CurrentScope == module.ModuleScope, "Sema should be at module scope right now.");

            var fileScope = sema._fileScopes[unitDecl.SourceFile];
            using var _ = sema.EnterScope(fileScope);

            sema._currentFileImports = sema._fileImports[unitDecl.SourceFile];

            foreach (var topLevelNode in unitDecl.TopLevelDeclarations)
            {
                if (sema.ForwardDeclareIfAllowedOutOfOrder(topLevelNode, out var declNamed, sema.ModuleScope))
                {
                    if (declNamed.Linkage == Linkage.Exported)
                    {
                        // We're not checking the result of this call here, since it *should* produce the same results
                        // as the forward declaration to module scope.
                        // when it doesn't it should be because it was more permissive, not conflicting with other exports only.
                        bool __ = module.ExportScope.AddDecl(declNamed);
                    }
                }
            }

            sema._currentFileImports = [];
        }

        if (module.Context.HasIssuedError) return;

        foreach (var unitDecl in unitDecls)
        {
            context.Assert(sema._fileImports.ContainsKey(unitDecl.SourceFile), $"No file import table found for this unit's source file ('{unitDecl.SourceFile.FilePath}').");
            context.Assert(sema._fileScopes.ContainsKey(unitDecl.SourceFile), $"No file scope found for this unit's source file ('{unitDecl.SourceFile.FilePath}').");
            context.Assert(sema._scopeStack.Count == 1 && sema.CurrentScope == module.ModuleScope, "Sema should be at module scope right now.");

            var fileScope = sema._fileScopes[unitDecl.SourceFile];
            using var _ = sema.EnterScope(fileScope);

            sema._currentFileImports = sema._fileImports[unitDecl.SourceFile];

            foreach (var topLevelNode in unitDecl.TopLevelDeclarations)
            {
                var decl = sema.AnalyseTopLevelDecl(topLevelNode);

                if (decl is SemaDeclFunction declFunction && declFunction.Name == "main" &&
                    declFunction.ReturnType.CanonicalType.Type == context.Types.LayeTypeInt &&
                    declFunction.ParameterDecls.Count == 0 &&
                    declFunction.Body is not null &&
                    module.ModuleName == LayeConstants.ProgramModuleName)
                {
                    if (declFunction.Linkage is not Linkage.Internal and not Linkage.Exported)
                        module.Context.Diag.Error("The 'main' function must either be defined with no linkage or as 'export'.");

                    declFunction.Linkage = Linkage.Exported;
                }

                if (decl is SemaDeclNamed declNamed)
                    module.AddDecl(declNamed);
            }

            sema._currentFileImports = [];
        }
    }

    public LayeModule Module { get; }
    public ChoirContext Context { get; }
    public Colors Colors { get; }

    private readonly Dictionary<SourceFile, Dictionary<string, Scope>> _fileImports = [];
    private readonly Dictionary<SourceFile, Scope> _fileScopes = [];
    private readonly Dictionary<SyntaxNode, SemaDeclNamed> _forwardDeclNodes = [];
    private readonly Stack<Scope> _scopeStack = [];
    private readonly Stack<SemaDeclFunction> _functionStack = [];

    private Dictionary<string, Scope> _currentFileImports = [];

    private Scope ModuleScope => Module.ModuleScope;
    private Scope CurrentScope => _scopeStack.Peek();
    private SemaDeclFunction CurrentFunction
    {
        get
        {
            if (_functionStack.TryPeek(out var function))
                return function;

            Context.Diag.ICE("Attempt to access the current function during sema from outside any function.");
            throw new UnreachableException();
        }
    }

    private Sema(LayeModule module)
    {
        Module = module;
        Context = module.Context;
        Colors = new(module.Context.UseColor);

        _scopeStack.Push(module.ModuleScope);
        foreach (var sourceFile in module.SourceFiles)
            _fileScopes[sourceFile] = new(module.ModuleScope);
    }

    private void ResolveModuleImports(SyntaxDeclModuleUnit[] unitDecls)
    {
        foreach (var unitDecl in unitDecls)
        {
            var sourceFile = unitDecl.SourceFile;
            var importScopes = _fileImports[sourceFile] = [];

            foreach (var importDecl in unitDecl.Header.ImportDeclarations)
            {
                var referencedModule = Module.Dependencies.Where(m => m.ModuleName == importDecl.ModuleNameText).SingleOrDefault();
                if (referencedModule is null)
                {
                    Context.Diag.Error(importDecl.TokenModuleName.Location, $"Module '{importDecl.ModuleNameText}' not found.");
                    continue;
                }

                if (importDecl.Queries.Count == 0)
                {
                    string scopeName = importDecl.IsAliased ? importDecl.AliasNameText : importDecl.ModuleNameText;
                    importScopes[scopeName] = referencedModule.ExportScope;
                }
                else if (importDecl.Queries.Any(q => q is SyntaxImportQueryWildcard))
                {
                    if (importDecl.Queries.Count != 1)
                    {
                        Context.Diag.Error(importDecl.Location, "An import declaration cannot have both a wildcard '*' and named queries.");
                        Context.Diag.Note(importDecl.Location, "This may change if there is a good enough reason, but it is currently restricted.");
                    }

                    var scope = importDecl.IsExported ? Module.ExportScope : Module.ModuleScope;
                    Context.Assert(!importDecl.IsExported, importDecl.Location, "We need to be careful about how 'export import' works, so it is currently not allowed.");

                    foreach (var (syntaxName, exportedDecls) in referencedModule.ExportScope)
                    {
                        foreach (var decl in exportedDecls)
                            scope.AddDecl(decl);
                    }
                }
                else
                {
                    Context.Todo(importDecl.Location, "If import queries are used, only the wildcard '*' is currently supported.");
                }
            }
        }

#if false
        foreach (var topLevelSyntax in OldModule.TopLevelSyntax)
            ProcessTopLevelSyntax(topLevelSyntax);

        void ProcessTopLevelSyntax(SyntaxNode topLevelSyntax)
        {
            if (topLevelSyntax is SyntaxDeclImport importDecl)
                ResolveModuleImport(importDecl);
        }

        void ResolveModuleImport(SyntaxDeclImport importDecl)
        {
            Context.Assert(false, importDecl.TokenModuleName.Location, "Library imports are currently not supported.");
            return;

#if false
            var importedFileInfo = Context.LookupFile(importDecl.ModuleNameText, Module.SourceFile.FileInfo.Directory, FileLookupLocations.IncludeDirectories);
            if (importedFileInfo is null)
            {
                Context.Diag.Error(importDecl.TokenModuleName.Location, $"could not find Laye module '{importDecl.ModuleNameText}'");
                return;
            }

            OldModule importedModule;
            var importedFile = Context.GetSourceFile(importedFileInfo);
            if (TranslationUnit.FindModuleBySourceFile(importedFile) is { } importedModuleResult)
                importedModule = importedModuleResult;
            else
            {
                importedModule = new OldModule(importedFile);
                TranslationUnit.AddModule(importedModule);
                Lexer.ReadTokens(importedModule);
                Parser.ParseSyntax(importedModule);
            }

            importDecl.ReferencedModule = importedModule;
            if (Context.HasIssuedError) return;

            Analyse(importedModule);

            if (importDecl.IsAliased)
            {
                Module.FileScope.AddNamespace(importDecl.AliasNameText, importedModule.ExportScope);
                if (importDecl.IsExported)
                    Module.ExportScope.AddNamespace(importDecl.AliasNameText, importedModule.ExportScope);

                if (importDecl.Queries.Count != 0)
                {
                    Context.Diag.Error(importDecl.Location, "import declarations can be aliased or have queries, but not both");
                }
            }
            else if (importDecl.Queries.Count == 0)
            {
                string scopeName = Path.GetFileNameWithoutExtension(importDecl.ModuleNameText);
                Module.FileScope.AddNamespace(scopeName, importedModule.ExportScope);
                if (importDecl.IsExported)
                    Module.ExportScope.AddNamespace(scopeName, importedModule.ExportScope);
            }

            foreach (var query in importDecl.Queries)
            {
                if (query is SyntaxImportQueryWildcard queryWildcard)
                {
                    foreach (var (symbolName, symbols) in importedModule.ExportScope)
                    {
                        foreach (var symbol in symbols)
                        {
                            Module.FileScope.AddSymbol(symbolName, symbol);
                            if (importDecl.IsExported)
                                Module.ExportScope.AddSymbol(symbolName, symbol);
                        }
                    }
                }
                else if (query is SyntaxImportQueryNamed queryNamed)
                {
                    if (queryNamed.Query.NamerefKind != NamerefKind.Default)
                    {
                        Context.Diag.Error(queryNamed.Location, "invalid path lookup in import context");
                        continue;
                    }

                    Scope? scope = importedModule.ExportScope;
                    for (int i = 0; i < queryNamed.Query.Names.Count - 1; i++)
                    {
                        var queryName = queryNamed.Query.Names[i];
                        if (queryName is not SyntaxToken queryToken || queryToken.Kind != TokenKind.Identifier)
                        {
                            Context.Diag.Error(queryName.Location, "invalid path name part in import context");
                            scope = null;
                            break;
                        }

                        var queryResults = scope.GetSymbols(queryToken.TextValue);
                        if (queryResults.Count == 0)
                        {
                            Context.Diag.Error(queryName.Location, $"could not find name '{queryToken.TextValue}' in this context");
                            scope = null;
                            break;
                        }

                        if (queryResults.Count != 1)
                        {
                            Context.Diag.Error(queryName.Location, $"name '{queryToken.TextValue}' does not resolve to a single namespace in this context");
                            scope = null;
                            break;
                        }

                        var queryResult = queryResults[0];
                        if (queryResult is not NamespaceSymbol namespaceSymbol)
                        {
                            Context.Diag.Error(queryName.Location, $"name '{queryToken.TextValue}' does not resolve to a namespace in this context");
                            scope = null;
                            break;
                        }
                    }

                    if (scope is null)
                    {
                        continue;
                    }

                    string symbolName = ImportQueryNamePartToSymbolName(queryNamed.Query.Names[^1]);
                    var symbols = scope.GetSymbols(symbolName);

                    foreach (var symbol in symbols)
                    {
                        Module.FileScope.AddSymbol(symbolName, symbol);
                        if (importDecl.IsExported)
                            Module.ExportScope.AddSymbol(symbolName, symbol);
                    }
                }
                else
                {
                    Context.Unreachable($"an unhandled/unknown import query was encountered: {query.GetType().Name}");
                    throw new UnreachableException();
                }
            }

            string ImportQueryNamePartToSymbolName(SyntaxNode queryName)
            {
                if (queryName is SyntaxToken queryToken && queryToken.Kind == TokenKind.Identifier)
                    return queryToken.TextValue;
                else if (queryName is SyntaxOperatorName operatorName)
                    return TransformOperatorNameToSymbolName(operatorName);

                Context.Unreachable("a name part must always be an identifier token or operator name instance");
                throw new UnreachableException();
            }
#endif
        }
#endif
    }

    private string TransformOperatorNameToSymbolName(SyntaxOperatorName operatorName)
    {
        switch (operatorName)
        {
            default:
            {
                Context.Unreachable("unhandled operator name type");
                throw new UnreachableException();
            }

            case SyntaxOperatorSimple simple: return $"operator {simple.TokenOperator.Kind.CanonicalOperatorName()}";
            case SyntaxOperatorDelete: return $"operator delete";
            case SyntaxOperatorDeleteArray: return $"operator delete[]";
            case SyntaxOperatorNew: return $"operator new";
            case SyntaxOperatorNewArray: return $"operator new[]";
            case SyntaxOperatorCast @cast:
            {
                var type = AnalyseType(@cast.Type);
                if (type.IsPoison) return "operator cast(poison)";
                return $"operator cast(type {type.Id})";
            }
        }
    }

    private void SetForeignStatusForDeclaration(SemaDeclNamed declNamed, IReadOnlyList<SyntaxAttrib> attribs)
    {
        foreach (var attrib in attribs)
        {
            switch (attrib)
            {
                case SyntaxAttribForeign attribForeign:
                {
                    if (declNamed.IsForeign)
                    {
                        Context.Diag.Error(attrib.Location, "Duplicate `foreign` attribute.");
                        break;
                    }

                    declNamed.IsForeign = true;
                    if (attribForeign.HasForeignName)
                        declNamed.ForeignSymbolName = attribForeign.ForeignNameText;
                    else declNamed.ForeignSymbolName = declNamed.Name;
                } break;
            }
        }
    }

    private Linkage GetLinkageFromAttributeList(IReadOnlyList<SyntaxAttrib> attribs)
    {
        var exportAttribs = attribs.Where(attrib => attrib is SyntaxAttribExport).ToArray();
        if (exportAttribs.Length >= 2)
            Context.Diag.Error(exportAttribs[1].Location, "Duplicate `export` attributes.");

        if (exportAttribs.Length != 0)
            return Linkage.Exported;

        return Linkage.Internal;
    }

    private bool ForwardDeclareIfAllowedOutOfOrder(SyntaxNode node, [NotNullWhen(true)] out SemaDeclNamed? forwardDecl, Scope? scope = null)
    {
        forwardDecl = null;

        var declaringScope = scope ?? CurrentScope;
        bool isAtModuleScope = declaringScope == ModuleScope;

        IReadOnlyList<SyntaxAttrib> attribs;

        switch (node)
        {
            default: return false;

            case SyntaxDeclAlias declAlias:
            {
                attribs = declAlias.Attribs;
                forwardDecl = new SemaDeclAlias(declAlias.Location, declAlias.TokenName.TextValue, declAlias.IsStrict);
            } break;

            case SyntaxDeclStruct declStruct:
            {
                attribs = declStruct.Attribs;

                var declScope = new Scope(declaringScope);
                forwardDecl = new SemaDeclStruct(declStruct.Location, declStruct.TokenName.TextValue)
                {
                    ParentStruct = null,
                    Scope = declScope,
                };
            } break;

            case SyntaxDeclEnum declEnum:
            {
                attribs = declEnum.Attribs;

                var declScope = new Scope(declaringScope);
                forwardDecl = new SemaDeclEnum(declEnum.Location, declEnum.TokenName.TextValue)
                {
                    Scope = declScope,
                };
            } break;

            case SyntaxDeclBinding declBinding:
            {
                if (!isAtModuleScope)
                    return false;

                attribs = declBinding.Attribs;
                forwardDecl = new SemaDeclBinding(declBinding.Location, declBinding.TokenName.TextValue);
            } break;

            case SyntaxDeclFunction declFunction:
            {
                // operators will have an empty name, you can't look them up normally
                string functionName = "";
                if (declFunction.Name is SyntaxToken { Kind: TokenKind.Identifier } tokenIdent)
                    functionName = tokenIdent.TextValue;

                attribs = declFunction.Attribs;
                forwardDecl = new SemaDeclFunction(declFunction.Location, functionName)
                {
                    IsDiscardable = declFunction.Attribs.Any(a => a is SyntaxAttribDiscardable),
                    IsInline = declFunction.Attribs.Any(a => a is SyntaxAttribInline),
                    VarargsKind = declFunction.VarargsKind,
                };
            } break;

            case SyntaxDeclRegister declRegister:
            {
                if (!isAtModuleScope)
                    return false;

                attribs = declRegister.Attribs;
                forwardDecl = new SemaDeclRegister(declRegister.Location, declRegister.TokenRegisterName.TextValue, declRegister.TokenDeclName.TextValue);
            } break;
        }

        Context.Assert(forwardDecl is not null, "Didn't generate the a forward declaration node.");

        forwardDecl.Linkage = GetLinkageFromAttributeList(attribs);
        SetForeignStatusForDeclaration(forwardDecl, attribs);

        if (forwardDecl is SemaDeclFunction f && f.IsForeign && f.ForeignSymbolName is null &&
            ((SyntaxDeclFunction)node).Name is SyntaxOperatorName functionNameOperator)
        {
            Context.Diag.Error(f.Location, "Operator functions cannot be marked as foreign without an explicit foreign name provided.");
        }

        _forwardDeclNodes[node] = forwardDecl;
        DeclareInScope(forwardDecl, declaringScope);

        return true;
    }

    private SemaDecl AnalyseTopLevelDecl(SyntaxNode decl)
    {
        switch (decl)
        {
            default:
            {
                Context.Unreachable($"invalid top level declaration node {decl.GetType().Name}");
                throw new UnreachableException();
            }

            case SyntaxDeclAlias or SyntaxDeclStruct or SyntaxDeclEnum or SyntaxDeclRegister or SyntaxDeclBinding or SyntaxDeclFunction:
                return (SemaDecl)AnalyseStmtOrDecl(decl);
        }
    }

    private SemaTypeQual AnalyseType(SyntaxNode type)
    {
        switch (type)
        {
            default:
            {
                Context.Assert(false, $"TODO: implement {type.GetType().Name} for {nameof(AnalyseType)}");
                throw new UnreachableException();
            }

            case SyntaxQualMut typeQualMut: return AnalyseType(typeQualMut.Inner).Qualified(typeQualMut.Location, TypeQualifiers.Mutable);
            case SyntaxTypeBuiltIn typeBuiltin: return typeBuiltin.Type.Qualified(type.Location);

            case SyntaxNameref typeNamed:
            {
                var res = LookupName(typeNamed, CurrentScope);
                if (res is LookupSuccess success)
                {
                    switch (success.Decl)
                    {
                        default:
                        {
                            Context.Diag.Error(typeNamed.Location, "This is not a type name.");
                            return SemaTypePoison.InstanceQualified;
                        }

                        case SemaDeclStruct declStruct: return new SemaTypeStruct(declStruct).Qualified(type.Location);
                        case SemaDeclEnum declEnum: return new SemaTypeEnum(declEnum).Qualified(type.Location);
                        case SemaDeclAlias declAlias: return new SemaTypeAlias(declAlias).Qualified(type.Location);
                    }
                }

                Context.Diag.Error(typeNamed.Location, "This is not a type name.");
                return SemaTypePoison.InstanceQualified;
            }

            case SyntaxIndex typeArray:
            {
                var elementType = AnalyseType(typeArray.Operand);

                var lengthExprs = new SemaExprEvaluatedConstant[typeArray.Indices.Count];
                for (int i = 0; i < lengthExprs.Length; i++)
                {
                    var expr = AnalyseExpr(typeArray.Indices[i]);
                    if (!TryEvaluate(expr, out var lengthConst))
                    {
                        Context.Diag.Error(expr.Location, "Could not evaluate this expression to a constant value.");
                        lengthExprs[i] = new SemaExprEvaluatedConstant(expr, new EvaluatedConstant(0));
                        continue;
                    }

                    if (lengthConst.Kind != EvaluatedConstantKind.Integer)
                    {
                        Context.Diag.Error(expr.Location, "The length of an array must evaluate to an integer value.");
                        lengthExprs[i] = new SemaExprEvaluatedConstant(expr, new EvaluatedConstant(0));
                        continue;
                    }

                    if (lengthConst.IntegerValue < 0 || lengthConst.IntegerValue > ulong.MaxValue)
                    {
                        Context.Diag.Error(expr.Location, $"The length of an array must evaluate to an integer value in the rage [0, {ulong.MaxValue}].");
                        lengthExprs[i] = new SemaExprEvaluatedConstant(expr, new EvaluatedConstant(0));
                        continue;
                    }

                    lengthExprs[i] = new SemaExprEvaluatedConstant(expr, lengthConst);
                }

                // TODO(local): check if dimensions are out of bounds for the underlying index type

                return new SemaTypeArray(Context, elementType, lengthExprs).Qualified(type.Location);
            }

            case SyntaxTypePointer typePointer:
                return new SemaTypePointer(Context, AnalyseType(typePointer.Inner)).Qualified(type.Location);

            case SyntaxTypeBuffer typeBuffer:
            {
                SemaExpr? terminator = null;
                if (typeBuffer.TerminatorExpr is { } terminatorExpr)
                {
                    Context.Diag.ICE("Buffer types with terminator expressions are currently not supported.");

                    terminator = AnalyseExpr(terminatorExpr);
                    if (!TryEvaluate(terminator, out var constantValue))
                    {
                        Context.Diag.Error(terminatorExpr.Location, "Could not evaluate terminator expression to a constant value");
                        goto return_the_buffer_type;
                    }

                    // TODO(local): any buffer type could syntactically have a sentinel terminator,
                    // but only a few types can have constant values represented.
                    // Check for numeric types as the primary (and probably only, for a long while)
                    // supported sentinel terminator type and validate the constant value (probably through convert functions).
                }

            return_the_buffer_type:;
                return new SemaTypeBuffer(Context, AnalyseType(typeBuffer.Inner), terminator).Qualified(type.Location);
            }

            case SyntaxTypeSlice typeSlice:
                return new SemaTypeSlice(Context, AnalyseType(typeSlice.Inner)).Qualified(type.Location);

            case SyntaxTypeNilable typeNilable:
                return new SemaTypeNilable(AnalyseType(typeNilable.Inner)).Qualified(type.Location);
        }
    }

    private SemaStmt AnalyseStmtOrDecl(SyntaxNode stmt, bool inheritCurrentScope = false)
    {
        if (stmt is SyntaxTypeBuffer or SyntaxTypeBuiltIn or SyntaxTypeNilable or SyntaxTypePointer or SyntaxTypeSlice)
        {
            Context.Assert(false, $"shouldn't ever call {nameof(AnalyseStmtOrDecl)} on nodes which can only be types; those should only exist in a type context and go through {nameof(AnalyseType)}");
            throw new UnreachableException();
        }

        switch (stmt)
        {
            default:
            {
                Context.Assert(false, $"TODO: implement {stmt.GetType().Name} for {nameof(AnalyseStmtOrDecl)}");
                throw new UnreachableException();
            }

            case SyntaxDeclAlias declAlias:
            {
                if (!_forwardDeclNodes.TryGetValue(stmt, out var semaNodeCheck))
                    Context.Unreachable("Alias declarations should have been forward declared.");

                Context.Assert(semaNodeCheck is SemaDeclAlias, declAlias.Location, "alias declaration did not have sema node of alias type");
                var semaNode = (SemaDeclAlias)semaNodeCheck;

                return AnalyseAlias(declAlias, semaNode);
            }

            case SyntaxDeclStruct declStruct:
            {
                if (!_forwardDeclNodes.TryGetValue(stmt, out var semaNodeCheck))
                    Context.Unreachable("Struct declarations should have been forward declared.");

                Context.Assert(semaNodeCheck is SemaDeclStruct, declStruct.Location, "struct declaration did not have sema node of struct type");
                var semaNode = (SemaDeclStruct)semaNodeCheck;

                AnalyseStruct(declStruct, semaNode);
                return semaNode;
            }

            case SyntaxDeclEnum declEnum:
            {
                if (!_forwardDeclNodes.TryGetValue(stmt, out var semaNodeCheck))
                    Context.Unreachable("Enum declarations should have been forward declared.");

                Context.Assert(semaNodeCheck is SemaDeclEnum, declEnum.Location, "enum declaration did not have sema node of enum type");
                var semaNode = (SemaDeclEnum)semaNodeCheck;

                return AnalyseEnum(declEnum, semaNode);
            }

            case SyntaxDeclBinding declBinding:
            {
                if (!_forwardDeclNodes.TryGetValue(stmt, out var semaNodeCheck))
                {
                    semaNodeCheck = new SemaDeclBinding(declBinding.Location, declBinding.TokenName.TextValue);
                    DeclareInScope((SemaDeclBinding)semaNodeCheck);
                }

                Context.Assert(semaNodeCheck is SemaDeclBinding, declBinding.Location, "binding declaration did not have sema node of binding type");
                var semaNode = (SemaDeclBinding)semaNodeCheck;

                bool isVar = declBinding.BindingType is SyntaxToken { Kind: TokenKind.Var };
                if (!isVar)
                {
                    semaNode.BindingType = AnalyseType(declBinding.BindingType);
                }

                if (declBinding.Initializer is { } syntaxInit)
                {
                    var typeHint = isVar ? null : semaNode.BindingType;
                    semaNode.InitialValue = AnalyseExpr(syntaxInit, typeHint);
                    if (typeHint is not null)
                        semaNode.InitialValue = ConvertOrError(semaNode.InitialValue, typeHint);
                    else semaNode.BindingType = semaNode.InitialValue.Type;
                }

                Context.Assert(semaNode.BindingType is not null, declBinding.Location, "Failed to assign a valid type to this declaration");
                return semaNode;
            }

            case SyntaxDeclFunction declFunction:
            {
                using var _s = EnterScope();

                if (!_forwardDeclNodes.TryGetValue(stmt, out var semaNodeCheck))
                    Context.Unreachable("Function declarations should have been forward declared.");

                Context.Assert(semaNodeCheck is SemaDeclFunction, declFunction.Location, "function declaration did not have sema node of function type");
                var semaNode = (SemaDeclFunction)semaNodeCheck;

                using var _f = EnterFunction(semaNode);

                semaNode.ReturnType = AnalyseType(declFunction.ReturnType);

                var paramDecls = new List<SemaDeclParam>();
                foreach (var param in declFunction.Params)
                {
                    var paramType = AnalyseType(param.ParamType);
                    var paramDecl = new SemaDeclParam(param.Location, param.TokenName.TextValue, paramType);
                    DeclareInScope(paramDecl);
                    paramDecls.Add(paramDecl);
                }

                semaNode.ParameterDecls = [.. paramDecls];

                if (declFunction.Body is SyntaxCompound bodyCompound)
                    semaNode.Body = (SemaStmtCompound)AnalyseStmtOrDecl(bodyCompound, inheritCurrentScope: true);
                else if (declFunction.Body is not null)
                {
                    Context.Assert(false, declFunction.Body.Location, $"unsupported syntax as function body: {declFunction.Body.GetType().Name}");
                    throw new UnreachableException();
                }

                return semaNode;
            }

            case SyntaxDeclRegister declRegister:
            {
                if (!_forwardDeclNodes.TryGetValue(stmt, out var semaNodeCheck))
                    Context.Unreachable("Register declarations should have been forward declared.");

                Context.Assert(semaNodeCheck is SemaDeclRegister, declRegister.Location, "register declaration did not have sema node of enum type");
                var semaNode = (SemaDeclRegister)semaNodeCheck;

                AnalyseRegister(declRegister, semaNode);
                return semaNode;
            }

            case SyntaxStmtXyzzy: return new SemaStmtXyzzy(stmt.Location);

            case SyntaxCompound stmtCompound:
            {
                var startDefer = CurrentScope.CurrentDefer;

                using var _ = EnterScope(!inheritCurrentScope);
                foreach (var node in stmtCompound.Body)
                    ForwardDeclareIfAllowedOutOfOrder(node, out var _);
                // TODO(local): handle forward declarations in compound statements
                // TODO(local): create scopes in compound statements.
                var childStatements = stmtCompound.Body.Select(node => AnalyseStmtOrDecl(node)).ToArray();

                var endDefer = CurrentScope.CurrentDefer;
                return new SemaStmtCompound(stmtCompound.Location, childStatements)
                {
                    StartDefer = startDefer,
                    EndDefer = endDefer,
                };
            }

            case SyntaxStmtReturn stmtReturn:
            {
                var currentDefer = CurrentScope.CurrentDefer;

                bool hasErroredOnNoReturn = false;
                if (CurrentFunction.ReturnType.IsNoReturn)
                {
                    hasErroredOnNoReturn = true;
                    Context.Diag.Error("Cannot return from a noreturn function.");
                }

                if (stmtReturn.Value is null)
                {
                    if (!hasErroredOnNoReturn && !CurrentFunction.ReturnType.IsVoid)
                        Context.Diag.Error("Must return a value from a non-void function.");

                    return new SemaStmtReturnVoid(stmtReturn.Location)
                    {
                        Defer = currentDefer,
                    };
                }

                if (!hasErroredOnNoReturn && CurrentFunction.ReturnType.IsVoid)
                    Context.Diag.Error("Cannot return a value from a void function.");

                var returnValue = AnalyseExpr(stmtReturn.Value, CurrentFunction.ReturnType);
                if (!CurrentFunction.ReturnType.IsVoid)
                    returnValue = ConvertOrError(returnValue, CurrentFunction.ReturnType);

                return new SemaStmtReturnValue(stmtReturn.Location, returnValue)
                {
                    Defer = currentDefer,
                };
            }

            case SyntaxStmtUnreachable: return new SemaStmtUnreachable(stmt.Location);

            case SyntaxStmtDefer stmtDefer:
            {
                var deferred = AnalyseStmtOrDecl(stmtDefer.Stmt);
                Context.Assert(deferred is not SemaDecl, deferred.Location, "Should not have parsed a declaration in defer context.");
                var semaNode = new SemaStmtDefer(stmtDefer.Location, deferred);

                var deferNode = new SemaDeferStackNode()
                {
                    Previous = CurrentScope.CurrentDefer,
                    Defer = semaNode,
                };

                CurrentScope.CurrentDefer = deferNode;

                return semaNode;
            }

            case SyntaxStmtIf stmtIf:
            {
                Context.Assert(stmtIf.Conditions.Count > 0, stmtIf.Location, "The parser gave us an `if` statement with no conditions.");
                var boolType = Context.Types.LayeTypeBool.Qualified(Location.Nowhere);

                SemaStmtIfPrimary[] conditions = new SemaStmtIfPrimary[stmtIf.Conditions.Count];
                for (int i = 0; i < stmtIf.Conditions.Count; i++)
                {
                    var condition = AnalyseExpr(stmtIf.Conditions[i].Condition, boolType);
                    condition = ConvertOrError(condition, boolType);

                    var body = AnalyseStmtOrDecl(stmtIf.Conditions[i].Body);
                    conditions[i] = new(stmtIf.Conditions[i].Location, condition, body);
                }

                SemaStmt? elseBody = null;
                if (stmtIf.ElseBody is { } syntaxElseBody)
                    elseBody = AnalyseStmtOrDecl(syntaxElseBody);

                return new SemaStmtIf(conditions, elseBody);
            }

            case SyntaxStmtWhileLoop stmtWhile: return AnalyseWhileLoop(stmtWhile);
            case SyntaxStmtForLoop stmtFor: return AnalyseForLoop(stmtFor);

            case SyntaxStmtAssign stmtAssign:
            {
                Context.Assert(stmtAssign.TokenAssignOp.Kind == TokenKind.Equal, stmtAssign.TokenAssignOp.Location, "Fancy assignments are not currently supported in sema.");

                var target = AnalyseExpr(stmtAssign.Left);
                if (!target.IsLValue && !target.IsRegister)
                    Context.Diag.Error(target.Location, $"Cannot assign to {target.ValueCategory.ToHumanString(includeArticle: true)}.");

                var value = AnalyseExpr(stmtAssign.Right, target.Type);
                if (target.IsLValue || target.IsRegister) value = ConvertOrError(value, target.Type);

                return new SemaStmtAssign(target, value);
            }

            case SyntaxStmtDiscard stmtDiscard:
            {
                var expr = AnalyseExpr(stmtDiscard.Expr);
                return new SemaStmtDiscard(stmtDiscard.Location, expr);
            }

            case SyntaxStmtAssert stmtAssert:
            {
                var exprCondition = AnalyseExpr(stmtAssert.Condition, Context.Types.LayeTypeBool.Qualified(Location.Nowhere));
                return new SemaStmtAssert(stmtAssert.Location, exprCondition, stmtAssert.TokenMessage?.TextValue ?? "Assertion failed.");
            }

            case SyntaxStmtExpr { Expr: SyntaxExprUnaryPostfix { TokenOperator.Kind: TokenKind.PlusPlus } exprPostInc } stmtPostInc:
            {
                var operand = AnalyseExpr(exprPostInc.Operand);
                if (operand.Type.IsPoison)
                    return new SemaStmtIncrement(operand);

                if (!operand.IsLValue)
                    Context.Diag.Error(operand.Location, $"Cannot increment {operand.ValueCategory.ToHumanString(includeArticle: true)}.");
                else if (!operand.Type.CanonicalType.IsNumeric && operand.Type.CanonicalType.Type is not SemaTypeBuffer)
                    Context.Diag.Error(operand.Location, $"Cannot increment a value of type {operand.Type.ToDebugString(Colors)}.");
                return new SemaStmtIncrement(operand);
            }

            case SyntaxStmtExpr { Expr: SyntaxExprUnaryPostfix { TokenOperator.Kind: TokenKind.MinusMinus } exprPostDec } stmtPostDec:
            {
                var operand = AnalyseExpr(exprPostDec.Operand);
                if (operand.Type.IsPoison)
                    return new SemaStmtDecrement(operand);

                if (!operand.IsLValue)
                    Context.Diag.Error(operand.Location, $"Cannot decrement {operand.ValueCategory.ToHumanString(includeArticle: true)}.");
                else if (!operand.Type.CanonicalType.IsNumeric && operand.Type.CanonicalType.Type is not SemaTypeBuffer)
                    Context.Diag.Error(operand.Location, $"Cannot decrement a value of type {operand.Type.ToDebugString(Colors)}.");
                return new SemaStmtDecrement(operand);
            }

            case SyntaxStmtExpr stmtExpr:
            {
                var expr = AnalyseExpr(stmtExpr.Expr);
                if (!expr.IsDiscardable)
                {
                    if (!expr.Type.IsVoid && !expr.Type.IsNoReturn && !expr.Type.IsPoison)
                        Context.Diag.Error(stmtExpr.Location, "The result of this expression is implicitly discarded. Use the `discard` keyword to mark this as intentional.");
                }

                return new SemaStmtExpr(expr);
            }
        }
    }

    private void DeclareInScope(SemaDeclNamed decl, Scope? thisScope = null)
    {
        thisScope ??= CurrentScope;
        if (!thisScope.AddDecl(decl))
            Context.Diag.Error(decl.Location, $"Redeclaration of '{decl.Name}' in a non-overloadable context.");
    }

    private SemaDeclAlias AnalyseAlias(SyntaxDeclAlias declAlias, SemaDeclAlias semaNode)
    {
        var aliasedType = AnalyseType(declAlias.Type);
        semaNode.AliasedType = aliasedType;
        return semaNode;
    }

    private void AnalyseStruct(SyntaxDeclStruct declStruct, SemaDeclStruct semaNode)
    {
        const int NOT_A_LEAF = -1;

        var fieldDecls = new SemaDeclField[declStruct.Fields.Count];
        for (int i = 0; i < declStruct.Fields.Count; i++)
        {
            var syntaxDeclField = declStruct.Fields[i];
            var fieldType = AnalyseType(syntaxDeclField.FieldType);
            fieldDecls[i] = new SemaDeclField(syntaxDeclField.Location, syntaxDeclField.TokenName.TextValue, semaNode, fieldType, i);
        }

        var variantDecls = new SemaDeclStruct[declStruct.Variants.Count];
        if (variantDecls.Length != 0)
        {
            int nLeaves = 0;
            for (int i = 0; i < declStruct.Variants.Count; i++)
            {
                var syntaxDeclVariant = declStruct.Variants[i];

                var variantScope = new Scope(semaNode.Scope);
                var variantNode = new SemaDeclStruct(syntaxDeclVariant.Location, syntaxDeclVariant.TokenName.TextValue)
                {
                    ParentStruct = semaNode,
                    Scope = variantScope,
                };

                VariantImpl(syntaxDeclVariant, variantNode, ref nLeaves);
                variantDecls[i] = variantNode;
            }
        }

        semaNode.FieldDecls = fieldDecls;
        semaNode.VariantDecls = variantDecls;

        void VariantImpl(SyntaxDeclStruct declStruct, SemaDeclStruct semaNode, ref int nLeaves)
        {
            Context.Assert(semaNode.IsVariant, "This should be a variant node.");

            // if this is a leaf variant, mark it and increase the leaf count.
            if (semaNode.IsLeaf)
            {
                // let's never let a tag be 0
                semaNode.VariantTag = nLeaves + 1;
                nLeaves += 1;
            }
            // in any other case, this is not a leaf variant.
            else
            {
                semaNode.VariantTag = NOT_A_LEAF;
            }

            var fieldDecls = new SemaDeclField[declStruct.Fields.Count];
            for (int i = 0; i < declStruct.Fields.Count; i++)
            {
                var syntaxDeclField = declStruct.Fields[i];
                var fieldType = AnalyseType(syntaxDeclField.FieldType);

                fieldDecls[i] = new SemaDeclField(syntaxDeclField.Location, syntaxDeclField.TokenName.TextValue, semaNode, fieldType, i);
            }

            var variantDecls = new SemaDeclStruct[declStruct.Variants.Count];
            if (variantDecls.Length != 0)
            {
                for (int i = 0; i < declStruct.Variants.Count; i++)
                {
                    var syntaxDeclVariant = declStruct.Variants[i];

                    var variantScope = new Scope(semaNode.Scope);
                    var variantNode = new SemaDeclStruct(syntaxDeclVariant.Location, syntaxDeclVariant.TokenName.TextValue)
                    {
                        ParentStruct = semaNode,
                        Scope = variantScope,
                    };

                    VariantImpl(syntaxDeclVariant, variantNode, ref nLeaves);
                    variantDecls[i] = variantNode;
                }
            }

            semaNode.FieldDecls = fieldDecls;
            semaNode.VariantDecls = variantDecls;
        }
    }

    private SemaDeclEnum AnalyseEnum(SyntaxDeclEnum declEnum, SemaDeclEnum semaNode)
    {
        Context.Unreachable("Enum declarations are not implemented in sema yet.");
        return semaNode;
    }

    private SemaStmtWhileLoop AnalyseWhileLoop(SyntaxStmtWhileLoop stmtWhile)
    {
        var boolType = Context.Types.LayeTypeBool.Qualified(Location.Nowhere);

        var condition = AnalyseExpr(stmtWhile.Condition, boolType);
        condition = ConvertOrError(condition, boolType);

        var body = AnalyseStmtOrDecl(stmtWhile.Body);
        var elseBody = stmtWhile.ElseBody is { } eb ? AnalyseStmtOrDecl(eb) : null;

        return new SemaStmtWhileLoop(stmtWhile.TokenWhile.Location, condition, body, elseBody);
    }

    private SemaStmtForLoop AnalyseForLoop(SyntaxStmtForLoop stmtFor)
    {
        using var _ = EnterScope();

        var boolType = Context.Types.LayeTypeBool.Qualified(Location.Nowhere);

        var initializer = stmtFor.Initializer is { } init ? AnalyseStmtOrDecl(init, true) : new SemaStmtXyzzy(Location.Nowhere);

        var condition = stmtFor.Condition is { } cond ? AnalyseExpr(cond, boolType) : new SemaExprLiteralBool(Location.Nowhere, true, boolType);
        condition = ConvertOrError(condition, boolType);
        
        var increment = stmtFor.Increment is { } inc ? AnalyseStmtOrDecl(inc, true) : new SemaStmtXyzzy(Location.Nowhere);

        var body = AnalyseStmtOrDecl(stmtFor.Body, true);

        return new SemaStmtForLoop(stmtFor.TokenFor.Location, initializer, condition, increment, body);
    }

    private void AnalyseRegister(SyntaxDeclRegister declRegister, SemaDeclRegister semaNode)
    {
        var registerType = AnalyseType(declRegister.RegisterType);
        semaNode.Type = registerType;

        if (!(registerType.IsInteger || registerType.IsFloat))
        {
            Context.Diag.Error(registerType.Location, "Register declarations must only be of integer or float types.");
            return;
        }
    }

    private SemaExpr AnalyseExpr(SyntaxNode expr, SemaTypeQual? typeHint = null)
    {
        if (expr is SyntaxTypeBuffer or SyntaxTypeBuiltIn or SyntaxTypeNilable or SyntaxTypePointer or SyntaxTypeSlice)
        {
            Context.Assert(false, $"shouldn't ever call {nameof(AnalyseExpr)} on nodes which can only be types; those should only exist in a type context and go through {nameof(AnalyseType)}");
            throw new UnreachableException();
        }

        switch (expr)
        {
            default:
            {
                Context.Assert(false, $"TODO: implement {expr.GetType().Name} for {nameof(AnalyseExpr)}");
                throw new UnreachableException();
            }

            case SyntaxNameref nameref: return AnalyseLookup(nameref, CurrentScope);
            case SyntaxExprUnaryPrefix unaryPrefix: return AnalyseUnaryPrefix(unaryPrefix, typeHint);
            case SyntaxExprUnaryPostfix unaryPostfix: return AnalyseUnaryPostfix(unaryPostfix, typeHint);
            case SyntaxExprBinary binary: return AnalyseBinary(binary, typeHint);
            case SyntaxExprCall call: return AnalyseCall(call, typeHint);
            case SyntaxExprCast cast: return AnalyseCast(cast, typeHint);
            case SyntaxExprField field: return AnalyseField(field, typeHint);
            case SyntaxIndex index: return AnalyseIndex(index, typeHint);
            case SyntaxExprConstructor ctor: return AnalyseConstructor(ctor, typeHint);
            case SyntaxGrouped grouped: return new SemaExprGrouped(grouped.Location, AnalyseExpr(grouped.Inner, typeHint));

            case SyntaxToken tokenInteger when tokenInteger.Kind == TokenKind.LiteralInteger:
            {
                SemaType intTypeUnqual;
                BigInteger intValue;

                if (tokenInteger.IntegerValue.GetBitLength() > Context.Types.MaxSupportedIntBitWidth)
                {
                    var C = new Colors(Context.UseColor);
                    Context.Diag.Error(tokenInteger.Location, "Sorry, we can't compile a number that big.");
                    Context.Diag.Note(tokenInteger.Location, $"The maximum supported integer type is {Context.Types.MaxSupportedIntBitWidth}, which is smaller than {C.LayeKeyword()}i{tokenInteger.IntegerValue.GetBitLength()}{C.Reset}, the type required to store this value.");
                    intTypeUnqual = SemaTypePoison.Instance;
                    intValue = BigInteger.Zero;
                }
                else
                {
                    intTypeUnqual = Context.Types.LayeTypeIntSized(Math.Max(1, (int)tokenInteger.IntegerValue.GetBitLength()));
                    intValue = tokenInteger.IntegerValue;
                }

                return new SemaExprLiteralInteger(tokenInteger.Location, intValue, intTypeUnqual.Qualified(tokenInteger.Location));
            }

            case SyntaxToken tokenString when tokenString.Kind == TokenKind.LiteralString:
            {
                var stringElementType = Context.Types.LayeTypeIntSized(8).Qualified(tokenString.Location);

                // NOTE(local): for now, we're always setting the type of a string literal to the `i8[]` type.
                // The type conversion check is free to change the type of a constant string when it encounters them.
#if false
                long stringByteCount = Encoding.UTF8.GetByteCount(tokenString.TextValue);

                SemaType stringType;
                // TODO(local): tightenn up the semantics of what type a string literal is.
                if (typeHint?.Type is SemaTypeArray typeHintArray && typeHintArray.ElementType.IsInteger && typeHintArray.ElementType.Size.Bits == 8)
                    stringType = Context.Types.LayeTypeArray(stringElementType, stringByteCount);
                else if (typeHint?.Type is SemaTypeBuffer typeHintBuffer && typeHintBuffer.ElementType.IsInteger && typeHintBuffer.ElementType.Size.Bits == 8)
                    stringType = Context.Types.LayeTypeBuffer(stringElementType, stringByteCount);
                else stringType = Context.Types.LayeTypeSlice(stringElementType);
#endif

                SemaType stringType = Context.Types.LayeTypeSlice(stringElementType);

                return new SemaExprLiteralString(tokenString.Location, tokenString.TextValue, stringType.Qualified(tokenString.Location));
            }

            case SyntaxToken tokenTrue when tokenTrue.Kind == TokenKind.True:
            {
                return new SemaExprLiteralBool(tokenTrue.Location, true, Context.Types.LayeTypeBool.Qualified(tokenTrue.Location));
            }

            case SyntaxToken tokenFalse when tokenFalse.Kind == TokenKind.False:
            {
                return new SemaExprLiteralBool(tokenFalse.Location, false, Context.Types.LayeTypeBool.Qualified(tokenFalse.Location));
            }

            case SyntaxToken tokenNil when tokenNil.Kind == TokenKind.Nil:
            {
                return new SemaExprLiteralNil(tokenNil.Location);
            }

            case SyntaxToken unhandledToken:
            {
                Context.Assert(false, $"TODO: implement {nameof(SyntaxToken)} (where kind = '{unhandledToken.Kind}') for {nameof(AnalyseExpr)}");
                throw new UnreachableException();
            }
        }
    }

    private abstract record class LookupResult(string Name);
    private sealed record class LookupSuccess(string Name, SemaDeclNamed Decl) : LookupResult(Name);
    private sealed record class LookupNotFound(string Name) : LookupResult(Name);
    private sealed record class LookupOverloads(string Name, SemaDeclNamed[] Decls) : LookupResult(Name);
    private sealed record class LookupAmbiguous(string Name, SemaDeclNamed[] Decls) : LookupResult(Name);
    private sealed record class LookupNonScopeInPath(string Name, SemaDeclNamed Decl) : LookupResult(Name);

    private LookupResult LookUpUnqualifiedName(string name, Scope scope, bool thisScopeOnly)
    {
        if (name.IsNullOrEmpty()) return new LookupNotFound(name);

        var overloads = new List<SemaDeclNamed>();

        Scope? scopeCheck = scope;
        while (scopeCheck is not null)
        {
            var decls = scopeCheck.LookUp(name);
            if (decls.Count == 0)
            {
                if (thisScopeOnly) break;
                scopeCheck = scopeCheck.Parent;
                continue;
            }

            if (decls.Count == 1 && decls[0] is not SemaDeclFunction)
                return new LookupSuccess(name, decls[0]);

            Context.Assert(decls.All(d => d is SemaDeclFunction),
                "At this point in unqualified name lookup, all declarations should be functions.");
            overloads.AddRange(decls);

            scopeCheck = scopeCheck.Parent;
        }

        if (overloads.Count == 0)
            return new LookupNotFound(name);

        if (overloads.Count == 1)
            return new LookupSuccess(name, overloads[0]);

        return new LookupOverloads(name, [.. overloads]);
    }

    private LookupResult LookUpQualifiedName(string[] names, Scope scope)
    {
        Context.Assert(names.Length > 1, "Should not be unqualified lookup.");

        string firstName = names[0];
        var res = LookUpUnqualifiedName(firstName, scope, false);
        switch (res)
        {
            // can't do scope resolution on an ambiguous name.
            case LookupAmbiguous: return res;
            // unqualified lookup should never complain about this.
            case LookupNonScopeInPath:
            {
                Context.Unreachable("Non-scope error in unqualified lookup.");
                throw new UnreachableException();
            }

            case LookupSuccess success:
            {
                var nextScope = GetScopeFromDecl(success.Decl);
                if (nextScope is null)
                    return new LookupNonScopeInPath(firstName, success.Decl);
                scope = nextScope;
            } break;

            // no name found, look through module imports.
            case LookupNotFound:
            {
                if (!_currentFileImports.TryGetValue(firstName, out var importedModuleScope))
                    return res;

                scope = importedModuleScope;
            } break;
        }

        for (int i = 1; i < names.Length - 1; i++)
        {
            string middleName = names[i];

            var decls = scope.LookUp(middleName);
            if (decls.Count == 0)
                return new LookupNotFound(middleName);

            if (decls.Count != 1)
                return new LookupAmbiguous(middleName, [.. decls]);

            var declScope = GetScopeFromDecl(decls[0]);
            if (declScope is null)
                return new LookupNonScopeInPath(firstName, decls[0]);

            scope = declScope;
        }

        return LookUpUnqualifiedName(names[^1], scope, true);
    }

    private Scope? GetScopeFromDecl(SemaDeclNamed decl)
    {
        switch (decl)
        {
            default: return null;
            case SemaDeclStruct declStruct: return declStruct.Scope;
            case SemaDeclEnum declEnum: return declEnum.Scope;
        }
    }

    private LookupResult LookupName(SyntaxNode nameNode, Scope scope)
    {
        switch (nameNode)
        {
            default:
            {
                Context.Unreachable(nameNode.Location, $"Unsupported name syntax node: {nameNode.GetType().FullName}.");
                throw new UnreachableException();
            }

            case SyntaxToken { Kind: TokenKind.Identifier } nameIdent:
                return LookUpUnqualifiedName(nameIdent.TextValue, scope, false);

            case SyntaxNameref { NamerefKind: NamerefKind.Default, Names: [SyntaxToken { Kind: TokenKind.Identifier } nameIdent] }:
                return LookUpUnqualifiedName(nameIdent.TextValue, scope, false);

            case SyntaxNameref { NamerefKind: NamerefKind.Default } defaultNameref when defaultNameref.Names.All(nameNode => nameNode is SyntaxToken { Kind: TokenKind.Identifier }):
                return LookUpQualifiedName(defaultNameref.Names.Select(n => ((SyntaxToken)n).TextValue).ToArray(), scope);

            case SyntaxNameref:
            {
                Context.Unreachable(nameNode.Location, $"Unsupported nameref syntax in name resolution.");
                throw new UnreachableException();
            }
        }
    }

    private SemaExpr AnalyseLookup(SyntaxNameref nameref, Scope scope)
    {
        var res = LookupName(nameref, scope);
        switch (res)
        {
            default:
            {
                Context.Diag.Error(nameref.Location, $"This does not exist in this context.");
                return new SemaExprLookup(nameref.Location, SemaTypePoison.InstanceQualified, null)
                {
                    Dependence = ExprDependence.ErrorDependent,
                };
            }

            case LookupSuccess success:
            {
                var decl = success.Decl;
                var valueCategory = ValueCategory.LValue;

                SemaTypeQual? exprType;
                switch (decl)
                {
                    default:
                    {
                        Context.Assert(false, nameref.Location, $"Unhandled entity declaration in lookup type resolution: {decl.GetType().FullName}.");
                        throw new UnreachableException();
                    }

                    case SemaDeclBinding declBinding: exprType = declBinding.BindingType; break;
                    case SemaDeclParam declParam: exprType = declParam.ParamType; break;
                    case SemaDeclFunction declFunction:
                    {
                        exprType = declFunction.FunctionType(Context).Qualified(decl.Location);
                        valueCategory = ValueCategory.RValue;
                    } break;

                    case SemaDeclRegister declRegister:
                    {
                        exprType = declRegister.Type;
                        valueCategory = ValueCategory.Register;
                    } break;
                }

                return new SemaExprLookup(nameref.Location, exprType, success.Decl)
                {
                    ValueCategory = valueCategory,
                };
            }
        }
    }

    private SemaExpr AnalyseUnaryPrefix(SyntaxExprUnaryPrefix unary, SemaTypeQual? typeHint = null)
    {
        var operand = AnalyseExpr(unary.Operand);
        switch (unary.TokenOperator.Kind)
        {
            default:
            {
                operand = LValueToRValue(operand);
                return UndefinedOperator();
            }

            case TokenKind.Ampersand:
            {
                if (!operand.IsLValue)
                {
                    Context.Diag.Error(unary.TokenOperator.Location, $"Cannot take the address of {operand.ValueCategory.ToHumanString(includeArticle: true)}.");
                    return UndefinedOperator(false);
                }

                return new SemaExprCast(operand.Location, CastKind.LValueToReference,
                    Context.Types.LayeTypePointer(operand.Type).Qualified(unary.Location),
                    operand);
            }

            case TokenKind.Star:
            {
                operand = LValueToRValue(operand);
                if (operand.Type.Type is SemaTypePointer typePtr)
                {
                    return new SemaExprCast(unary.TokenOperator.Location, CastKind.PointerToLValue, typePtr.ElementType, operand)
                    {
                        ValueCategory = ValueCategory.LValue,
                        IsCompilerGenerated = true
                    };
                }
                else
                {
                    Context.Diag.Error(unary.TokenOperator.Location, $"Cannot dereference a value of type {operand.Type.ToDebugString(Colors)}.");
                    return UndefinedOperator(false);
                }
            }
        }

        SemaExprUnary UndefinedOperator(bool reportError = true)
        {
            if (reportError)
                Context.Diag.Error(unary.Location, $"Unary operator '{unary.TokenOperator.Location.Span(Context)}' is not defined for operand {operand.Type.ToDebugString(Colors)}.");
            return new SemaExprUnaryUndefined(unary.TokenOperator, operand);
        }
    }

    private SemaExprUnary AnalyseUnaryPostfix(SyntaxExprUnaryPostfix unary, SemaTypeQual? typeHint = null)
    {
        var operand = AnalyseExpr(unary.Operand);
        var operandType = operand.Type.CanonicalType.Type;

        if (operandType.IsPoison)
            return new SemaExprUnaryUndefined(unary.TokenOperator, operand);

        switch (unary.TokenOperator.Kind)
        {
            case TokenKind.PlusPlus:
            case TokenKind.MinusMinus:
            {
                string operatorImage = unary.TokenOperator.Kind == TokenKind.PlusPlus ? "++" : "--";
                Context.Diag.Error(unary.TokenOperator.Location, $"The postfix unary operator '{operatorImage}' is not an expression.");
                Context.Diag.Note("In Laye, the postfix increment and decrement operators are statements.");
                return new SemaExprUnaryUndefined(unary.TokenOperator, operand);
            }
        }

        return UndefinedOperator();

        SemaExprUnary UndefinedOperator()
        {
            Context.Diag.Error(unary.Location, $"Unary operator '{unary.TokenOperator.Location.Span(Context)}' is not defined for operand {operand.Type.ToDebugString(Colors)}.");
            return new SemaExprUnaryUndefined(unary.TokenOperator, operand);
        }
    }

    private static readonly Dictionary<BinaryOperatorKind, (TokenKind TokenKind, BinaryOperatorKind OperatorKind)[]> _builtinBinaryOperators = new()
    {
        { BinaryOperatorKind.Integer, [
            (TokenKind.Plus, BinaryOperatorKind.Add),
            (TokenKind.Minus, BinaryOperatorKind.Sub),
            (TokenKind.Star, BinaryOperatorKind.Mul),
            (TokenKind.Slash, BinaryOperatorKind.Div),
            (TokenKind.Percent, BinaryOperatorKind.Rem),

            (TokenKind.EqualEqual, BinaryOperatorKind.Eq),
            (TokenKind.BangEqual, BinaryOperatorKind.Neq),
            (TokenKind.Less, BinaryOperatorKind.Lt),
            (TokenKind.LessEqual, BinaryOperatorKind.Le),
            (TokenKind.Greater, BinaryOperatorKind.Gt),
            (TokenKind.GreaterEqual, BinaryOperatorKind.Ge),
        ] },

        { BinaryOperatorKind.Pointer, [
            (TokenKind.EqualEqual, BinaryOperatorKind.Eq),
            (TokenKind.BangEqual, BinaryOperatorKind.Neq),
        ] },
    };

    private SemaExprBinary AnalyseBinary(SyntaxExprBinary binary, SemaTypeQual? typeHint = null)
    {
        var left = LValueToRValue(AnalyseExpr(binary.Left));
        var right = LValueToRValue(AnalyseExpr(binary.Right));

        if (left.Type.IsPoison || right.Type.IsPoison)
            return new SemaExprBinaryBuiltIn(BinaryOperatorKind.Undefined, binary.TokenOperator, SemaTypePoison.InstanceQualified, left, right);

        var leftType = left.Type.CanonicalType;
        var rightType = right.Type.CanonicalType;

        var operatorKind = BinaryOperatorKind.Undefined;
        if (leftType.IsInteger && rightType.IsInteger)
            operatorKind |= BinaryOperatorKind.Integer;
        else if (IsPointerLike(leftType) && IsPointerLike(rightType))
            operatorKind |= BinaryOperatorKind.Pointer;

        if (operatorKind == BinaryOperatorKind.Undefined)
            return UndefinedOperator();

        if (!_builtinBinaryOperators.TryGetValue(operatorKind, out var availableOperators))
            return UndefinedOperator();

        var definedOperator = availableOperators.Where(kv => kv.TokenKind == binary.TokenOperator.Kind).Select(kv => kv.OperatorKind).SingleOrDefault();
        if (definedOperator == BinaryOperatorKind.Undefined) return UndefinedOperator();

        operatorKind |= definedOperator;

        SemaTypeQual binaryType;
        if (!ConvertToCommonTypeOrError(ref left, ref right))
            binaryType = SemaTypePoison.InstanceQualified;
        else
        {
            if (operatorKind.IsComparisonOperator())
                binaryType = Context.Types.LayeTypeBool.Qualified(binary.Location);
            else binaryType = left.Type;
        }

        return new SemaExprBinaryBuiltIn(operatorKind, binary.TokenOperator, binaryType, left, right);

        bool IsPointerLike(SemaTypeQual type)
        {
            var t = type.Type.CanonicalType;
            return t.IsPointer || t.IsBuffer || t is SemaTypeNil;
        }

        SemaExprBinary UndefinedOperator()
        {
            Context.Diag.Error(binary.Location, $"Binary operator '{binary.TokenOperator.Location.Span(Context)}' is not defined for operands {left.Type.ToDebugString(Colors)} and {right.Type.ToDebugString(Colors)}.");
            return new SemaExprBinaryBuiltIn(BinaryOperatorKind.Undefined, binary.TokenOperator, SemaTypePoison.InstanceQualified, left, right);
        }
    }

    private SemaExprCall AnalyseCall(SyntaxExprCall call, SemaTypeQual? typeHint = null)
    {
        var callee = AnalyseExpr(call.Callee);
        var callableType = callee.Type.Type;

        switch (callableType)
        {
            case SemaTypePoison typePoison:
            {
                var arguments = call.Args.Select(arg => EvaluateIfPossible(AnalyseExpr(arg))).ToArray();
                return new SemaExprCall(call.Location, SemaTypePoison.InstanceQualified, callee, arguments);
            }

            case SemaTypeFunction typeFunction:
            {
                int paramCount = typeFunction.ParamTypes.Count;
                if (typeFunction.VarargsKind == VarargsKind.Laye)
                    paramCount--; // don't include the last parameter in regular arg/param checks

                int argumentCount = call.Args.Count;

                if (argumentCount < paramCount)
                {
                    Context.Diag.Error(call.Location, $"Not enough arguments to function call. Expected {paramCount}, got {argumentCount}.");
                }

                if (argumentCount > paramCount)
                {
                    if (typeFunction.VarargsKind == VarargsKind.None)
                        Context.Diag.Error(call.Location, $"Too many arguments to function call. Expected {paramCount}, got {argumentCount}.");
                }

                SemaTypeQual? layeVarargsElementType = null;
                if (typeFunction.VarargsKind == VarargsKind.Laye)
                {
                    Context.Assert(typeFunction.ParamTypes.Count >= 1, "Can't be a Laye varargs function without at least one argument.");

                    var lastParamType = typeFunction.ParamTypes[typeFunction.ParamTypes.Count - 1];
                    var lastParamTypeCanon = lastParamType.Type.CanonicalType;

                    Context.Assert(lastParamTypeCanon is SemaTypeSlice, "Laye varargs function expected to end in a slice parameter.");
                    layeVarargsElementType = ((SemaTypeSlice)lastParamTypeCanon).ElementType;
                }

                SemaExpr[] arguments = new SemaExpr[argumentCount];
                for (int i = 0; i < arguments.Length; i++)
                {
                    SemaTypeQual? argTypeHint = null;
                    if (i < typeFunction.ParamTypes.Count)
                        argTypeHint = typeFunction.ParamTypes[i];
                    else
                    {
                        if (typeFunction.VarargsKind == VarargsKind.Laye)
                        {
                            Context.Assert(layeVarargsElementType is not null, "where did it go");
                            argTypeHint = layeVarargsElementType;
                        }
                    }

                    var argument = AnalyseExpr(call.Args[i], argTypeHint);
                    if (argTypeHint is not null)
                        argument = ConvertOrError(argument, argTypeHint);
                    else
                    {
                        // do no real conversion if the function call is already invalid
                        if (typeFunction.VarargsKind == VarargsKind.None)
                            argument = LValueToRValue(argument);
                        // otherwise, convert to a valid C varargs type
                        else if (typeFunction.VarargsKind == VarargsKind.C)
                            argument = ConvertToCVarargsOrError(argument);
                        else Context.Assert(false, "Unexpected varargs kind without type hint.");
                    }

                    arguments[i] = argument;
                }

                return new SemaExprCall(call.Location, typeFunction.ReturnType, callee, arguments)
                {
                    IsDiscardable = typeFunction.IsDiscardable
                };
            }

            default:
            {
                var C = new Colors(Context.UseColor);
                Context.Diag.Error(call.Callee.Location, $"Cannot call an expression of type {callee.Type.ToDebugString(C)}");

                var arguments = call.Args.Select(e => EvaluateIfPossible(AnalyseExpr(e))).ToArray();
            } break;
        }

        Context.Todo(call.Location, "Handle other kinds of calls.");
        throw new UnreachableException();
    }

    private SemaExpr AnalyseCast(SyntaxExprCast cast, SemaTypeQual? typeHint = null)
    {
        SemaTypeQual castType;
        if (cast.IsAutoCast)
        {
            if (typeHint is null)
            {
                Context.Diag.Error(cast.Location, "Unable to determine the type of the auto-cast. Specify the desired type manually.");
                castType = Context.Types.LayeTypePoison.Qualified(cast.Location);
            }
            else castType = typeHint;
        }
        else castType = AnalyseType(cast.TargetType!);

        var expr = AnalyseExpr(cast.Expr);
        if (Convert(ref expr, castType))
            return expr;

        if (expr.Type.IsInteger && castType.IsInteger)
        {
            CastKind castKind;
            if (expr.Type.Size == castType.Size)
                castKind = CastKind.NoOp;
            else if (expr.Type.Size > castType.Size)
                castKind = CastKind.IntegralTruncate;
            // TODO(local): zero extend casting for handling unsigned types
            else castKind = CastKind.IntegralSignExtend;

            return new SemaExprCast(cast.Location, castKind, castType, expr);
        }

        Context.Diag.Error(cast.Location, $"Cannot convert from {expr.Type.ToDebugString(Colors)} to {castType.ToDebugString(Colors)}.");
        return new SemaExprCast(cast.Location, CastKind.Invalid, castType, expr);
    }

    private SemaExpr AnalyseField(SyntaxExprField field, SemaTypeQual? typeHint = null)
    {
        string fieldName = field.FieldNameText;
        var operand = AnalyseExpr(field.Operand);

        switch (operand.Type.CanonicalType.Type)
        {
            default:
            {
                Context.Diag.Error(field.Operand.Location, $"Cannot index a value of type {operand.Type.ToDebugString(Colors)}.");
                return new SemaExprFieldBadIndex(field.Location, operand, fieldName)
                {
                    ValueCategory = ValueCategory.LValue,
                };
            }

            case SemaTypeStruct typeStruct:
            {
                if (!operand.IsLValue)
                {
                    // TODO(local): figure out a better way to say this.
                    Context.Diag.Error(field.Operand.Location, "Cannot index a non-lvalue.");
                }

                var structDecl = typeStruct.DeclStruct;
                if (!structDecl.TryLookupField(fieldName, out var declField, out _))
                {
                    Context.Diag.Error(field.Operand.Location, $"No such field '{fieldName}' on type {typeStruct.ToDebugString(Colors)}.");
                    return new SemaExprFieldBadIndex(field.Location, operand, fieldName)
                    {
                        ValueCategory = ValueCategory.LValue,
                    };
                }

                return new SemaExprFieldStructIndex(field.Location, operand, declField, declField.Offset)
                {
                    ValueCategory = ValueCategory.LValue,
                };
            }
        }
    }

    private SemaExpr AnalyseIndex(SyntaxIndex index, SemaTypeQual? typeHint = null)
    {
        var operand = AnalyseExpr(index.Operand);
        var indices = index.Indices.Select(expr => AnalyseExpr(expr)).ToArray();

        var operandTypeCanon = operand.Type.CanonicalType.Type;

        if (operandTypeCanon is SemaTypeBuffer typeBuffer)
        {
            operand = LValueToRValue(operand);

            for (int i = 0; i < indices.Length; i++)
                indices[i] = ConvertOrError(indices[i], Context.Types.LayeTypeInt.Qualified(Location.Nowhere));

            if (indices.Length != 1)
                Context.Diag.Error(index.Location, $"Expected exactly one index to a buffer, but got {indices.Length}.");

            var elementType = typeBuffer.ElementType;
            return new SemaExprIndexBuffer(elementType, operand, indices[0])
            {
                ValueCategory = ValueCategory.LValue
            };
        }
        else if (operandTypeCanon is SemaTypeArray typeArray)
        {
            for (int i = 0; i < indices.Length; i++)
                indices[i] = ConvertOrError(indices[i], Context.Types.LayeTypeInt.Qualified(Location.Nowhere));

            if (indices.Length != typeArray.Arity)
                Context.Diag.Error(index.Location, $"Expected {typeArray.Arity} indices, but got {indices.Length}.");

            var elementType = typeArray.ElementType;
            return new SemaExprIndexArray(elementType, operand, indices)
            {
                ValueCategory = ValueCategory.LValue
            };
        }
        else
        {
            Context.Diag.Error(index.Location, $"Cannot index value of type {operand.Type.ToDebugString(Colors)}.");
            return new SemaExprIndexInvalid(operand, indices)
            {
                ValueCategory = ValueCategory.LValue
            };
        }
    }

    public SemaExpr AnalyseConstructor(SyntaxExprConstructor ctor, SemaTypeQual? typeHint = null)
    {
        SemaTypeQual ctorType;
        SemaConstructorInitializer[] inits = new SemaConstructorInitializer[ctor.Inits.Count];

        if (ctor.Type is SyntaxToken { Kind: TokenKind.Var })
        {
            if (typeHint is null)
            {
                Context.Diag.Error(ctor.Type.Location, "Unable to infer a type for this construct in this context.");
                ctorType = SemaTypePoison.InstanceQualified;
            }
            else ctorType = typeHint;
        }
        else ctorType = AnalyseType(ctor.Type);

        bool hasEncounteredDesignator = false;

        bool hasErroredOnExcessiveInitializer = false;
        bool hasErroredOnNonDesignatedInitializer = false;

        int initIndex = 0;

        if (ctorType.CanonicalType.Type is SemaTypeStruct ctorStruct)
        {
            if (!ctorStruct.DeclStruct.IsLeaf)
            {
                Context.Diag.Error(ctorType.Location, $"Cannot construct a value of type {ctorType.ToDebugString(Colors)}: it has child variants. Specify a variant of this type to construct.");
            }

            for (int i = 0; i < inits.Length; i++)
            {
                var init = ctor.Inits[i];
                Size initOffset = ctorStruct.Size;

                SemaExpr initExpr;
                if (init is SyntaxConstructorInitDesignated initDesignated)
                {
                    hasEncounteredDesignator = true;
                    Context.Todo(init.Location, "Designated initializers are currently not implemented.");
                }
                else
                {
                    if (hasEncounteredDesignator)
                    {
                        ReportInitializerAfterDesignated(init.Location);
                        initExpr = AnalyseExpr(init.Value);
                        initExpr = LValueToRValue(initExpr);
                        inits[i] = new SemaConstructorInitializer(init.Location, initExpr, initOffset);
                        continue;
                    }

                    if (initIndex < ctorStruct.DeclStruct.FieldDecls.Count)
                    {
                        var initField = ctorStruct.DeclStruct.FieldDecls[initIndex];
                        initOffset = initField.Offset;

                        initExpr = AnalyseExpr(init.Value, initField.FieldType);
                        initExpr = ConvertOrError(initExpr, initField.FieldType);

                        initIndex++;
                    }
                    else
                    {
                        if (!hasErroredOnExcessiveInitializer)
                        {
                            hasErroredOnExcessiveInitializer = true;
                            Context.Diag.Error(init.Location, "Initializer is beyond the bounds of the struct.");
                        }

                        initExpr = AnalyseExpr(init.Value);
                        initExpr = LValueToRValue(initExpr);
                    }

                    inits[i] = new SemaConstructorInitializer(init.Location, initExpr, initOffset);
                }
            }
        }
        else if (ctorType.CanonicalType.Type is SemaTypeArray ctorArray)
        {
            var elementType = ctorArray.ElementType;
            for (int i = 0; i < inits.Length; i++)
            {
                var init = ctor.Inits[i];
                Size initOffset = ctorArray.Size;

                SemaExpr initExpr;
                if (init is SyntaxConstructorInitDesignated initDesignated)
                {
                    hasEncounteredDesignator = true;
                    Context.Todo(init.Location, "Designated initializers are currently not implemented.");
                }
                else
                {
                    if (hasEncounteredDesignator)
                    {
                        ReportInitializerAfterDesignated(init.Location);
                        initExpr = AnalyseExpr(init.Value);
                        initExpr = LValueToRValue(initExpr);
                        inits[i] = new SemaConstructorInitializer(init.Location, initExpr, initOffset);
                        continue;
                    }

                    initExpr = AnalyseExpr(init.Value, elementType);
                    initExpr = ConvertOrError(initExpr, elementType);

                    if (initIndex < ctorArray.FlatLength)
                    {
                        initOffset = elementType.Size * initIndex;
                        initIndex++;
                    }
                    else
                    {
                        if (!hasErroredOnExcessiveInitializer)
                        {
                            hasErroredOnExcessiveInitializer = true;
                            Context.Diag.Error(init.Location, "Initializer is beyond the bounds of the array.");
                        }
                    }

                    inits[i] = new SemaConstructorInitializer(init.Location, initExpr, initOffset);
                }
            }
        }
        else
        {
            if (!ctorType.IsPoison)
                Context.Diag.Error(ctorType.Location, $"Cannot construct a value of type {ctorType.ToDebugString(Colors)} in this way.");

            // ensure we still report errors for initializers if there are any
            inits = new SemaConstructorInitializer[ctor.Inits.Count];
            for (int i = 0; i < inits.Length; i++)
            {
                var init = ctor.Inits[i];

                var initExpr = AnalyseExpr(init.Value);
                initExpr = LValueToRValue(initExpr);

                inits[i] = new SemaConstructorInitializer(init.Location, initExpr, Size.FromBytes(0));
            }
        }

        return new SemaExprConstructor(ctor.Location, ctorType, inits);

        void ReportInitializerAfterDesignated(Location location)
        {
            if (!hasErroredOnNonDesignatedInitializer)
            {
                hasErroredOnNonDesignatedInitializer = true;
                Context.Diag.Error(location, "Positional initializers must all occur before designated initializers.");

                var firstDesignatedLocation = ctor.Inits.First(init => init is SyntaxConstructorInitDesignated).Location;
                Context.Diag.Note(firstDesignatedLocation, "The first designated initializer occured here.");
            }
        }
    }

    private bool TryEvaluate(SemaExpr expr, out EvaluatedConstant value)
    {
        var evaluator = new ConstantEvaluator();
        return evaluator.TryEvaluate(expr, out value);
    }

    private SemaExpr EvaluateIfPossible(SemaExpr expr)
    {
        if (TryEvaluate(expr, out var constant))
            return new SemaExprEvaluatedConstant(expr, constant);

        return expr;
    }

    private const int ConvertScoreNoOp = 0;
    private const int ConvertScoreImpossible = -1;
    private const int ConvertScoreContainsErrors = -2;

    private int ConvertImpl(ref SemaExpr expr, SemaTypeQual toQual, bool performConversion)
    {
        var from = expr.Type.CanonicalType.Type;
        var to = toQual.CanonicalType.Type;

        if (from.IsPoison || to.IsPoison)
            return ConvertScoreNoOp;

        if (from.IsErrored || to.IsErrored)
            return ConvertScoreContainsErrors;

        int score = ConvertScoreNoOp;
        if (expr.IsLValue)
        {
            if (performConversion)
                expr = LValueToRValue(expr);
            score = 1;
        }

        if (from == to)
        {
            if (performConversion)
                expr = EvaluateIfPossible(expr);
            return score;
        }

        bool CheckContainerType<T>(ref SemaExpr expr, bool checkNil = false)
            where T : SemaContainerType<T>
        {
            if (from is T fromCont && to is T toCont)
            {
                if (fromCont.ElementType.TypeEquals(toCont.ElementType, TypeComparison.WithQualifierConversions))
                {
                    if (performConversion)
                        expr = ImplicitCast(expr, toQual);
                    return true;
                }
            }

            if (checkNil && from is SemaTypeNil && to is T)
            {
                Context.Assert(expr is SemaExprLiteralNil, expr.Location, "The only thing which should have the 'nil' type is the literal nil expression.");
                if (performConversion)
                    expr = new SemaExprLiteralNil(expr.Location, toQual);
                return true;
            }

            return false;
        }

        if (CheckContainerType<SemaTypePointer>(ref expr, true))
            return score;

        if (CheckContainerType<SemaTypeBuffer>(ref expr, true))
            return score;

        if (CheckContainerType<SemaTypeNilable>(ref expr, true))
            return score;

        if (CheckContainerType<SemaTypeArray>(ref expr))
            return score;

        if (CheckContainerType<SemaTypeSlice>(ref expr))
            return score;

        // TODO(local): more conversion checks

        if (TryEvaluate(expr, out var evaluatedConstant))
        {
            if (evaluatedConstant.Kind == EvaluatedConstantKind.Integer && to.IsNumeric)
            {
                if (to.IsFloat)
                {
                    Context.Assert(false, "TODO: Converting an evaluated integer constant to a float constant is not supported; floats are not currently supported at this stage.");
                    throw new UnreachableException();
                }

                long bitCount = evaluatedConstant.IntegerValue.GetBitLength();
                if (bitCount <= to.Size.Bits)
                {
                    if (performConversion)
                        expr = new SemaExprEvaluatedConstant(ImplicitCast(expr, toQual), evaluatedConstant);
                    return score;
                }
            }
            else if (evaluatedConstant.Kind == EvaluatedConstantKind.String)
            {
                // check for the lack of qualifiers all the way down, must match these types exactly
                if (to == Context.Types.LayeTypeI8Slice || to == Context.Types.LayeTypeI8Buffer)
                {
                    if (performConversion)
                        expr = new SemaExprEvaluatedConstant(ImplicitCast(expr, toQual), evaluatedConstant);
                    return score;
                }
            }
        }

        if (from.IsInteger && to.IsInteger)
        {
            if (from.Size <= to.Size)
            {
                if (performConversion)
                    expr = WrapWithCast(expr, toQual, CastKind.IntegralSignExtend);
                return score;
            }
        }

        return ConvertScoreImpossible;
    }

    private bool Convert(ref SemaExpr expr, SemaTypeQual to)
    {
        if (expr.IsErrored) return true;
        return ConvertImpl(ref expr, to, true) >= 0;
    }

    private SemaExpr ConvertOrError(SemaExpr expr, SemaTypeQual to)
    {
        if (expr.IsErrored) return expr;
        if (!Convert(ref expr, to))
            Context.Diag.Error(expr.Location, $"Expression of type {expr.Type.ToDebugString(Colors)} is not convertible to {to.ToDebugString(Colors)}.");

        return expr;
    }

    private SemaExpr ConvertToCVarargsOrError(SemaExpr expr)
    {
        var exprType = expr.Type.Type.CanonicalType;
        if (exprType.IsInteger && exprType.Size.Bits <= Context.Types.LayeTypeFFIInt.Size.Bits)
            return ConvertOrError(expr, Context.Types.LayeTypeFFIInt.Qualified(Location.Nowhere));
        else if (exprType.IsFloat && exprType.Size.Bits <= Context.Types.LayeTypeFFIDouble.Size.Bits)
            return ConvertOrError(expr, Context.Types.LayeTypeFFIDouble.Qualified(Location.Nowhere));
        else if (exprType.IsNumeric || exprType.IsPointer || exprType.IsBuffer)
            return LValueToRValue(expr);

        Context.Todo(expr.Location, $"ConvertToCVarargsOrError for expr of type {expr.Type.ToDebugString(Colors)}.");
        throw new UnreachableException();
    }

    private int TryConvert(ref SemaExpr expr, SemaTypeQual to)
    {
        return ConvertImpl(ref expr, to, false);
    }

    private bool ConvertToCommonType(ref SemaExpr a, ref SemaExpr b)
    {
        int a2bScore = TryConvert(ref a, b.Type);
        int b2aScore = TryConvert(ref b, a.Type);

        if (a2bScore >= 0 && (a2bScore <= b2aScore || b2aScore < 0))
            return Convert(ref a, b.Type);
        
        return Convert(ref b, a.Type);
    }

    private bool ConvertToCommonTypeOrError(ref SemaExpr a, ref SemaExpr b)
    {
        if (!ConvertToCommonType(ref a, ref b))
        {
            Context.Diag.Error(a.Location, $"Can't convert expressions of type {a.Type.ToDebugString(Colors)} and {b.Type.ToDebugString(Colors)} to a common type.");
            return false;
        }

        return true;
    }

    private bool ImplicitDereference(ref SemaExpr expr)
    {
        while (expr.Type.Type is SemaTypePointer typePtr)
        {
            expr = LValueToRValue(expr);
            expr = new SemaExprCast(expr.Location, CastKind.PointerToLValue, typePtr.ElementType, expr)
            {
                ValueCategory = ValueCategory.LValue,
                IsCompilerGenerated = true
            };
        }

        return expr.IsLValue;
    }

    private SemaExprCast WrapWithCast(SemaExpr expr, SemaTypeQual type, CastKind castKind)
    {
        return new SemaExprCast(expr.Location, castKind, type, expr)
        {
            IsCompilerGenerated = true,
        };
    }

    private SemaExprCast ImplicitCast(SemaExpr expr, SemaTypeQual type)
    {
        return WrapWithCast(expr, type, CastKind.Implicit);
    }

    private SemaExpr PointerToIntegerCast(SemaExpr expr)
    {
        if (expr.Type.Type is SemaTypePointer or SemaTypeBuffer)
            return WrapWithCast(expr, Context.Types.LayeTypeInt.Qualified(expr.Location), CastKind.BitCast);
        
        return expr;
    }

    private SemaExpr LValueToRValue(SemaExpr expr)
    {
        if (expr.IsErrored) return expr;

        if (expr.IsLValue)
            expr = WrapWithCast(expr, expr.Type, CastKind.LValueToRValue);

        return expr;
    }

    private IDisposable EnterScope(Scope scope)
    {
        scope.CurrentDefer = CurrentScope.CurrentDefer;
        return new ScopeDisposable(this, scope);
    }

    private IDisposable EnterScope(bool createNewScope = true)
    {
        return createNewScope ? new ScopeDisposable(this) : new ScopeDisposableNoPush(this);
    }

    private CurrentFunctionDisposable EnterFunction(SemaDeclFunction function)
    {
        return new CurrentFunctionDisposable(this, function);
    }

    private sealed class ScopeDisposableNoPush : IDisposable
    {
        private readonly Sema _sema;
        private readonly Scope _scope;

        public ScopeDisposableNoPush(Sema sema)
        {
            _sema = sema;
            _scope = sema.CurrentScope;
        }

        public void Dispose()
        {
            if (!_sema._scopeStack.TryPeek(out var scope))
            {
                _sema.Context.Diag.ICE($"Exited a {nameof(ScopeDisposableNoPush)}, but there were no scopes");
                throw new UnreachableException();
            }

            _sema.Context.Assert(ReferenceEquals(scope, _scope), $"Exited a {nameof(ScopeDisposableNoPush)}, but the scope was not the correct scope");
        }
    }

    private sealed class ScopeDisposable : IDisposable
    {
        private readonly Sema _sema;
        private readonly Scope _scope;

        public ScopeDisposable(Sema sema, Scope? scope = null)
        {
            _sema = sema;
            _scope = scope ?? new Scope(sema.CurrentScope) { CurrentDefer = sema.CurrentScope.CurrentDefer };
            sema._scopeStack.Push(_scope);
        }

        public void Dispose()
        {
            if (!_sema._scopeStack.TryPop(out var scope))
            {
                _sema.Context.Diag.ICE($"Exited a {nameof(ScopeDisposable)}, but there were no scopes");
                throw new UnreachableException();
            }

            _sema.Context.Assert(ReferenceEquals(scope, _scope), $"Exited a {nameof(ScopeDisposable)}, but the scope was not the correct scope");
        }
    }

    private sealed class CurrentFunctionDisposable : IDisposable
    {
        private readonly Sema _sema;
        private readonly SemaDeclFunction _function;

        public CurrentFunctionDisposable(Sema sema, SemaDeclFunction function)
        {
            _sema = sema;
            _function = function;
            sema._functionStack.Push(function);
        }

        public void Dispose()
        {
            if (!_sema._functionStack.TryPop(out var function))
            {
                _sema.Context.Diag.ICE($"Exited a {nameof(CurrentFunctionDisposable)}, but there were no functions");
                throw new UnreachableException();
            }

            _sema.Context.Assert(ReferenceEquals(function, _function), $"Exited a {nameof(CurrentFunctionDisposable)}, but the function was not the correct function");
        }
    }
}
#pragma warning restore CA1822 // Mark members as static
