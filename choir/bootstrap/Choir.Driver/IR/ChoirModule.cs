using System.Text;

namespace Choir.IR;

public sealed class ChoirModule(ChoirContext context, string name)
{
    internal readonly Dictionary<string, ChoirValue> _decls = [];
    public IEnumerable<ChoirValue> GlobalDecls => _decls.Values;

    public ChoirContext Context { get; } = context;
    public string Name { get; } = name;

    public void AddGlobal(ChoirValue globalValue)
    {
        if (_decls.ContainsKey(globalValue.Name))
            Context.Diag.Error(globalValue.Location, $"Redeclaration of '{globalValue.Name}'.");

        _decls[globalValue.Name] = globalValue;
    }

    public string ToIRString()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"// Choir Module - '{Name}'");

        var globals = GlobalDecls.ToArray();
        for (int i = 0; i < globals.Length; i++)
        {
            if (i > 0) builder.AppendLine();
            builder.AppendLine(globals[i].ToIRString());
        }

        return builder.ToString();
    }
}
