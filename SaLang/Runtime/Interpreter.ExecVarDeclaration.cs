using SaLang.Analyzers.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private RuntimeResult ExecVarDeclaration(VarDeclaration vd)
    {
        var res = EvalExpr(vd.Expr);
        if (res.IsError)
            return res;
        
        _env.Define(vd.Name, res.Value, vd.IsReadonly);
        return RuntimeResult.Nothing();
    }
}
