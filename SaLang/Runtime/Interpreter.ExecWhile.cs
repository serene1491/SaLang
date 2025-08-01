using SaLang.Analyzers.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private RuntimeResult ExecWhile(WhileStmt stmt)
    {
        var originalEnv = _env;

        while (true)
        {
            var condRes = EvalExpr(stmt.Condition);
            if (condRes.IsError) 
            {
                _env = originalEnv;
                return RuntimeResult.Error(condRes.Value);
            }
            if (!IsTruthy(condRes.Value)) 
                break;

            try
            {
                _env = new Environment(originalEnv);

                foreach (var s in stmt.Body)
                {
                    var res = ExecStmt(s);
                    if (res.IsError || res.IsReturn)
                        return res;
                }
            }
            finally
            {
                _env = originalEnv;
            }
        }

        _env = originalEnv;
        return RuntimeResult.Nothing();
    }
}
