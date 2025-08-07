using System.Collections.Generic;
using SaLang.Common;
using SaLang.Syntax.Nodes;
using SaLang.Analyzers.Syntax;
namespace SaLang.Parsing;

public partial class Parser
{
    #region Tracing
    private List<TraceFrame> _trace = new();
    private void TraceEnter(string prod, Token? at = null) => _trace.Add(Trace(prod, at));
    private void TraceExit() => _trace.RemoveAt(_trace.Count - 1);
    private TraceFrame Trace(string productionName, Token? at = null)
    {
        var t = at ?? Curr;
        return new TraceFrame(productionName, _sourceFile, t.Line, t.Column);
    }
    #endregion

    private readonly string _sourceFile;
    private List<Token> _tokens = new();
    private int cur = 0;

    public Parser(string sourceFile = "<memory>")
        => _sourceFile = sourceFile;

    private Token Curr => cur < _tokens.Count ? _tokens[cur] : _tokens[^1];

    private bool Check(TokenType t, string lex = null)
        => Curr.Type == t && (lex == null || Curr.Lexeme == lex);

    private bool Match(TokenType t, string lex = null)
    {
        if (Curr.Type == t && (lex == null || Curr.Lexeme == lex))
        {
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
}
