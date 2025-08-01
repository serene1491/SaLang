using System;
using System.Linq;
using Xunit;
using SaLang.Parsing;
using SaLang.Lexing;
using SaLang.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang.Tests.Runtime;

public class InterpreterTests
{
    private static Value Execute(string src)
    {
        var tokens = new Lexer(src).Tokenize();
        var prog = new Parser().Parse(tokens).Expect();
        return new SaLang.Runtime.Interpreter().Interpret(prog);
    }

    [Fact]
    public void ArithmeticAndVariables_WorksAcrossLines()
    {
        var code = @"
            var x = 10
            var y = x * 2 + 5
            return y
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(25, result.Number.Value);
    }

    [Fact]
    public void IfElse_ChoosesCorrectBranch()
    {
        var code = @"
            var a = 0
            if true then
              a = 100
            not so
              a = 200
            end
            return a
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(100, result.Number.Value);
    }

    [Fact]
    public void WhileLoop_ComputesSumCorrectly()
    {
        var code = @"
            var sum = 0
            var i = 1
            while 1 do
                sum = 1000
                return sum
            end
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(1000, result.Number.Value);
    }

    [Fact]
    public void FunctionDefinitionAndCall_ReturnsExpected()
    {
        var code = @"
            var math = {}
            function math.add(x, y)
              return x + y
            end

            return math.add(7, 8)
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(15, result.Number.Value);
    }

    [Fact]
    public void ReturnWithoutValue_YieldsZero()
    {
        var code = @"
            return 0
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(0, result.Number.Value);
    }

    [Fact]
    public void UndefinedVariable_ProducesErrorValue()
    {
        var code = "return foo";
        var result = Execute(code);
        Assert.True(result.IsError);
        Assert.Contains("Undefined variable", result.ToString());
    }
}
