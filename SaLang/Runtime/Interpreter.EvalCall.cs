using System.Collections.Generic;
using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
using SaLang.Common;
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

        var callFrame = new TraceFrame(
            name,
            ce.Span.File,
            ce.Span.Line,
            ce.Span.Column
        );
        _callStack.Push(callFrame);

        Value rawResult;
        try
        {
            rawResult = fn(args);
        }
        finally
        {
            _callStack.Pop();
        }

        if (rawResult.IsError) return RuntimeResult.Error(rawResult);

        return RuntimeResult.Normal(rawResult);
    }
}
