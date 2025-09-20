using System.Collections.Generic;
using SaLang.Common;
using SaLang.Syntax.Nodes;
using SaLang.Analyzers.Syntax;
namespace SaLang.Parsing;

public partial class Parser
{
    private List<SyntaxResult<Ast>> ParseBlockBody(params string[] terminators)
    {
        var body = new List<SyntaxResult<Ast>>();
        var terms = new HashSet<string>(terminators);

        while (cur < _tokens.Count)
        {
            if (Curr.Type == TokenType.EOF)
                break;

            // If current token is one of the terminators, stop and let the caller consume it.
            if (Curr.Type == TokenType.Keyword && terms.Contains(Curr.Lexeme))
                break;

            // Otherwise parse a statement. Nested block parsers (if/function/do) will
            // consume their own 'end' tokens, so we shouldn't try to manage depth here.
            body.Add(ParseStmt());
        }

        return body;
    }
}