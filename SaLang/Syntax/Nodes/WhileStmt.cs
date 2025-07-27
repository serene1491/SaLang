using System.Collections.Generic;

namespace SaLang.Syntax.Nodes;

public class WhileStmt : Ast
{
    public Ast Condition { get; set; }
    public List<Ast> Body { get; set; }
}
