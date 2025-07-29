namespace SaLang.Syntax.Nodes;

public class VarAssign : Ast
{
    public TableAccess Table;
    public required Ast Expr;
    public required string Name;
}
