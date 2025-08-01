using SaLang.Analyzers.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private RuntimeResult ExecIf(IfStmt iff)
    {
        var originalEnv = _env;

        foreach (var clause in iff.Clauses)
        {
            if (clause.Condition != null)
            {
                var condRes = EvalExpr(clause.Condition);
                if (condRes.IsError)
                    return RuntimeResult.Error(condRes.Value);
                if (!IsTruthy(condRes.Value))
                    continue;
            }

            try
            {
                _env = new Environment(originalEnv);

                foreach (var stmt in clause.Body)
                {
                    var r = ExecStmt(stmt);
                    if (r.IsError || r.IsReturn)
                        return r;
                }
            }
            finally
            {
                _env = originalEnv;
            }

            return RuntimeResult.Nothing();
        }

        return RuntimeResult.Nothing();
    }
}
