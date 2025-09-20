using SaLang.Common;
using SaLang.Syntax.Nodes;
using SaLang.Analyzers.Syntax;
namespace SaLang.Parsing;

public partial class Parser
{
    private SyntaxResult<ForInStmt> ParseForIn()
    {
        TraceEnter("ParseForIn");

        var varName = Curr.Lexeme;
        Match(TokenType.Identifier);
        Match(TokenType.Keyword, "in");
        
        var iterable = ParseExpr();
        if (iterable.TryGetError(out var iErr))
            return SyntaxResult<ForInStmt>.Fail(iErr);

        Match(TokenType.Keyword, "do");

        var rawBody = ParseBlockBody("end");
        var bodyRes = rawBody.Sequence();
        if (bodyRes.TryGetError(out var err))
            return SyntaxResult<ForInStmt>.Fail(err);
        var body = bodyRes.Expect();

        Match(TokenType.Keyword, "end");
        TraceExit();

        return SyntaxResult<ForInStmt>.Ok(new ForInStmt
        {
            VarName = varName,
            Iterable = iterable.Expect(),
            Body = body
        });
    }
}