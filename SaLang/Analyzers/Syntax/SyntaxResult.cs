using System.Collections.Generic;
using SaLang.Common;
using SaLang.Syntax.Nodes;
namespace SaLang.Analyzers.Syntax;

/// <summary>
/// Represents the result of parsing an expression or statement.
/// </summary>
public readonly struct SyntaxResult
{
    public bool IsError { get; }
    public Ast Node { get; }
    public Error? Error { get; }

    private SyntaxResult(bool isError, Ast node = null, Error? error = null)
    {
        IsError = isError;
        Node = node;
        Error = error;
    }

    public static SyntaxResult Normal(Ast ast) => new(false, node: ast);
    public static SyntaxResult Fail(ErrorCode code, object[] args, List<TraceFrame> stack)
        => new(true, error: new Error(code, args, stack));
}
