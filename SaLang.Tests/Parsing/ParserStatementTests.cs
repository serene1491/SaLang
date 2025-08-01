using Xunit;
using SaLang.Parsing;
using SaLang.Lexing;
using SaLang.Syntax.Nodes;
namespace SaLang.Tests.Parsing;

public class ParserStatementTests
{
    private static ProgramNode MakeParser(string src)
    {
        var tokens = new Lexer(src).Tokenize();
        return new Parser().Parse(tokens).Expect();
    }

    [Fact]
    public void ParseVarDeclaration_ShouldProduceVarDeclNode()
    {
        var prog = MakeParser("var x = 42");
        Assert.IsType<VarDeclaration>(prog.Stmts[0]);
        var vd = (VarDeclaration)prog.Stmts[0];
        Assert.Equal("x", vd.Name);
        Assert.IsType<LiteralNumber>(vd.Expr);
    }

    [Fact]
    public void ParseReturn_ShouldProduceNumberReturn()
    {
        var prog = MakeParser("return 0");
        var rs = Assert.IsType<ReturnStmt>(prog.Stmts[0]);
        Assert.IsType<LiteralNumber>(rs.Expr);
    }
}
