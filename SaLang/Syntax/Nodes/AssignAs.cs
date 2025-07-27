namespace SaLang.Syntax.Nodes;

public class AssignAs : Ast
{
    public required Ast Expr;
    public required string Name;
}
