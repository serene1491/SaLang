using System.Collections.Generic;
using SaLang.Common;
using SaLang.Syntax.Nodes;
using SaLang.Analyzers.Syntax;
namespace SaLang.Parsing;

public partial class Parser
{
    /// <summary>
    /// Reads from the current token until one of the specified terminators, respecting nested blocks
    /// </summary>
    private List<SyntaxResult<Ast>> ParseBlockBody(bool alreadyInside, params string[] terminators)
    {
        var body = new List<SyntaxResult<Ast>>();
        int depth = alreadyInside? 1 : 0;
        var terms = new HashSet<string>(terminators) { "end" };

        while (cur < _tokens.Count)
        {
            if (Curr.Type == TokenType.EOF)
                break;

            if (Curr.Type == TokenType.Keyword &&
                (Curr.Lexeme == "function" || Curr.Lexeme == "do" || Curr.Lexeme == "if"))
            {
                depth++;
                body.Add(ParseStmt());
                continue;
            }

            if (depth == 0 && Curr.Type == TokenType.Keyword && terms.Contains(Curr.Lexeme))
                break;

            if (Curr.Type == TokenType.Keyword && Curr.Lexeme == "end")
            {
                if (depth > 0)
                {
                    depth--; cur++;
                    continue;
                }
                else
                    break;
            }

            body.Add(ParseStmt());
        }

        return body;
    }
}