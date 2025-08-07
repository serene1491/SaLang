using SaLang.Common;
using SaLang.Syntax.Nodes;
using SaLang.Analyzers.Syntax;
namespace SaLang.Parsing;

public partial class Parser
{
    private SyntaxResult<IfStmt> ParseIf()
    {
        TraceEnter("ParseIf");
        var iff = new IfStmt();

        var cond = ParseExpr();
        if (cond.TryGetError(out var err))
            return SyntaxResult<IfStmt>.Fail(err);
        
        Match(TokenType.Keyword, "then");
        var rawThenStmts = ParseBlockBody(alreadyInside: false, "elseif", "else", "not", "end");
        var thenbodyRes = rawThenStmts.Sequence();
        
        if (thenbodyRes.TryGetError(out var tErr))
            return SyntaxResult<IfStmt>.Fail(tErr);
        
        iff.Clauses.Add(new IfClause {
            Condition = cond.Expect(),
            Body = thenbodyRes.Expect() });

        while (Match(TokenType.Keyword, "elseif"))
        {
            var elifCond = ParseExpr();
            if (elifCond.TryGetError(out var effErr))
                return SyntaxResult<IfStmt>.Fail(effErr);

            Match(TokenType.Keyword, "then");
            var rawElifStmts = ParseBlockBody(alreadyInside: false, "elseif", "else", "not", "end");
            var elifbodyRes = rawElifStmts.Sequence();

            if (elifbodyRes.TryGetError(out var fErr))
                return SyntaxResult<IfStmt>.Fail(fErr);
            
            iff.Clauses.Add(new IfClause
            {
                Condition = elifCond.Expect(),
                Body = elifbodyRes.Expect()
            });
        }

        if (Match(TokenType.Keyword, "else") || Match(TokenType.Keyword, "not"))
        {
            Match(TokenType.Keyword, "so");
            var rawElseStmts = ParseBlockBody(alreadyInside: false, "end");
            var elseBodyRes = rawElseStmts.Sequence();
            if (elseBodyRes.TryGetError(out var eErr))
                return SyntaxResult<IfStmt>.Fail(eErr);

            iff.Clauses.Add(new IfClause{
                Body = elseBodyRes.Expect()
            });
        }

        Match(TokenType.Keyword, "end");
        TraceExit();
        return SyntaxResult<IfStmt>.Ok(iff);
    }
}