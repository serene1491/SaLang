using SaLang.Common;
using SaLang.Syntax.Nodes;
using SaLang.Analyzers.Syntax;
namespace SaLang.Parsing;

public partial class Parser
{
    private SyntaxResult<WhileStmt> ParseWhile()
    {
        TraceEnter("ParseWhile");
        var conditionRes = ParseExpr();
        if (conditionRes.TryGetError(out var err))
            return SyntaxResult<WhileStmt>.Fail(err);
        
        Match(TokenType.Keyword, "do");

        var rawBody = ParseBlockBody(alreadyInside: true, "end");
        var bodyRes = rawBody.Sequence();
        if (bodyRes.TryGetError(out var bErr))
            return SyntaxResult<WhileStmt>.Fail(bErr);
        var body = bodyRes.Expect();

        Match(TokenType.Keyword, "end");
        TraceExit();

        return SyntaxResult<WhileStmt>.Ok(new WhileStmt
        {
            Condition = conditionRes.Expect(),
            Body = body
        });
    }
}