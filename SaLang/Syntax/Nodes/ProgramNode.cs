using System.Collections.Generic;
namespace SaLang.Syntax.Nodes;

public class ProgramNode : Ast
{
    public List<Ast> Stmts = new();
}
