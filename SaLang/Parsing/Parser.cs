using System;
using System.Collections.Generic;
using SaLang.Syntax;
using SaLang.Common;
using SaLang.Syntax.Nodes;
namespace SaLang.Parsing;

// Parser
public class Parser
{
    private readonly string _sourceFile;
    private List<Token> _tokens = new();
    private int cur = 0;

    public Parser(string sourceFile = "<memory>")
    =>
        _sourceFile = sourceFile;

    private Token Curr => cur < _tokens.Count ? _tokens[cur] : _tokens[^1];

    private bool Match(TokenType t, string lex = null)
    {
        if (Curr.Type == t && (lex == null || Curr.Lexeme == lex))
        {
            cur++;
            return true;
        }
        return false;
    }

    public ProgramNode Parse(List<Token> tokens)
    {
        _tokens = tokens;
        cur = 0;
        var prog = new ProgramNode();
        while (!Match(TokenType.EOF)) prog.Stmts.Add(ParseStmt());
        return prog;
    }

    private Ast ParseStmt()
    {
        if (Match(TokenType.Keyword, "var"))      return ParseVar();
        if (Match(TokenType.Keyword, "function")) return ParseFunc();
        if (Match(TokenType.Keyword, "return"))   return ParseReturn();

        var expr = ParseExpr();
        if (Match(TokenType.Keyword, "as"))
        {
            var name = Curr.Lexeme;
            Match(TokenType.Identifier);
            return new AssignAs { Expr = expr, Name = name };
        }

        return new ExpressionStmt { Expr = expr };
    }

    private VarDeclaration ParseVar()
    {
        var name = Curr.Lexeme;
        Match(TokenType.Identifier);
        Match(TokenType.Symbol, "=");
        var e = ParseExpr();
        return new VarDeclaration { Name = name, Expr = e };
    }

    private FuncDecl ParseFunc()
    {
        // Capture span of 'function' keyword
        var funcToken = _tokens[cur - 1];
        var span = new Span(_sourceFile, funcToken.Line + 1, funcToken.Column); // Count from 1 instead of 0 in funcToken.Line

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

        var body = ParseBlockBody();

        return new FuncDecl
        {
            Span = span,
            Table = table,
            Name = fname,
            Params = ps,
            Body = body
        };
    }

    private ReturnStmt ParseReturn()
    {
        var expr = ParseExpr();
        return new ReturnStmt { Expr = expr };
    }

    private Ast ParseExpr() => ParseBinary(0);

    private static readonly Dictionary<string, int> precedences = new()
    {
        ["+"] = 1,
        ["-"] = 1,
        ["*"] = 2,
        ["/"] = 2,
        ["#"] = 3 // Unary length operator (len)
    };

    private Ast ParseBinary(int parentPrecedence)
    {
        Ast left = ParseUnary();

        while (true)
        {
            if (Curr.Type != TokenType.Symbol)
                break;
            string op = Curr.Lexeme;
            if (!precedences.TryGetValue(op, out int prec) || prec < parentPrecedence)
                break;

            Match(TokenType.Symbol, op);
            Ast right = ParseBinary(prec + 1);
            string funcName = op switch
            {
                "+" => "sum",
                "-" => "sub",
                "*" => "mul",
                "/" => "div",
                _ => throw new Exception($"Operator not implemented {op}")
            };

            left = new CallExpr { Callee = new Ident { Name = funcName }, Args = new List<Ast> { left, right } };
        }

        return left;
    }

    private Ast ParseUnary()
    {
        if (Match(TokenType.Symbol, "#"))
        {
            var operand = ParseUnary();
            return new CallExpr { Callee = new Ident { Name = "len" }, Args = new List<Ast> { operand } };
        }
        return ParsePrimary();
    }

    private Ast ParsePrimary()
    {
        Ast expr;
        if (Match(TokenType.Number))
            expr = new LiteralNumber { Value = double.Parse(_tokens[cur - 1].Lexeme) };
        else if (Match(TokenType.String))
            expr = new LiteralString { Value = _tokens[cur - 1].Lexeme };
        else if (Match(TokenType.Identifier))
        {
            string name = _tokens[cur - 1].Lexeme;
            expr = new Ident { Name = name };
            while (Match(TokenType.Symbol, "."))
            {
                string key = _tokens[cur].Lexeme;
                Match(TokenType.Identifier);
                expr = new TableAccess { Table = ((Ident)expr).Name, Key = key };
            }
        }
        else if (Match(TokenType.Symbol, "{"))
        {
            var tbl = new TableLiteral();
            while (!Match(TokenType.Symbol, "}"))
            {
                string k = Curr.Lexeme;
                Match(TokenType.Identifier);
                Match(TokenType.Symbol, "=");
                tbl.Pairs[k] = ParseExpr();
                Match(TokenType.Symbol, ",");
            }
            expr = tbl;
        }
        else if (Match(TokenType.Symbol, "("))
        {
            expr = ParseExpr();
            Match(TokenType.Symbol, ")");
        }
        else
            throw new Exception($"Syntax error at {Curr.Lexeme}");

        while (Match(TokenType.Symbol, "("))
        {
            var args = new List<Ast>();
            if (!Match(TokenType.Symbol, ")"))
            {
                do { args.Add(ParseExpr()); } while (Match(TokenType.Symbol, ","));
                Match(TokenType.Symbol, ")");
            }
            expr = new CallExpr { Callee = expr, Args = args };
        }

        return expr;
    }

    /// <summary>
    /// Reads from the current token until the `end` that closes the initial block, respecting nesting
    /// </summary>
    private List<Ast> ParseBlockBody()
    {
        var body = new List<Ast>();
        int depth = 1;

        while (depth > 0 && !Match(TokenType.EOF))
        {
            if (Curr.Type == TokenType.Keyword && Curr.Lexeme == "function" || Curr.Lexeme == "do")
            {
                depth++;
                body.Add(ParseStmt());
            }
            else if (Curr.Type == TokenType.Keyword && Curr.Lexeme == "end")
            {
                cur++;
                depth--;
            }
            else
                body.Add(ParseStmt());
        }

        return body;
    }
}
