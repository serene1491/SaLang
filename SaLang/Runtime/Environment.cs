using System.Collections.Generic;
using SaLang.Analyzers.Semantic;
using SaLang.Analyzers;
namespace SaLang.Runtime;

public class Environment {
    private readonly Dictionary<string, (Value value, bool isReadonly)> _vals = new();
    public readonly Environment Parent;
    public Environment(Environment parent = null)
    {
        Parent = parent;
    }

    public SemanticResult Define(string n, Value v, bool isReadonly = false)
    {
        if (_vals.TryGetValue(n, out var _)) return SemanticResult.Fail(ErrorCode.SemanticDuplicateSymbol);

        _vals[n] = (v, isReadonly);
        return SemanticResult.Nothing();
    }

    public SemanticResult Assign(string n, Value v)
    {
        if (_vals.TryGetValue(n, out var entry))
        {
            if (entry.isReadonly) return SemanticResult.Fail(ErrorCode.SemanticReadonlyAssignment);

            _vals[n] = (v, entry.isReadonly);
            return SemanticResult.Nothing();
        }
        else if (Parent != null)
            return Parent.Assign(n, v);
        else
            return SemanticResult.Fail(ErrorCode.SemanticUndefinedVariable);
    }

    public Value? Get(string n)
    {
        if (_vals.TryGetValue(n, out var entry)) return entry.value;
        if (Parent != null) return Parent.Get(n);
        else return null;
    }
}
