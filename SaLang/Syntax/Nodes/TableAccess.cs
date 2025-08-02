namespace SaLang.Syntax.Nodes;

public class TableAccess : Ast
{
    public required Ast TableExpr;
    public required string Key;
}