using System.Collections.Generic;
namespace SaLang.Syntax.Nodes;

public class FuncDecl : Ast
{
    public required Span Span;
    public required string Table;
    public required string Name;
    public required List<string> Params;
    public required List<Ast> Body;
}
