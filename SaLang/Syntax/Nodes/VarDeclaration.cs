namespace SaLang.Syntax.Nodes;

public class VarDeclaration : Ast
{
    public required string Name;
    public required Ast Expr;
}
