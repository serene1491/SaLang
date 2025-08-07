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
                if x then return x end
                return std.error('bad')
            end

            var r1 = c.mayFail(5)
            var r2 = c.mayFail(0)
            var out1 = nil
            var out2 = nil

            if r1.ok then
                out1 = 100
            not so
                out1 = 200
            end

            if r2.fail then
                out2 = 300
            not so
                out2 = 400
            end

            return out1 + out2
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(400, result.Number);
    }

    [Fact]
    public void UnsafeFunction_FailSkipsIf()
    {
        var code = @"
            var c = {}
            unsafe function c.mayFail(x)
                if x then return x end
                return std.error('fail')
            end

            var r = c.mayFail(0)
            var out = 10

            if r.ok then
                out = 50
            not so
                out = 20
            end

            return out
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(20, result.Number);
    }

    [Fact]
    public void UnsafeFunction_OkSkipsFail()
    {
        var code = @"
            var c = {}
            unsafe function c.mayFail(x)
                if x then return x end
                return std.error('fail')
            end

            var r = c.mayFail(3)
            var out = 100

            if r.fail then
                out = 5
            not so
                out = 33
            end

            return out
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(33, result.Number);
    }

    [Fact]
    public void UnsafeFunction_RawValueHasOkFail()
    {
        var code = @"
            var val = 5
            var out = 0

            if val.ok then
                out = 1
            not so
                out = 2
            end

            return out
        ";
        var result = Execute(code);
        Assert.True(result.IsError);
        Assert.Contains("[E-R4008]", result.String);
    }

    [Fact]
    public void UnsafeFunction_OkCanBeString()
    {
        var code = @"
            var c = {}
            unsafe function c.mayFail(x)
                if x == 1 then return 'ok-value' end
                return std.error('not one')
            end

            var r = c.mayFail(1)
            var out = 'nope'

            if r.ok then
                out = r.ok
            not so
                out = 'failed'
            end

            return out
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal("ok-value", result.String);
    }
}
