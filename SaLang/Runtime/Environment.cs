using System;
using System.Collections.Generic;
namespace SaLang.Runtime;

public class Environment {
    private readonly Dictionary<string, Value> vals = new();
    private readonly Environment _parent;
    public Environment(Environment parent = null)
    {
        _parent = parent;
    }

    public void Define(string n, Value v) => vals[n] = v;
    
    public void Assign(string n, Value v)
    {
        if (vals.ContainsKey(n)) vals[n] = v;
        else if (_parent != null) _parent.Assign(n, v);
        else throw new Exception($"Undefined var {n}");
    }
    
    public Value Get(string n)
    {
        if (vals.TryGetValue(n, out Value value)) return value;
        if (_parent != null) return _parent.Get(n);
        throw new Exception($"Undefined var {n}");
    }
}
