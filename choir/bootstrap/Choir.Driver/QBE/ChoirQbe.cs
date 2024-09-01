using System.Diagnostics;
using System.Text;

using Choir.Front.Laye.Sema;
using Choir.IR;

namespace Choir.Qbe;

public static class ChoirIR_QbeExtensions
{
    private enum IncludeType
    {
        Never,
        IfNotLiteral,
        Always,
    }

    public static string ToQbeString(this ChoirModule module)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# Choir Module - '{module.Name}'");

        for (int i = 0; i < module._globals.Count; i++)
        {
            if (i > 0) builder.AppendLine();
            builder.AppendLine(module._globals[i].ToQbeString());
        }

        return builder.ToString();
    }

    private static string ToQbeInstString(this ChoirInst inst)
    {
        switch (inst)
        {
            default: throw new NotImplementedException($"Unimplemented Choir inst {inst.GetType().Name}");

            case ChoirInstAdd add:
            {
                return $"add {add.Left.ToQbeValueString(IncludeType.IfNotLiteral)}, {add.Left.ToQbeValueString(IncludeType.IfNotLiteral)}";
            }

            case ChoirInstRet ret: return $"ret {ret.Value.ToQbeValueString(IncludeType.Never)}";
            case ChoirInstRetVoid: return $"ret";
        }
    }

    public static string ToQbeString(this ChoirValue value)
    {
        switch (value)
        {
            default: throw new NotImplementedException($"Unimplemented Choir value {value.GetType().Name}");

            case ChoirInst inst:
            {
                string instText = inst.ToQbeInstString();
                if (inst.Type.Type is not ChoirTypeVoid)
                    return $"{inst.ToValueString(false)} ={inst.Type.ToQbeString()} {instText}";
                return instText;
            }

            case ChoirFunction function:
            {
                var builder = new StringBuilder();
                if (function.Linkage == Linkage.Exported)
                    builder.Append("export ");

                builder.Append("function ");

                if (function.ReturnType.Type is not ChoirTypeVoid)
                    builder.Append(function.ReturnType.ToQbeString()).Append(' ');

                builder.Append(function.Sigil).Append(function.Name).Append('(');
                for (int i = 0; i < function.Params.Count; i++)
                {
                    if (i > 0) builder.Append(", ");
                    builder.Append(function.Params[i].ToQbeValueString(IncludeType.Always));
                }

                builder.Append(')');

                if (function.Blocks.Count != 0)
                {
                    builder.AppendLine(" {");
                    for (int i = 0; i < function.Blocks.Count; i++)
                    {
                        if (i > 0) builder.AppendLine();
                        builder.AppendLine(function.Blocks[i].ToQbeString());
                    }

                    builder.Append('}');
                }

                return builder.ToString();
            }

            case ChoirBlock block:
            {
                var builder = new StringBuilder();

                builder.Append(block.Sigil).AppendLine(block.Name);
                for (int i = 0; i < block.Instructions.Count; i++)
                {
                    if (i > 0) builder.AppendLine();
                    builder.Append("  ").Append(block.Instructions[i].ToQbeInstString());
                }

                return builder.ToString();
            }
        }
    }

    private static string ToQbeValueStringImpl(this ChoirValue value)
    {
        switch (value)
        {
            default: return $"{value.Sigil}{value.Name}";
            case ChoirValueLiteralInteger literalInteger: return literalInteger.Value.ToString();
        }
    }

    private static string ToQbeValueString(this ChoirValue value, IncludeType includeType = IncludeType.Always)
    {
        bool shouldIncludeType = includeType switch
        {
            IncludeType.Never => false,
            IncludeType.Always => true,
            IncludeType.IfNotLiteral => value is not ChoirValueLiteralInteger,
            _ => throw new UnreachableException(),
        };

        return shouldIncludeType ? $"{value.Type.Type.ToQbeString()} {value.ToQbeValueStringImpl()}" : $"{value.ToQbeValueStringImpl()}";
    }

    public static string ToQbeString(this ChoirTypeLoc typeLoc) => typeLoc.Type.ToQbeString();
    public static string ToQbeString(this ChoirType type)
    {
        switch (type)
        {
            case ChoirTypeVoid:
            default: return "";

            case ChoirTypeI32: return "w";
            case ChoirTypeI64: return "l";
        }
    }
}
