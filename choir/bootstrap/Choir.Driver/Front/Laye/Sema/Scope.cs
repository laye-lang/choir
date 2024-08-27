using System.Collections;

namespace Choir.Front.Laye.Sema;

public abstract record class Symbol;
public sealed record class NamespaceSymbol(Scope Symbols) : Symbol;
public sealed record class EntitySymbol(SemaDecl Entity) : Symbol;

public sealed class Scope(Scope? parent = null)
    : IEnumerable<(string Name, IReadOnlyList<Symbol> Symbols)>, IEquatable<Scope>
{
    public Scope? Parent { get; } = parent;
    public int Count => _symbols.Count;

    private readonly Dictionary<string, HashSet<Symbol>> _symbols = [];

    public IReadOnlyList<Symbol> GetSymbols(string name) => [.. GetSymbolSet(name)];
    private HashSet<Symbol> GetSymbolSet(string name)
    {
        if (!_symbols.TryGetValue(name, out var symbols))
            _symbols[name] = symbols = [];
        return symbols;
    }

    public void AddSymbol(string name, Symbol symbol)
    {
        GetSymbolSet(name).Add(symbol);
    }

    public void AddNamespace(string name, Scope @namespace)
    {
        GetSymbolSet(name).Add(new NamespaceSymbol(@namespace));
    }

    public void AddDecl(string name, SemaDecl entity)
    {
        GetSymbolSet(name).Add(new EntitySymbol(entity));
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<(string Name, IReadOnlyList<Symbol> Symbols)> GetEnumerator()
    {
        return _symbols.Select(kv => (kv.Key, (IReadOnlyList<Symbol>)[.. kv.Value])).GetEnumerator();
    }

    public override int GetHashCode() => _symbols.GetHashCode();
    public override bool Equals(object? obj) => obj is Scope other && Equals(other);
    public bool Equals(Scope? other)
    {
        return ReferenceEquals(this, other);
    }
}
