using Xunit;
using SaLang.Parsing;
using SaLang.Lexing;
using SaLang.Syntax.Nodes;
using System;
namespace SaLang.Tests.Parsing;

public class ParserExpressionTests
{
    private ProgramNode Parse(string src)
        => new Parser().Parse(new Lexer(src).Tokenize()).Expect();

    [Theory]
    [InlineData("1+2", "+")]
    [InlineData("3*4", "*")]
    public void BinaryOperators_ShouldProduceCallExpr(string expr, string op)
    {
        var prog = Parse(expr);
        var call = Assert.IsType<CallExpr>(prog.Stmts[0] is ExpressionStmt es ? es.Expr : null);
        Assert.Equal(op == "+" ? "__sum" : "__mul", ((Ident)call.Callee).Name);
    }

    [Theory]
    [InlineData("nil", typeof(LiteralNil))]
    [InlineData("true", typeof(LiteralBool))]
    [InlineData("false", typeof(LiteralBool))]
    public void LiteralKeywords_ShouldBeRecognized(string src, Type expectedType)
    {
        var prog = Parse(src);
        var lit = prog.Stmts[0] is ExpressionStmt es ? es.Expr : null;
        Assert.IsType(expectedType, lit);
    }
}
