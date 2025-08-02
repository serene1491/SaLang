using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private RuntimeResult ExecVarAssign(VarAssign aa)
    {
        var res = EvalExpr(aa.Expr);
        if (res.IsError) return RuntimeResult.Error(res.Value);

        if (aa.Table != null)
        {
            var tblObjRes = EvalExpr(aa.Table.TableExpr);
            if (tblObjRes.IsError) return tblObjRes;
            var tblObj = tblObjRes.Value.Table;
            if (tblObj == null)
                return RuntimeResult.Error(Value.FromError(new Error(
                    ErrorCode.RuntimeInvalidFunctionCall,
                    errorStack: [.. _callStack],
                    args: [$"'{ExprToString(aa.Table.TableExpr)}' is not a table"]
                )));

            tblObj[aa.Table.Key] = res.Value;
            return RuntimeResult.Nothing();
        }
        else
        {
            var sem = _env.Assign(aa.Name, res.Value);
            if (sem.IsError)
                return RuntimeResult.Error(Value.FromError(new Error(
                    sem.Error.Value,
                    errorStack: [.. _callStack],
                    args: [aa.Name]
                )));
            return RuntimeResult.Nothing();
        }
    }
}
