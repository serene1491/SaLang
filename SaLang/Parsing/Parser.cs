using System.Collections.Generic;
using SaLang.Syntax;
using SaLang.Common;
using SaLang.Syntax.Nodes;
using SaLang.Analyzers.Syntax;
using SaLang.Analyzers;
using System;
namespace SaLang.Parsing;

// Parser
public class Parser
{
    #region Tracing
    private List<TraceFrame> _trace = new();
    private void TraceEnter(string prod, Token? at = null) => _trace.Add(Trace(prod, at));
    private void TraceExit() => _trace.RemoveAt(_trace.Count - 1);
    private TraceFrame Trace(string productionName, Token? at = null){
        var t = at ?? Curr;
        return new TraceFrame(productionName, _sourceFile, t.Line, t.Column);
    }
    #endregion

    private readonly string _sourceFile;
    private List<Token> _tokens = new();
    private int cur = 0;

    public Parser(string sourceFile = "<memory>")
    =>
        _sourceFile = sourceFile;

    private Token Curr => cur < _tokens.Count ? _tokens[cur] : _tokens[^1];

    private bool Check(TokenType t, string lex = null)
        => Curr.Type == t && (lex == null || Curr.Lexeme == lex);

    private bool Match(TokenType t, string lex = null)
    {
        if (Curr.Type == t && (lex == null || Curr.Lexeme == lex)){
            cur++;
            return true;
        }
        return false;
    }

    public SyntaxResult<ProgramNode> Parse(List<Token> tokens)
    {
        _tokens = tokens;
        cur = 0;
        var prog = new ProgramNode();

        while (!Match(TokenType.EOF))
        {
            var stmtRes = ParseStmt();
            if (stmtRes.TryGetError(out var err))
                return SyntaxResult<ProgramNode>.Fail(err.Code, err.Args, err.ErrorStack);

            stmtRes.TryGetValue(out var stmtNode);
            prog.Stmts.Add(stmtNode);
        }

        return SyntaxResult<ProgramNode>.Ok(prog);
    }

    private SyntaxResult<Ast> ParseStmt()
    {
        if (Match(TokenType.Keyword, "var"))
            return ParseVarDeclaration().Upcast<VarDeclaration, Ast>();
        if (Check(TokenType.Keyword, "function"))
            return ParseFunc(false).Upcast<FuncDecl, Ast>();
        if (Match(TokenType.Keyword, "unsafe"))
            return ParseFunc(true).Upcast<FuncDecl, Ast>();
        if (Match(TokenType.Keyword, "return"))
            return ParseReturn().Upcast<ReturnStmt, Ast>();
        if (Match(TokenType.Keyword, "if"))
            return ParseIf().Upcast<IfStmt, Ast>();
        if (Match(TokenType.Keyword, "for"))
            return ParseForIn().Upcast<ForInStmt, Ast>();
        if (Match(TokenType.Keyword, "while"))
            return ParseWhile().Upcast<WhileStmt, Ast>();
        
        TraceEnter("ParseExpr");

        var rawExpr = ParseExpr();
        if (!rawExpr.TryUnwrap(out var expr, out var fail))
        {
            TraceExit();
            return fail;
        }

        if (Match(TokenType.Symbol, "="))
        {
            TraceExit();
            return ParseVarAssign(expr).Upcast<VarAssign, Ast>();
        }
        if (Match(TokenType.Keyword, "as")) // Read-only var declaration
        {
            var name = Curr.Lexeme;
            Match(TokenType.Identifier);
            var decl = new VarDeclaration { Expr = expr, Name = name, IsReadonly = true };
            TraceExit();
            return SyntaxResult<Ast>.Ok(decl);
        }
        TraceExit();
        return SyntaxResult<Ast>.Ok(new ExpressionStmt { Expr = expr });
    }

    private SyntaxResult<VarDeclaration> ParseVarDeclaration()
    {
        TraceEnter("ParseVarDeclaration");
        var name = Curr.Lexeme;
        Match(TokenType.Identifier);
        Match(TokenType.Symbol, "=");
        var exprRes = ParseExpr();
        if (exprRes.TryGetError(out var err)) return SyntaxResult<VarDeclaration>.Fail(err);

        var decl = new VarDeclaration { Name = name, Expr = exprRes.Expect(), IsReadonly = false };
        TraceExit();
        return SyntaxResult<VarDeclaration>.Ok(decl);
    }

    private SyntaxResult<VarAssign> ParseVarAssign(Ast acess)
    {
        TraceEnter("ParseVarAssign");
        if (acess is Ident id)
        {
            var rightRes = ParseExpr();
            if (rightRes.TryGetError(out var err)) return SyntaxResult<VarAssign>.Fail(err);
            return SyntaxResult<VarAssign>.Ok(new VarAssign { Name = id.Name, Expr = rightRes.Expect() });
        }
        else if (acess is TableAccess ta)
        {
            var rightRes = ParseExpr();
            if (rightRes.TryGetError(out var err)) return SyntaxResult<VarAssign>.Fail(err);
            return SyntaxResult<VarAssign>.Ok(
                new VarAssign { Table = ta, Name = ta.Key, Expr = rightRes.Expect() });
        }
        TraceExit();
        return SyntaxResult<VarAssign>.Fail(
            ErrorCode.SyntaxUnexpectedToken,
            new[] { Curr.Lexeme },
            _trace
        );
    }

    private SyntaxResult<FuncDecl> ParseFunc(bool isUnsafe)
    {
        Match(TokenType.Keyword, "function");
        TraceEnter("ParseFunc");
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

        var rawBody = ParseBlockBody(alreadyInside: false, "end");
        var bodyRes = rawBody.Sequence();
        if (bodyRes.TryGetError(out var err)) return SyntaxResult<FuncDecl>.Fail(err);
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

    private SyntaxResult<IfStmt> ParseIf()
    {
        TraceEnter("ParseIf");
        var iff = new IfStmt();

        var cond = ParseExpr();
        if (cond.TryGetError(out var err)) return SyntaxResult<IfStmt>.Fail(err);
        
        Match(TokenType.Keyword, "then");
        var rawThenStmts = ParseBlockBody(alreadyInside: false, "elseif", "else", "not", "end");
        var thenbodyRes = rawThenStmts.Sequence();
        if (thenbodyRes.TryGetError(out var tErr)) return SyntaxResult<IfStmt>.Fail(tErr);
        
        iff.Clauses.Add(new IfClause { Condition = cond.Expect(), Body = thenbodyRes.Expect() });

        while (Match(TokenType.Keyword, "elseif"))
        {
            var elifCond = ParseExpr();
            if (elifCond.TryGetError(out var effErr)) return SyntaxResult<IfStmt>.Fail(effErr);

            Match(TokenType.Keyword, "then");
            var rawElifStmts = ParseBlockBody(alreadyInside: false, "elseif", "else", "not", "end");
            var elifbodyRes = rawElifStmts.Sequence();
            if (elifbodyRes.TryGetError(out var fErr)) return SyntaxResult<IfStmt>.Fail(fErr);
            
            iff.Clauses.Add(new IfClause { Condition = elifCond.Expect(), Body = elifbodyRes.Expect() });
        }

        if (Match(TokenType.Keyword, "else") || Match(TokenType.Keyword, "not"))
        {
            Match(TokenType.Keyword, "so");
            var rawElseStmts = ParseBlockBody(alreadyInside: false, "end");
            var elseBodyRes = rawElseStmts.Sequence();
            if (elseBodyRes.TryGetError(out var eErr)) return SyntaxResult<IfStmt>.Fail(eErr);

            iff.Clauses.Add(new IfClause { Body = elseBodyRes.Expect() });
        }

        Match(TokenType.Keyword, "end");
        TraceExit();
        return SyntaxResult<IfStmt>.Ok(iff);
    }

    private SyntaxResult<ForInStmt> ParseForIn()
    {
        TraceEnter("ParseForIn");
        var varName = Curr.Lexeme;
        Match(TokenType.Identifier);
        Match(TokenType.Keyword, "in");
        var iterable = ParseExpr();
        if (iterable.TryGetError(out var iErr)) return SyntaxResult<ForInStmt>.Fail(iErr);

        Match(TokenType.Keyword, "do");

        var rawBody = ParseBlockBody(alreadyInside: true, "end");
        var bodyRes = rawBody.Sequence();
        if (bodyRes.TryGetError(out var err)) return SyntaxResult<ForInStmt>.Fail(err);
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

    private SyntaxResult<WhileStmt> ParseWhile()
    {
        TraceEnter("ParseWhile");
        var conditionRes = ParseExpr();
        if (conditionRes.TryGetError(out var err)) return SyntaxResult<WhileStmt>.Fail(err);
        
        Match(TokenType.Keyword, "do");

        var rawBody = ParseBlockBody(alreadyInside: true, "end");
        var bodyRes = rawBody.Sequence();
        if (bodyRes.TryGetError(out var bErr)) return SyntaxResult<WhileStmt>.Fail(bErr);
        var body = bodyRes.Expect();

        Match(TokenType.Keyword, "end");
        TraceExit();
        return SyntaxResult<WhileStmt>.Ok(new WhileStmt
        {
            Condition = conditionRes.Expect(),
            Body = body
        });
    }

    private SyntaxResult<ReturnStmt> ParseReturn()
    {
        TraceEnter("ParseReturn");
        var exprRes = ParseExpr();
        if (exprRes.TryGetError(out var err)) return SyntaxResult<ReturnStmt>.Fail(err);
        
        TraceExit();
        return SyntaxResult<ReturnStmt>.Ok(new ReturnStmt { Expr = exprRes.Expect() });
    }

    private SyntaxResult<Ast> ParseExpr() => ParseBinary(0);

    private static readonly Dictionary<string, int> precedences = new()
    {
        ["+"] = 1,
        ["-"] = 1,
        ["*"] = 2,
        ["/"] = 2,
        ["#"] = 3 // Unary length operator (len)
    };

    private SyntaxResult<Ast> ParseBinary(int parentPrecedence)
    {
        SyntaxResult<Ast> rawLeft = ParseUnary();
        if (!rawLeft.TryUnwrap(out var left, out var fail)) return fail;

        while (true)
        {
            if (Curr.Type != TokenType.Symbol)
                break;
            string op = Curr.Lexeme;
            if (!precedences.TryGetValue(op, out int prec) || prec < parentPrecedence)
                break;

            Match(TokenType.Symbol, op);
            var rightRes = ParseBinary(prec + 1);
            if (!rightRes.TryUnwrap(out var right, out var rfail)) return rfail;
            string funcName = op switch
            {
                "+" => "sum",
                "-" => "sub",
                "*" => "mul",
                "/" => "div",
                _ => null,
            };
            if (funcName == null)
                return SyntaxResult<Ast>.Fail(
                           ErrorCode.InternalUnsupportedExpressionType,
                           new object[] { op },
                           _trace
                       );

            left = new CallExpr { Callee = new Ident { Name = funcName }, Args = new List<Ast> { left, right } };
        }
        return SyntaxResult<Ast>.Ok(left);
    }

    private SyntaxResult<Ast> ParseUnary()
    {
        if (Match(TokenType.Symbol, "#"))
        {
            var operandRes = ParseUnary();
            if (!operandRes.TryUnwrap(out var operand, out var fail)) return fail;
            return SyntaxResult<Ast>.Ok(
                new CallExpr { Callee = new Ident { Name = "len" }, Args = new List<Ast> { operand } });
        }
        return ParsePrimary();
    }

    private SyntaxResult<Ast> ParsePrimary()
    {
        Ast expr;
        
        if (Match(TokenType.Keyword, "nil")) expr = new LiteralNil();
        else if (Match(TokenType.Keyword, "true")) expr = new LiteralBool { Value = true };
        else if (Match(TokenType.Keyword, "false")) expr = new LiteralBool { Value = false };
        
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
                if (!p.TryUnwrap(out var value, out var fail)) return fail;

                tbl.Pairs[k] = value;
                Match(TokenType.Symbol, ",");
            }
            expr = tbl;
        }
        else if (Match(TokenType.Symbol, "("))
        {
            var rawExpr = ParseExpr();
            if (!rawExpr.TryUnwrap(out var e, out var fail)) return fail;
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
            var args = new List<Ast>();
            if (!Match(TokenType.Symbol, ")"))
            {
                do{
                    var eRaw = ParseExpr();
                    if (!eRaw.TryUnwrap(out var e, out var rfail)) return rfail;

                    args.Add(e);
                } while (Match(TokenType.Symbol, ","));
                Match(TokenType.Symbol, ")");
            }
            expr = new CallExpr { Callee = expr, Args = args };
        }

        return SyntaxResult<Ast>.Ok(expr);
    }

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
                if (depth > 0){
                    depth--; cur++; continue;
                }
                else
                    break;
            }

            body.Add(ParseStmt());
        }

        return body;
    }
}
