using System;
using Xunit;
using SaLang.Parsing;
using SaLang.Lexing;
using SaLang.Runtime;
namespace SaLang.Tests.Runtime;

public class InterpreterUnsafeTests
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
    public void UnsafeFunction_SuccessfulReturn_PopulatesOk()
    {
        var code = @"
            var c = {}
            unsafe function c.a()
                return 42
            end
            var res = c.a()
            return res.ok
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(ValueKind.Number, result.Kind);
        Assert.Equal(42, result.Number.Value);
    }

    [Fact]
    public void UnsafeFunction_ErrorReturn_PopulatesFailMessage()
    {
        var msg = Execute(@"
            var c = {}
            unsafe function c.a()
                return std.error('oops happened')
            end
            var res = c.a()
            return res.fail.message
        ");
        Assert.False(msg.IsError);
        Assert.Equal(ValueKind.String, msg.Kind);
        Assert.Equal("Unhandled exception: oops happened.", msg.String);
    }

    [Fact]
    public void UnsafeFunction_ReturnNilWhenNoError_PopulatesNilFail()
    {
        var code = @"
            var c = {}
            unsafe function c.a()
                return 123
            end
            var res = c.a()
            return res.fail
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(ValueKind.Nil, result.Kind);
    }

    [Fact]
    public void UnsafeFunction_NoExplicitReturn_PopulatesFailNil()
    {
        var fail = Execute(@"
            var c = {}
            unsafe function c.a()
                var x = 5
            end
            var res = c.a()
            return res.fail
        ");
        Assert.False(fail.IsError);
        Assert.Equal(ValueKind.Nil, fail.Kind);
    }

    [Fact]
    public void UnsafeFunction_PropagatesThisCorrectly()
    {
        var code = @"
            var c = { v = 10 }
            unsafe function c.get()
                return this.v * 2
            end
            var res = c.get()
            return res.ok
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(20, result.Number);
    }

    [Fact]
    public void UnsafeFunction_CanChainPropertyAccessOnFail()
    {
        var code = @"
            var c = {}
            unsafe function c.a()
                return std.error('fail msg')
            end
            var res = c.a()
            return res.fail.message
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal("Unhandled exception: fail msg.", result.String);
    }

    [Fact]
    public void UnsafeFunction_CanUseInIfConditions()
    {
        var code = @"
            var c = {}
            unsafe function c.mayFail(x)
                if x > 0 then return x end
                return std.error('bad')
            end

            var r1 = c.mayFail(5)
            if r1.ok then var out1 = r1.ok * 2 else so var out1 = 0 end

            var r2 = c.mayFail(-1)
            if r2.fail then var out2 = 1 else so var out2 = 0 end

            return out1 + out2
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(11, result.Number);
    }
}
