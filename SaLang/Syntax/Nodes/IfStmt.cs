using System.Collections.Generic;

namespace SaLang.Syntax.Nodes;

public class IfStmt : Ast
{
    public List<IfClause> Clauses { get; set; } = new();
} 
