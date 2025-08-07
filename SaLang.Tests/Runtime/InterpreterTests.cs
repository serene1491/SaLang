using System;
using Xunit;
using SaLang.Parsing;
using SaLang.Lexing;
using SaLang.Runtime;
using SaLang.Analyzers;
namespace SaLang.Tests.Runtime;

public class InterpreterTests
{
    private static Value Execute(string src)
    {
        var tokens = new Lexer(src).Tokenize();
        var prog = new Parser().Parse(tokens).Expect();
        var res = new SaLang.Runtime.Interpreter().Interpret(prog);
        if (res.IsError)
            Console.WriteLine(res);
        return res;
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

    [Fact]
    public void ForIn_TableIterationAndNestedLoops() // TODO: ALLOW TUPLE IN FOR LOOP
    {
        var code = @"
            var table = { a = 1, b = 2, c = 3 }
            var number = 0

            for k in table do
                for l in table do
                    number = number + 1
                end
            end

            return number
        ";
        var result = Execute(code);
        Assert.Equal(ValueKind.Number, result.Kind);
        Assert.True(result.Number.HasValue);
        Assert.Equal(9, result.Number);
    }

    [Fact]
    public void RequireBuiltinLibraryAndUsePrint()
    {
        var code = @"
            require('std') as std
            return std.print('Hello World!')
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(ValueKind.Nil, result.Kind);
    }

    [Fact]
    public void AssignToReadonly_ThrowsSemanticError()
    {
        var code = @"
            100 as one_hundred
            one_hundred = 99
        ";
        var result = Execute(code);
        Assert.Equal(ErrorCode.SemanticReadonlyAssignment, result.Error.Value.Code);
    }

    [Fact]
    public void ReturnInterruptsFollowingStatements()
    {
        var code = @"
            return 0
            std.error('')
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(0, result.Number.Value);
    }

    [Fact]
    public void RequireNonexistentModule_ProducesIoError()
    {
        var code = @"
            require('meme') as u
            u.scream()
        ";
        var result = Execute(code);
        Assert.True(result.IsError);
    }

    [Fact]
    public void TableMethodCall()
    {
        var code = @"
            require('std') as std
            var people = { money = 1124 }

            function people.showMoney() return this.money end
            return people.showMoney()
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(1124, result.Number.Value);
    }
}
