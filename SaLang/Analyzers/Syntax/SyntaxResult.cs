using System;
using System.Collections.Generic;
using SaLang.Common;
using SaLang.Syntax.Nodes;
namespace SaLang.Analyzers.Syntax;

/// <summary>
/// Represents the result of parsing an expression or statement.
/// </summary>
public readonly struct SyntaxResult<T>
{
    public bool IsError { get; }
    private readonly T _value;
    private readonly Error _error;

    private SyntaxResult(T value)
    {
        IsError = false;
        _value = value;
        _error = default!;
    }

    private SyntaxResult(Error error)
    {
        IsError = true;
        _error = error;
        _value = default!;
    }

    /// <summary>
    /// Creates a success result containing the AST.
    /// </summary>
    public static SyntaxResult<T> Ok(T value) => new(value);

    /// <summary>
    /// Creates a failure result containing the error.
    /// </summary>
    public static SyntaxResult<T> Fail(ErrorCode code, object[] args, List<TraceFrame> stack)
        => new(new Error(code, args, stack));
    public static SyntaxResult<T> Fail(Error err)
        => new(new Error(err.Code, err.Args, err.ErrorStack));

    /// <summary>
    /// Tries to get the value of T. Returns false if IsError==true.
    /// </summary>
    public bool TryGetValue(out T value)
    {
        if (!IsError)
        {
            value = _value;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// Tries to get the Error. Returns false if IsError==false.
    /// </summary>
    public bool TryGetError(out Error error)
    {
        if (IsError)
        {
            error = _error;
            return true;
        }
        error = default!;
        return false;
    }

    public T Expect()
    {
        if (IsError)
            throw new InvalidOperationException(
                $"Tried to unwrap SyntaxResult<{typeof(T).Name}> but it contains an error: {_error}");
        return _value;
    }
}