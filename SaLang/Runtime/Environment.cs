using System;
using System.Collections.Generic;
namespace SaLang.Runtime;

public class Environment {
    private readonly Dictionary<string, (Value value, bool isReadonly)> _vals = new();
    public readonly Environment Parent;
    public Environment(Environment parent = null)
    {
        Parent = parent;
    }

    public void Define(string n, Value v, bool isReadonly = false)
        => _vals[n] = (v, isReadonly);
    
    public void Assign(string n, Value v)
    {
        if (_vals.TryGetValue(n, out var entry))
        {
            if (entry.isReadonly) throw new Exception($"Cannot assign to readonly variable '{n}'");

            _vals[n] = (v, entry.isReadonly);
        }
        else if (Parent != null) Parent.Assign(n, v);
        else throw new Exception($"Undefined var '{n}'");
    }
    
    public Value Get(string n)
    {
        if (_vals.TryGetValue(n, out var entry)) return entry.value;
        if (Parent != null) return Parent.Get(n);
        throw new Exception($"Undefined var '{n}'");
    }
}
