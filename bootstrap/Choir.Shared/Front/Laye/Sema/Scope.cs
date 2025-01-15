using System.Collections;

namespace Choir.Front.Laye.Sema;

public sealed class Scope(Scope? parent = null)
    : IEnumerable<(string Name, IReadOnlyList<SemaDeclNamed> Symbols)>, IEquatable<Scope>
{
    public Scope? Parent { get; } = parent;
    public int Count => _symbols.Count;

    public SemaDeferStackNode? CurrentDefer { get; set; }

    private readonly Dictionary<string, HashSet<SemaDeclNamed>> _symbols = [];

    public IReadOnlyList<SemaDeclNamed> LookUp(string name) => [.. GetDeclSet(name)];
    private HashSet<SemaDeclNamed> GetDeclSet(string name)
    {
        if (!_symbols.TryGetValue(name, out var symbols))
            _symbols[name] = symbols = [];
        return symbols;
    }

    public bool AddDecl(SemaDeclNamed entity)
    {
        var decls = GetDeclSet(entity.Name);

        bool result = true;
        if (decls.Count > 0)
        {
            bool canOverload = entity is SemaDeclFunction && decls.All(d => d is SemaDeclFunction);
            if (!canOverload)
            {
                result = false;
                decls.Clear();
            }
        }

        decls.Add(entity);
        return result;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<(string Name, IReadOnlyList<SemaDeclNamed> Symbols)> GetEnumerator()
    {
        return _symbols.Select(kv => (kv.Key, (IReadOnlyList<SemaDeclNamed>)[.. kv.Value])).GetEnumerator();
    }

    public override int GetHashCode() => _symbols.GetHashCode();
    public override bool Equals(object? obj) => obj is Scope other && Equals(other);
    public bool Equals(Scope? other)
    {
        return ReferenceEquals(this, other);
    }
}
