using System.Collections.Generic;
using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private RuntimeResult EvalCall(CallExpr ce)
    {
        // Reconstruct something like "obj.method" recursively
        string name = ExprToString(ce.Callee);

        var fnVal = EvalExpr(ce.Callee);
        if (fnVal.IsError) return fnVal;
        var fn = fnVal.Value.Func;
        if (fn == null)
            return RuntimeResult.Error(Value.FromError(new Error(
                ErrorCode.RuntimeInvalidFunctionCall,
                errorStack: [.. _callStack],
                args: [name]
            )));

        var args = new List<Value>();
        foreach (var a in ce.Args)
        {
            var av = EvalExpr(a);
            if (av.IsError) return av;
            args.Add(av.Value);
        }

        var result = fn(args);
        if (result.IsError)
            return RuntimeResult.Error(result);

        return RuntimeResult.Normal(result);
    }
}
