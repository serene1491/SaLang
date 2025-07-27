using System.Collections.Generic;

namespace SaLang.Syntax.Nodes;

public class IfClause : Ast
{
    public Ast Condition { get; set; } // NULLABLE <only for else!!>
    public List<Ast> Body { get; set; } = new();
}
