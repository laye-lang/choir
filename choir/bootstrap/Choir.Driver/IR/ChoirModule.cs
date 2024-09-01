using System.Text;

namespace Choir.IR;

public sealed class ChoirModule(ChoirContext context, string name)
{
    internal readonly List<ChoirValue> _globals = [];

    internal readonly Dictionary<string, List<ChoirValue>> _decls = [];

    public ChoirContext Context { get; } = context;
    public string Name { get; } = name;

    public void AddGlobal(ChoirValue globalValue)
    {
        // in-order globals
        _globals.Add(globalValue);

        if (!_decls.TryGetValue(globalValue.Name, out var decls))
            _decls[globalValue.Name] = decls = new List<ChoirValue>(2);

        if (globalValue is ChoirFunction function)
        {
            if (function.Blocks.Count > 0)
            {
                var forwardDecls = decls.Where(d => d is ChoirFunction).Cast<ChoirFunction>().Where(f => f.Blocks.Count == 0).ToArray();
                if (forwardDecls.Length != decls.Count)
                    Context.Diag.Error($"Redefinition of global {globalValue.Name}");

                foreach (var fd in forwardDecls)
                    fd.Definition = function;
            }
        }
        else
        {
            if (decls.Count != 0)
                Context.Diag.Error(globalValue.Location, "Can only forward-declare functions in Choir source code, and redeclarations are not allowed.");
        }

        decls.Add(globalValue);
    }

    public string ToIRString()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"// Choir Module - '{Name}'");

        for (int i = 0; i < _globals.Count; i++)
        {
            if (i > 0) builder.AppendLine();
            builder.AppendLine(_globals[i].ToIRString());
        }

        return builder.ToString();
    }
}
