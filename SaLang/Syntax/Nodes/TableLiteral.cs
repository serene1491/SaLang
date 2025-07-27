using System.Collections.Generic;
namespace SaLang.Syntax.Nodes;

public class TableLiteral : Ast
{
    public Dictionary<string, Ast> Pairs = new();
}
