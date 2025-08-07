using System.Collections.Generic;
using SaLang.Common;
namespace SaLang.Syntax.Nodes;

public class CallExpr : Ast
{
    public required Ast Callee;
    public required List<Ast> Args;
    public required Span Span { get; set; }
}
