﻿using System.Numerics;

namespace Choir.Front.Laye.Sema;

public enum EvaluatedConstantKind
{
    Integer,
    Float,
    String,
}

public readonly struct EvaluatedConstant
{
    public readonly EvaluatedConstantKind Kind;
    public readonly BigInteger IntegerValue;
    public readonly string StringValue;

    public EvaluatedConstant(BigInteger integerValue)
    {
        Kind = EvaluatedConstantKind.Integer;
        IntegerValue = integerValue;
        StringValue = "";
    }

    public EvaluatedConstant(string stringValue)
    {
        Kind = EvaluatedConstantKind.String;
        StringValue = stringValue;
    }
}

public sealed class ConstantEvaluator
{
    public bool TryEvaluate(SemaExpr expr, out EvaluatedConstant value)
    {
        value = default;
        switch (expr)
        {
            default: return false;

            case SemaExprLiteralInteger integerLiteral:
            {
                value = new EvaluatedConstant(integerLiteral.LiteralValue);
                return true;
            }

            case SemaExprLiteralString stringLiteral:
            {
                value = new EvaluatedConstant(stringLiteral.LiteralValue);
                return true;
            }
        }
    }
}
