using SaLang.Syntax.Nodes;
using SaLang.Analyzers.Syntax;
namespace SaLang.Parsing;

public partial class Parser
{
    private SyntaxResult<ReturnStmt> ParseReturn()
    {
        TraceEnter("ParseReturn");
        var exprRes = ParseExpr();
        if (exprRes.TryGetError(out var err))
            return SyntaxResult<ReturnStmt>.Fail(err);
        
        TraceExit();
        return SyntaxResult<ReturnStmt>.Ok(new ReturnStmt {
            Expr = exprRes.Expect() });
    }
}