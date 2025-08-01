using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private RuntimeResult ExecForIn(ForInStmt stmt)
    {
        var iterRes = EvalExpr(stmt.Iterable);
        if (iterRes.IsError) return RuntimeResult.Error(iterRes.Value);
        var iterable = iterRes.Value;
        if (iterable.Table == null)
            if (iterable.Table == null) return RuntimeResult.Error(Value.FromError(new Error(
                ErrorCode.SemanticInvalidArguments, errorStack: [.. _callStack],
                args: ["for-statement", "table", iterable.Kind]
            )));

        var originalEnv = _env;

        foreach (var kv in iterable.Table){
        try
        {
            _env = new Environment(originalEnv);
            _env.Define(stmt.VarName, kv.Value);

            foreach (var innerKv in iterable.Table)
            {
                var savedEnvInner = _env;
                try
                {
                    _env = new Environment(savedEnvInner);
                    _env.Define(stmt.VarName, innerKv.Value);
                    foreach (var s in stmt.Body)
                    {
                        var res = ExecStmt(s);
                        if (res.IsError || res.IsReturn) return res;
                    }
                }
                finally{
                    _env = savedEnvInner;
                }
            }
        }
        finally{
            _env = originalEnv;
        }}

        return RuntimeResult.Nothing();
    }
}
