using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
using SaLang.Analyzers.Semantic;
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
            var tableValRes = EvalTableAccess(new TableAccess { Table = aa.Table.Table, Key = aa.Table.Key });
            if (tableValRes.IsError)
            {
                var baseTableRes = ResolveIdentifier(aa.Table.Table);
                if (baseTableRes.IsError)
                    return baseTableRes;

                var baseTable = baseTableRes.Value.Table;
                if (baseTable == null)
                    return RuntimeResult.Error(Value.FromError(new Error(
                        ErrorCode.RuntimeInvalidFunctionCall, errorStack: [.. _callStack],
                        args: [$"Variable '{aa.Table.Table}' is not a table"]
                    )));

                baseTable[aa.Table.Key] = res.Value;
                return RuntimeResult.Nothing();
            }
            else
            {
                var tblRes = ResolveIdentifier(aa.Table.Table);
                if (tblRes.IsError) return tblRes;

                var tbl = tblRes.Value.Table;
                if (tbl == null)
                    return RuntimeResult.Error(Value.FromError(new Error(
                        ErrorCode.RuntimeInvalidFunctionCall, errorStack: [.. _callStack],
                        args: [$"Variable '{aa.Table.Table}' is not a table"]
                    )));

                tbl[aa.Table.Key] = res.Value;
                return RuntimeResult.Nothing();
            }
        }
        else
        {
            SemanticResult r = _env.Assign(aa.Name, res.Value);
            if (r.IsError)
                return RuntimeResult.Error(Value.FromError(new Error(
                    r.Error.Value, errorStack: [.. _callStack],
                    args: [aa.Name]
                )));
            
            return RuntimeResult.Nothing();
        }
    }
}
