using SaLang.Common;
using SaLang.Syntax.Nodes;
using SaLang.Analyzers.Syntax;
namespace SaLang.Parsing;

public partial class Parser
{
    private SyntaxResult<VarDeclaration> ParseVarDeclaration()
    {
        TraceEnter("ParseVarDeclaration");
        var name = Curr.Lexeme;
        Match(TokenType.Identifier);
        Match(TokenType.Symbol, "=");
        var exprRes = ParseExpr();
        if (exprRes.TryGetError(out var err))
            return SyntaxResult<VarDeclaration>.Fail(err);

        var decl = new VarDeclaration
        {
            Name = name,
            Expr = exprRes.Expect(),
            IsReadonly = false
        };
        
        TraceExit();
        return SyntaxResult<VarDeclaration>.Ok(decl);
    }
}