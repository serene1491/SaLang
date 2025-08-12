using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private RuntimeResult EvalTableAccess(TableAccess ta)
    {
        var targetRes = EvalExpr(ta.TableExpr);
        if (targetRes.IsError) return targetRes;

        var target = targetRes.Value;
        if (target.Kind != ValueKind.Table)
            return RuntimeResult.Normal(Value.Nil());

        if (target.Table.TryGetValue(ta.Key, out var val))
            return RuntimeResult.Normal(val);

        return RuntimeResult.Normal(Value.Nil());
    }
}
