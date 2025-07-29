namespace SaLang.Syntax.Nodes;

public class VarDeclaration : Ast
{
    public required string Name;
    public required bool IsReadonly;
    public required Ast Expr;
}
