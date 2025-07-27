using System.Collections.Generic;

namespace SaLang.Syntax.Nodes;

public class ForInStmt : Ast
{
    public string VarName { get; set; }
    public Ast Iterable { get; set; }
    public List<Ast> Body { get; set; }
}
