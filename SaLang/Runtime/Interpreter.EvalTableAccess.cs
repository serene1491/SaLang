using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private RuntimeResult EvalTableAccess(TableAccess ta)
    {
        var tblObjRes = EvalExpr(ta.TableExpr);
        if (tblObjRes.IsError)
            return tblObjRes;
        var tblObj = tblObjRes.Value.Table;
        if (tblObj != null && tblObj.TryGetValue(ta.Key, out var v))
            return RuntimeResult.Normal(v);

        return RuntimeResult.Error(Value.FromError(new Error(
            ErrorCode.RuntimeKeyNotFound,
            errorStack: [.. _callStack],
            args: [ta.Key, ExprToString(ta.TableExpr)]
        )));
    }
}
