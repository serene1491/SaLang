using System;
using System.Collections.Generic;
using SaLang.Common;
namespace SaLang.Analyzers;

public readonly struct Error
{
    public ErrorCode Code { get; }
    /// <summary>
    /// Placed in the error template message
    /// </summary>
    public object[] Args { get; }
    public List<TraceFrame> ErrorStack { get; }

    public Error(ErrorCode code, object[] args = null, List<TraceFrame> errorStack = null)
    {
        Code       = code;
        Args       = args ?? Array.Empty<object>();
        ErrorStack = errorStack ?? new List<TraceFrame>();
    }

    public string Message => Code.GetMessage(Args);

    public string GetStack()
    {
        var stack = "\nStack trace:\n";

        foreach (var frame in ErrorStack)
            stack += $"    at {frame.FunctionName} in {frame.File}:{frame.Line};{frame.Column}\n";

        return stack + "End atack trace";
    }

    public string Build()
    {
        var codeStr = Code.GetCode();
        var errorMessage = Code.GetMessage(Args);
        string errorFormated = $"[{codeStr}] {errorMessage}";
        if (ErrorStack.Count < 1)
            return errorFormated;
        errorFormated += GetStack();
        return errorFormated;
    }

    public override string ToString() => Build();
}
