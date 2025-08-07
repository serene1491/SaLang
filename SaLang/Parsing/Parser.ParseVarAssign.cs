using SaLang.Syntax.Nodes;
using SaLang.Analyzers.Syntax;
using SaLang.Analyzers;
namespace SaLang.Parsing;

public partial class Parser
{
    private SyntaxResult<VarAssign> ParseVarAssign(Ast acess)
    {
        TraceEnter("ParseVarAssign");
        if (acess is Ident id)
        {
            var rightRes = ParseExpr();
            if (rightRes.TryGetError(out var err))
                return SyntaxResult<VarAssign>.Fail(err);
                
            return SyntaxResult<VarAssign>.Ok(new VarAssign
            {
                Name = id.Name,
                Expr = rightRes.Expect()
            });
        }
        else if (acess is TableAccess ta)
        {
            var rightRes = ParseExpr();
            if (rightRes.TryGetError(out var err))
                return SyntaxResult<VarAssign>.Fail(err);
            
            return SyntaxResult<VarAssign>.Ok(
                new VarAssign
                {
                    Table = ta,
                    Name = ta.Key,
                    Expr = rightRes.Expect()
                });
        }

        TraceExit();
        return SyntaxResult<VarAssign>.Fail(
            ErrorCode.SyntaxUnexpectedToken,
            new[] { Curr.Lexeme },
            _trace
        );
    }
}