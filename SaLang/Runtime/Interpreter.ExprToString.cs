using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private static string ExprToString(Ast expr) => expr switch
    {
        Ident i => i.Name,
        TableAccess ta => $"{ExprToString(ta.TableExpr)}.{ta.Key}",
        _ => "<anonymous>"
    };
}
