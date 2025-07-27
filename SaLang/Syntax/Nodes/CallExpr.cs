using System.Collections.Generic;
namespace SaLang.Syntax.Nodes;

public class CallExpr : Ast
{
    public required Ast Callee;
    public required List<Ast> Args;
}
