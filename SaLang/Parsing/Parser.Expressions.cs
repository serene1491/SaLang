using System.Collections.Generic;
using SaLang.Common;
using SaLang.Syntax.Nodes;
using SaLang.Analyzers.Syntax;
using SaLang.Analyzers;
namespace SaLang.Parsing;

public partial class Parser
{
    private SyntaxResult<Ast> ParseExpr() => ParseBinary(0);

    private static readonly Dictionary<string, int> precedences = new()
    {
        ["||"] = 0,
        ["&&"] = 0,

        [">"]  = 1, ["<"]  = 1,
        ["=="] = 1, ["<="] = 1,
        [">="] = 1, ["!="] = 1,
        ["~="] = 1,

        ["+"]  = 2, ["-"]  = 2,
        [".."] = 2,

        ["*"]  = 3, ["/"]  = 3,
        ["%"]  = 3,

        ["#"]  = 4, // Unary length operator (__len)
        ["!"]  = 4, // Unary logic negation operator (__not)
    };

    private SyntaxResult<Ast> ParseBinary(int parentPrecedence)
    {
        SyntaxResult<Ast> rawLeft = ParseUnary();
        if (!rawLeft.TryUnwrap(out var left, out var fail))
            return fail;

        while (true)
        {
            if (Curr.Type != TokenType.Symbol)
                break;
            string op = Curr.Lexeme;
            if (!precedences.TryGetValue(op, out int prec) || prec < parentPrecedence)
                break;
            
            var opToken = Curr;
            Match(TokenType.Symbol, op);
            var rightRes = ParseBinary(prec + 1);
            if (!rightRes.TryUnwrap(out var right, out var rfail))
                return rfail;
            string funcName = op switch
            {
                "+" => "__sum",
                "-" => "__sub",
                "*" => "__mul",
                "/" => "__div",
                "%" => "__module",
                ".." => "__concat",

                ">" => "__greater",
                "<" => "__less",

                "==" => "__equals",
                "<=" => "__lessEquals",
                ">=" => "__greaterEquals",
                "!=" => "__notEquals",
                "~=" => "__approxmate",

                "||" => "__or",
                "&&" => "__and",

                _ => null,
            };
            if (funcName == null)
                return SyntaxResult<Ast>.Fail(
                    ErrorCode.InternalUnsupportedExpressionType,
                    new object[] { op },
                    _trace
                );

            left = new CallExpr
            {
                Callee = new Ident { Name = funcName },
                Args = new List<Ast> { left, right },
                Span = new Span(
                    _sourceFile,
                    opToken.Line,
                    opToken.Column
                )
            };
        }
        return SyntaxResult<Ast>.Ok(left);
    }

    private SyntaxResult<Ast> ParseUnary()
    {
        var opToken = Curr;
        
        if (Match(TokenType.Symbol, "#"))
        {
            var operandRes = ParseUnary();
            if (!operandRes.TryUnwrap(out var operand, out var fail))
                return fail;

            return SyntaxResult<Ast>.Ok(
                new CallExpr
                {
                    Callee = new Ident { Name = "__len" },
                    Args = new List<Ast> { operand },
                    Span = new Span(
                        _sourceFile,
                        opToken.Line,
                        opToken.Column
                    )
                });
        }
        else if (Match(TokenType.Symbol, "!"))
        {
            var operandRes = ParseUnary();
            if (!operandRes.TryUnwrap(out var operand, out var fail))
                return fail;
            
            return SyntaxResult<Ast>.Ok(
                new CallExpr
                {
                    Callee = new Ident { Name = "__not" },
                    Args = new List<Ast> { operand },
                    Span = new Span(
                        _sourceFile,
                        opToken.Line,
                        opToken.Column
                    )
                });
        }
        else if (Match(TokenType.Symbol, "++"))
        {
            var operandRes = ParseUnary();
            if (!operandRes.TryUnwrap(out var operand, out var fail))
                return fail;
            
            return SyntaxResult<Ast>.Ok(
                new CallExpr
                {
                    Callee = new Ident { Name = "__sum" },
                    Args = new List<Ast> { operand, new LiteralNumber(){Value = 1} },
                    Span = new Span(
                        _sourceFile,
                        opToken.Line,
                        opToken.Column
                    )
                });
        }
        else if (Match(TokenType.Symbol, "--"))
        {
            var operandRes = ParseUnary();
            if (!operandRes.TryUnwrap(out var operand, out var fail))
                return fail;
            
            return SyntaxResult<Ast>.Ok(
                new CallExpr
                {
                    Callee = new Ident { Name = "__sub" },
                    Args = new List<Ast> { operand, new LiteralNumber(){Value = 1} },
                    Span = new Span(
                        _sourceFile,
                        opToken.Line,
                        opToken.Column
                    )
                });
        }
        return ParsePrimary();
    }

    private SyntaxResult<Ast> ParsePrimary()
    {
        Ast expr;
        
        if (Match(TokenType.Keyword, "nil"))
            expr = new LiteralNil();
        else if (Match(TokenType.Keyword, "true"))
            expr = new LiteralBool { Value = true };
        else if (Match(TokenType.Keyword, "false"))
            expr = new LiteralBool { Value = false };
        
        else if (Match(TokenType.Number))
            expr = new LiteralNumber { Value = double.Parse(_tokens[cur - 1].Lexeme) };
        else if (Match(TokenType.String))
            expr = new LiteralString { Value = _tokens[cur - 1].Lexeme };
        else if (Match(TokenType.Identifier))
        {
            expr = new Ident { Name = _tokens[cur - 1].Lexeme };

            while (Match(TokenType.Symbol, "."))
            {
                var key = Curr.Lexeme;
                Match(TokenType.Identifier);
                expr = new TableAccess
                {
                    TableExpr = expr,
                    Key       = key
                };
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
                var p = ParseExpr();
                if (!p.TryUnwrap(out var value, out var fail))
                    return fail;

                tbl.Pairs[k] = value;
                Match(TokenType.Symbol, ",");
            }
            expr = tbl;
        }
        else if (Match(TokenType.Symbol, "("))
        {
            var rawExpr = ParseExpr();
            if (!rawExpr.TryUnwrap(out var e, out var fail))
                return fail;
            expr = e;

            Match(TokenType.Symbol, ")");
        }
        else
            return SyntaxResult<Ast>.Fail(
                ErrorCode.SyntaxUnexpectedToken,
                new object[] { Curr.Lexeme },
                _trace
            );

        while (Match(TokenType.Symbol, "("))
        {
            var openParen = _tokens[cur - 1];
            
            var args = new List<Ast>();
            if (!Match(TokenType.Symbol, ")"))
            {
                do{
                    var eRaw = ParseExpr();
                    if (!eRaw.TryUnwrap(out var e, out var rfail))
                        return rfail;

                    args.Add(e);
                } while (Match(TokenType.Symbol, ","));
                Match(TokenType.Symbol, ")");
            }
            expr = new CallExpr
            {
                Callee = expr,
                Args = args,
                Span = new Span(
                    _sourceFile,
                    openParen.Line,
                    openParen.Column
                )
            };
        }

        return SyntaxResult<Ast>.Ok(expr);
    }
}