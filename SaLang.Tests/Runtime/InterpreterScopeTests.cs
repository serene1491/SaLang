using System;
using Xunit;
using SaLang.Parsing;
using SaLang.Lexing;
using SaLang.Runtime;
using Environment = SaLang.Runtime.Environment;
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
    public void AssignInIf_AffectsOut()
    {
        var code = @"
            var a = nil
            if true then
                a = 1
            end
            return a
        ";
        var result = Execute(code);
        Assert.Equal(1, result.Number);
    }

    [Fact]
    public void ErrorInsideIf_WithoutUnsafe_StopsExecution()
    {
        var code = @"
            var a = 0
            if true then
                a = 1
                std.error('fail')
            end
            return a
        ";
        var result = Execute(code);
        Assert.True(result.IsError);
        Assert.Equal("Unhandled exception: fail.", result.Error.Value.Message);
    }

    [Fact]
    public void AssignmentBeforeErrorInIf_Persists()
    {
        var code = @"
            var a = 0
            if true then
                a = 42
                std.error('fail')
            end
            return a
        ";
        var result = Execute(code);
        Assert.True(result.IsError);
    }

    [Fact]
    public void ErrorInElse_StopsExecution()
    {
        var code = @"
            var a = 5
            if false then
                a = 1
            else
                std.error('fail in else')
            end
            return a
        ";
        var result = Execute(code);
        Assert.True(result.IsError);
        Assert.Equal("Unhandled exception: fail in else.", result.Error.Value.Message);
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
            var t = {}
            function t.g()
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

    [Fact]
    public void Assign_UpdatesParentScope()
    {
        var parent = new Environment();
        parent.Define("out", Value.FromNumber(0)); // define no parent

        var child = new Environment(parent);
        var res = child.Assign("out", Value.FromNumber(5));

        // o Assign deve retornar sucesso
        Assert.False(res.IsError);
        // e o valor no parent deve ter sido atualizado
        var got = parent.Get("out");
        Assert.NotNull(got);
        Assert.Equal(ValueKind.Number, got.Value!.Kind);
        Assert.Equal(5, got?.Number);
    }

    [Fact]
    public void Assign_ToUndefinedVariable_ReturnsError()
    {
        var parent = new Environment();
        var child = new Environment(parent);

        var res = child.Assign("x", Value.FromNumber(1));

        Assert.True(res.IsError);
        Assert.Null(parent.Get("x"));
    }

    [Fact]
    public void Assign_ToReadonlyInParent_ReturnsReadonlyError()
    {
        var parent = new Environment();
        parent.Define("one_hundred", Value.FromNumber(100), isReadonly: true);

        var child = new Environment(parent);
        var res = child.Assign("one_hundred", Value.FromNumber(200));

        Assert.True(res.IsError);
        var got = parent.Get("one_hundred");
        Assert.NotNull(got);
        Assert.Equal(100, got.Value.Number);
    }

    [Fact]
    public void AssignInIf_UpdatesParentVariable()
    {
        var code = @"
            var out = 0
            if true then
                out = 5
            end
            return out
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(5, result.Number);
    }

    [Fact]
    public void VarInIf_ShadowsParentVariable()
    {
        var code = @"
            var out = 1
            if true then
                var out = 2
            end
            return out
        ";
        var result = Execute(code);
        Assert.False(result.IsError);
        Assert.Equal(1, result.Number);
    }
}
