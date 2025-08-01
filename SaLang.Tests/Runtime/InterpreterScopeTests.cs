using System;
using Xunit;
using SaLang.Parsing;
using SaLang.Lexing;
using SaLang.Runtime;
namespace SaLang.Tests.Runtime;

public class InterpreterScopeTests
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
    public void VarInIf_DoesNotLeakToOuterScope()
    {
        var code = @"
            if true then
                var x = 42
            end
            return x
        ";
        var result = Execute(code);
        Assert.True(result.IsError);
        Assert.Contains("Undefined variable", result.ToString());
    }

    [Fact]
    public void VarInElse_DoesNotLeakToOuterScope()
    {
        var code = @"
            if false then
                var a = 1
            not so
                return a
            end
        ";
        var result = Execute(code);
        Assert.True(result.IsError);
    }

    [Fact]
    public void FunctionParameters_AreLocalToFunction()
    {
        var code = @"
            var x = 5
            var m = {}
            function m.f(x)
                return x * 2
            end
            var y = m.f(10)
            return x
        ";
        var result = Execute(code);
        Assert.Equal(5, result.Number);
    }

    [Fact]
    public void FunctionLocals_DoNotLeakOut()
    {
        var code = @"
            function g()
                var tmp = 99
                return tmp
            end
            var z = g()
            return tmp
        ";
        var result = Execute(code);
        Assert.True(result.IsError);
    }

    [Fact]
    public void ThisBinding_IsolatedPerCall()
    {
        var code = @"
            var obj1 = { v = 1 }
            var obj2 = { v = 2 }

            function obj1.get() return this.v end
            function obj2.get() return this.v end

            var r1 = obj1.get()
            var r2 = obj2.get()
            return r1 * 10 + r2
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(1 * 10 + 2, result.Number.Value);
    }

    [Theory]
    [InlineData("var a = 1 if false then var b = 2 end return b")]
    [InlineData("var a = 1 if true then var b = 2 end return b")]
    public void IfVarLeak_Theory(string code)
    {
        var result = Execute(code);
        Assert.True(result.IsError);

    }
}
