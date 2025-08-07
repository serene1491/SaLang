using System.Collections.Generic;
using SaLang.Common;
using SaLang.Syntax.Nodes;
using SaLang.Analyzers.Syntax;
namespace SaLang.Parsing;

public partial class Parser
{
    private SyntaxResult<FuncDecl> ParseFunc(bool isUnsafe)
    {
        Match(TokenType.Keyword, "function");
        TraceEnter("ParseFunc");
        // Capture span of 'function' keyword
        var funcToken = _tokens[cur - 1];
        var span = new Span(
            _sourceFile,
            funcToken.Line + 1,
            funcToken.Column); // Count from 1 instead of 0 in funcToken.Line

        // Parse Table.Name
        var table = Curr.Lexeme;
        Match(TokenType.Identifier);
        Match(TokenType.Symbol, ".");
        var fname = Curr.Lexeme;
        Match(TokenType.Identifier);

        // Parameters
        Match(TokenType.Symbol, "(");
        var ps = new List<string>();
        while (!Match(TokenType.Symbol, ")"))
        {
            ps.Add(Curr.Lexeme);
            Match(TokenType.Identifier);
            Match(TokenType.Symbol, ",");
        }

        var rawBody = ParseBlockBody(alreadyInside: false, "end");
        var bodyRes = rawBody.Sequence();
        if (bodyRes.TryGetError(out var err))
            return SyntaxResult<FuncDecl>.Fail(err);
        
        Match(TokenType.Keyword, "end");

        //Console.WriteLine($"[ParseFunc] {table}.{fname} body statements: {bodyRes.Expect().Count}");
        //foreach (var s in bodyRes.Expect())
        //    Console.WriteLine("  → " + s.GetType().Name);

        TraceExit();
        return SyntaxResult<FuncDecl>.Ok(new FuncDecl
        {
            Span = span,
            Table = table,
            Unsafe = isUnsafe,
            Name = fname,
            Params = ps,
            Body = bodyRes.Expect()
        });
    }
}