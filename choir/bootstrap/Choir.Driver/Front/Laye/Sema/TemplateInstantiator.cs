using System.Diagnostics;

namespace Choir.Front.Laye.Sema;

public sealed class TemplateInstantiator(Sema sema, Dictionary<SemaDeclTemplateParameter, BaseSemaNode> args)
{
    public Sema Sema { get; } = sema;
    public ChoirContext Context { get; } = sema.Context;
    public Dictionary<SemaDeclTemplateParameter, BaseSemaNode> Args { get; } = args;

    public BaseSemaNode Instantiate(BaseSemaNode node)
    {
        switch (node)
        {
            case SemaDecl decl: return InstantiateDecl(decl);
            case SemaStmt stmt: return InstantiateStmt(stmt);
            case SemaExpr expr: return InstantiateExpr(expr);
            case SemaType type: return InstantiateType(type);
            default:
            {
                Context.Unreachable($"unknown sema node kind {node.GetType().Name}");
                throw new UnreachableException();
            }
        }
    }

    public SemaDecl InstantiateDecl(SemaDecl decl)
    {
        switch (decl)
        {
            default:
            {
                Context.Unreachable($"unknown sema decl kind {decl.GetType().Name}");
                throw new UnreachableException();
            }
        }
    }

    public SemaStmt InstantiateStmt(SemaStmt stmt)
    {
        switch (stmt)
        {
            default:
            {
                Context.Unreachable($"unknown sema stmt kind {stmt.GetType().Name}");
                throw new UnreachableException();
            }
        }
    }

    public SemaExpr InstantiateExpr(SemaExpr expr)
    {
        switch (expr)
        {
            default:
            {
                Context.Unreachable($"unknown sema expr kind {expr.GetType().Name}");
                throw new UnreachableException();
            }
        }
    }

    public SemaType InstantiateType(SemaType type)
    {
        switch (type)
        {
            default:
            {
                Context.Unreachable($"unknown sema type kind {type.GetType().Name}");
                throw new UnreachableException();
            }
        }
    }

    public SemaTypeQual InstantiateType(SemaTypeQual type)
    {
        return InstantiateType(type.Type).Qualified(type.Location, type.Qualifiers);
    }

    #region Decls

    public SemaDeclParam InstantiateParam(SemaDeclParam n)
    {
        var type = InstantiateType(n.ParamType);
        var param = new SemaDeclParam(n.Location, n.Name, type);
        // cache/store in the procedure scope
        return param;
    }

    #endregion

    #region Stmts

    #endregion

    #region Exprs

    #endregion

    #region Types

    public SemaTypeArray InstantiateArrayType(SemaTypeArray n)
    {
        throw new NotImplementedException();
        //return new SemaTypeArray(InstantiateType(n.ElementType), n.Lengths);
    }

    #endregion
}
