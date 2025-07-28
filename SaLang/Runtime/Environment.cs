using System;
using System.Collections.Generic;
namespace SaLang.Runtime;

public class Environment {
    private readonly Dictionary<string, Value> vals = new();
    public readonly Environment Parent;
    public Environment(Environment parent = null)
    {
        Parent = parent;
    }

    public void Define(string n, Value v) => vals[n] = v;
    
    public void Assign(string n, Value v)
    {
        if (vals.ContainsKey(n)) vals[n] = v;
        else if (Parent != null) Parent.Assign(n, v);
        else throw new Exception($"Undefined var {n}");
    }
    
    public Value Get(string n)
    {
        if (vals.TryGetValue(n, out Value value)) return value;
        if (Parent != null) return Parent.Get(n);
        throw new Exception($"Undefined var {n}");
    }
}
