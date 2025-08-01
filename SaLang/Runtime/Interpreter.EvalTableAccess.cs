using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private RuntimeResult EvalTableAccess(TableAccess ta)
    {
        var tblVal = ResolveIdentifier(ta.Table);
        if (tblVal.IsError) return tblVal;

        var tbl = tblVal.Value.Table;
        if (tbl != null && tbl.TryGetValue(ta.Key, out var v))
            return RuntimeResult.Normal(v);

        return RuntimeResult.Error(Value.FromError(new Error(
            ErrorCode.RuntimeKeyNotFound, errorStack: [.. _callStack],
            args: [ta.Key, ta.Table]
        )));
    }
}
