using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Choir.CommandLine;
using Choir.Front.Laye.Syntax;

using static System.Formats.Asn1.AsnWriter;

namespace Choir.Front.Laye.Sema;

#pragma warning disable CA1822 // Mark members as static
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

        foreach (var topLevelNode in module.TopLevelSyntax)
            sema.ForwardDeclareIfAllowedOutOfOrder(topLevelNode);
        
        if (module.Context.HasIssuedError) return;

        foreach (var topLevelNode in module.TopLevelSyntax)
        {
            var decl = sema.AnalyseTopLevelDecl(topLevelNode);
            module.AddDecl(decl);
        }
    }

    private static bool IsImportDeclForCHeader(SyntaxDeclImport importDecl)
    {
        return importDecl.ImportKind == ImportKind.FilePath && importDecl.ModuleNameText.EndsWith(".h", StringComparison.InvariantCultureIgnoreCase);
    }

    public Module Module { get; }
    public ChoirContext Context { get; }
    public TranslationUnit TranslationUnit { get; }

    private readonly Dictionary<SyntaxNode, SemaDecl> _forwardDeclNodes = [];
    private readonly Stack<Scope> _scopes = [];
    private readonly Stack<SemaDeclFunction> _functions = [];

    private Scope Scope => _scopes.Count == 0 ? Module.FileScope : _scopes.Peek();
    private SemaDeclFunction CurrentFunction
    {
        get
        {
            if (_functions.TryPeek(out var function))
                return function;

            Context.Diag.ICE("Attempt to access the current function during sema from outside any function.");
            throw new UnreachableException();
        }
    }

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
            Module importedModule;
            if (IsImportDeclForCHeader(importDecl))
            {
                importedModule = ParseCHeaderFromImport(importDecl);
                //var cSema = new Sema(importedModule);
                importDecl.ReferencedModule = importedModule;
                goto handle_imports_and_exports;
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
            if (TranslationUnit.FindModuleBySourceFile(importedFile) is {} importedModuleResult)
                importedModule = importedModuleResult;
            else
            {
                importedModule = new Module(importedFile);
                TranslationUnit.AddModule(importedModule);
                Lexer.ReadTokens(importedModule);
                Parser.ParseSyntax(importedModule);
            }
            
            importDecl.ReferencedModule = importedModule;
            if (Context.HasIssuedError) return;

            Analyse(importedModule);

            handle_imports_and_exports:;
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
        }
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

    private void ForwardDeclareIfAllowedOutOfOrder(SyntaxNode node)
    {
        SemaDecl forwardDecl;
        // if we're in file scope, scoping will be handled separately (for now)
        Scope? nonFileScope = _scopes.TryPeek(out var scope2) ? scope2 : null;
        switch (node)
        {
            default: return;

            case SyntaxDeclAlias declAlias:
            {
                forwardDecl = new SemaDeclAlias(declAlias.Location, declAlias.TokenName.TextValue, declAlias.IsStrict);
                nonFileScope?.AddDecl(declAlias.TokenName.TextValue, forwardDecl);
            } break;

            case SyntaxDeclStruct declStruct:
            {
                forwardDecl = new SemaDeclStruct(declStruct.Location, declStruct.TokenName.TextValue);
                nonFileScope?.AddDecl(declStruct.TokenName.TextValue, forwardDecl);
            } break;

            case SyntaxDeclEnum declEnum:
            {
                forwardDecl = new SemaDeclEnum(declEnum.Location, declEnum.TokenName.TextValue);
                nonFileScope?.AddDecl(declEnum.TokenName.TextValue, forwardDecl);
            } break;

            case SyntaxDeclBinding declBinding:
            {
                forwardDecl = new SemaDeclBinding(declBinding.Location, declBinding.TokenName.TextValue);
                nonFileScope?.AddDecl(declBinding.TokenName.TextValue, forwardDecl);
            } break;

            case SyntaxDeclFunction declFunction:
            {
                if (declFunction.Name is not SyntaxToken tokenIdent || tokenIdent.Kind != TokenKind.Identifier)
                    throw new NotImplementedException("need to have a generic entity name type");
                forwardDecl = new SemaDeclFunction(declFunction.Location, tokenIdent.TextValue);
                nonFileScope?.AddDecl(tokenIdent.TextValue, forwardDecl);
            } break;
        }

        _forwardDeclNodes[node] = forwardDecl;
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

            case SyntaxDeclImport declImport:
            {
                Context.Assert(declImport.ReferencedModule is not null, declImport.Location, "import syntax should have a referenced module if we're getting this far");
                var semaNode = new SemaDeclImport(declImport.Location, declImport.ReferencedModule, declImport.IsExported, []);
                return semaNode;
            }
            
            case SyntaxDeclAlias or SyntaxDeclStruct or SyntaxDeclEnum or SyntaxDeclBinding or SyntaxDeclFunction:
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
        }
    }

    private SemaStmt AnalyseStmtOrDecl(SyntaxNode stmt, bool inheritCurrentScope = false)
    {
        if (stmt is SyntaxTypeBuffer or SyntaxTypeBuiltIn or SyntaxTypeNilable or SyntaxTypePointer or SyntaxTypeReference or SyntaxTypeSlice)
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
                    semaNodeCheck = new SemaDeclAlias(declAlias.Location, declAlias.TokenName.TextValue, declAlias.IsStrict);
                Context.Assert(semaNodeCheck is SemaDeclAlias, declAlias.Location, "alias declaration did not have sema node of alias type");
                var semaNode = (SemaDeclAlias)semaNodeCheck;
                return semaNode;
            }

            case SyntaxDeclStruct declStruct:
            {
                using var _ = EnterScope();
                if (!_forwardDeclNodes.TryGetValue(stmt, out var semaNodeCheck))
                    semaNodeCheck = new SemaDeclStruct(declStruct.Location, declStruct.TokenName.TextValue);
                Context.Assert(semaNodeCheck is SemaDeclStruct, declStruct.Location, "struct declaration did not have sema node of struct type");
                var semaNode = (SemaDeclStruct)semaNodeCheck;
                return semaNode;
            }

            case SyntaxDeclEnum declEnum:
            {
                if (!_forwardDeclNodes.TryGetValue(stmt, out var semaNodeCheck))
                    semaNodeCheck = new SemaDeclEnum(declEnum.Location, declEnum.TokenName.TextValue);
                Context.Assert(semaNodeCheck is SemaDeclEnum, declEnum.Location, "enum declaration did not have sema node of enum type");
                var semaNode = (SemaDeclEnum)semaNodeCheck;
                return semaNode;
            }

            case SyntaxDeclBinding declBinding:
            {
                if (!_forwardDeclNodes.TryGetValue(stmt, out var semaNodeCheck))
                    semaNodeCheck = new SemaDeclBinding(declBinding.Location, declBinding.TokenName.TextValue);
                Context.Assert(semaNodeCheck is SemaDeclBinding, declBinding.Location, "binding declaration did not have sema node of binding type");
                var semaNode = (SemaDeclBinding)semaNodeCheck;
                return semaNode;
            }

            case SyntaxDeclFunction declFunction:
            {
                using var _s = EnterScope();

                if (!_forwardDeclNodes.TryGetValue(stmt, out var semaNodeCheck))
                {
                    if (declFunction.Name is not SyntaxToken tokenIdent || tokenIdent.Kind != TokenKind.Identifier)
                        throw new NotImplementedException("need to have a generic entity name type");
                    semaNodeCheck = new SemaDeclFunction(declFunction.Location, tokenIdent.TextValue);
                }
                Context.Assert(semaNodeCheck is SemaDeclFunction, declFunction.Location, "function declaration did not have sema node of function type");
                var semaNode = (SemaDeclFunction)semaNodeCheck;

                using var _f = EnterFunction(semaNode);

                semaNode.ReturnType = AnalyseType(declFunction.ReturnType);
                
                Context.Assert(declFunction.Params.Count == 0, declFunction.Location, "TODO: implement sema for top-level function parameters");
                
                if (declFunction.Body is SyntaxCompound bodyCompound)
                    semaNode.Body = (SemaStmtCompound)AnalyseStmtOrDecl(bodyCompound, inheritCurrentScope: true);
                else if (declFunction.Body is not null)
                {
                    Context.Assert(false, declFunction.Body.Location, $"unsupported syntax as function body: {declFunction.Body.GetType().Name}");
                    throw new UnreachableException();
                }

                return semaNode;
            }

            case SyntaxCompound stmtCompound:
            {
                using var _ = EnterScope(!inheritCurrentScope);
                foreach (var node in stmtCompound.Body)
                    ForwardDeclareIfAllowedOutOfOrder(node);
                // TODO(local): handle forward declarations in compound statements
                // TODO(local): create scopes in compound statements.
                var childStatements = stmtCompound.Body.Select(node => AnalyseStmtOrDecl(node)).ToArray();
                return new SemaStmtCompound(stmtCompound.Location, childStatements);
            }

            case SyntaxStmtReturn stmtReturn:
            {
                if (stmtReturn.Value is null)
                {
                    return new SemaStmtReturnVoid(stmtReturn.Location);
                }

                var returnValue = AnalyseExpr(stmtReturn.Value);
                returnValue = ConvertOrError(returnValue, CurrentFunction.ReturnType);
                return new SemaStmtReturnValue(stmtReturn.Location, returnValue);
            }
        }
    }

    private SemaExpr AnalyseExpr(SyntaxNode expr)
    {
        if (expr is SyntaxTypeBuffer or SyntaxTypeBuiltIn or SyntaxTypeNilable or SyntaxTypePointer or SyntaxTypeReference or SyntaxTypeSlice)
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

            case SyntaxToken unhandledToken:
            {
                Context.Assert(false, $"TODO: implement {nameof(SyntaxToken)} (where kind = '{unhandledToken.Kind}') for {nameof(AnalyseExpr)}");
                throw new UnreachableException();
            }
        }
    }

    private bool TryEvaluate(SemaExpr expr, out EvaluatedConstant value)
    {
        var evaluator = new ConstantEvaluator();
        return evaluator.TryEvaluate(expr, out value);
    }

    private const int ConvertScoreNoOp = 0;
    private const int ConvertScoreImpossible = -1;
    private const int ConvertScoreContainsErrors = -2;

    private int ConvertImpl(ref SemaExpr expr, SemaTypeQual to, bool performConversion)
    {
        var from = expr.Type.Requalified();
        to = to.Requalified();

        if (from.Type.IsPoison || to.Type.IsPoison)
            return ConvertScoreNoOp;

        if (from.Type.IsErrored || to.Type.IsErrored)
            return ConvertScoreContainsErrors;

        if (performConversion)
        {
            expr = LValueToRValue(expr, false);
            from = expr.Type.Requalified();
        }

        // TODO(local): type-equals

        int score = 0;
        if (expr.IsLValue) score = 1;

        // TODO(local): more conversion checks

        if (TryEvaluate(expr, out var evaluatedConstant))
        {
            if (evaluatedConstant.Kind == EvaluatedConstantKind.Integer && to.Type.IsNumeric)
            {
                if (to.Type.IsFloat)
                {
                    Context.Assert(false, "TODO: Converting an evaluated integer constant to a float constant is not supported; floats are not currently supported at this stage.");
                    throw new UnreachableException();
                }

                long bitCount = evaluatedConstant.IntegerValue.GetBitLength();
                if (bitCount <= to.Type.Size.Bits)
                {
                    if (performConversion)
                        expr = new SemaExprEvaluatedConstant(ImplicitCast(expr, to), evaluatedConstant);
                    return score;
                }
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
            Context.Diag.Error(expr.Location, $"expression of type {expr.Type} is not convertible to {expr.Type}");

        return expr;
    }

    private SemaExpr ConvertToCVarargsOrError(SemaExpr expr, SemaTypeQual to)
    {
        throw new NotImplementedException();
        // return expr;
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

    private bool ImplicitDereference(ref SemaExpr expr)
    {
        if (expr.Type.Type is SemaTypeReference typeRef)
        {
            expr = LValueToRValue(expr, false);
            expr = WrapWithCast(expr, typeRef.ElementType, CastKind.ReferenceToLValue);
        }

        while (expr.Type.Type is SemaTypePointer)
        {
            expr = SemaExprDereference.Create(Context, expr.Location, expr);
            expr.IsCompilerGenerated = true;
        }

        return expr.IsLValue;
    }

    private bool ImplicitDeReference(ref SemaExpr expr)
    {
        if (expr.Type.Type is SemaTypeReference typeRef)
        {
            expr = LValueToRValue(expr, false);
            expr = WrapWithCast(expr, typeRef.ElementType, CastKind.ReferenceToLValue);
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

    private SemaExpr LValueToRValue(SemaExpr expr, bool stripReference)
    {
        if (expr.IsErrored) return expr;

        if (expr.IsLValue)
            expr = WrapWithCast(expr, expr.Type, CastKind.LValueToRValue);
        
        if (stripReference && expr.Type.Type is SemaTypeReference typeRef)
        {
            expr = WrapWithCast(expr, typeRef.ElementType, CastKind.ReferenceToLValue);
            expr = LValueToRValue(expr, false);
        }

        return expr;
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
            _scope = sema.Scope;
        }

        public void Dispose()
        {
            if (!_sema._scopes.TryPeek(out var scope))
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

        public ScopeDisposable(Sema sema)
        {
            _sema = sema;
            _scope = new Scope();
            sema._scopes.Push(_scope);
        }

        public void Dispose()
        {
            if (!_sema._scopes.TryPop(out var scope))
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
            sema._functions.Push(function);
        }

        public void Dispose()
        {
            if (!_sema._functions.TryPop(out var function))
            {
                _sema.Context.Diag.ICE($"Exited a {nameof(CurrentFunctionDisposable)}, but there were no functions");
                throw new UnreachableException();
            }

            _sema.Context.Assert(ReferenceEquals(function, _function), $"Exited a {nameof(CurrentFunctionDisposable)}, but the function was not the correct function");
        }
    }
}
#pragma warning restore CA1822 // Mark members as static
