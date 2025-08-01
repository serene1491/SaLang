using System.Collections.Generic;
using SaLang.Analyzers.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private RuntimeResult EvalTable(TableLiteral tl)
    {
        var d = new Dictionary<string, Value>();
        foreach (var kv in tl.Pairs)
        {
            var v = EvalExpr(kv.Value);
            if (v.IsError) return v;
            d[kv.Key] = v.Value;
        }
        return RuntimeResult.Normal(Value.FromTable(d));
    }
}
