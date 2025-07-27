namespace SaLang.Analyzers;

public enum ErrorCode
{
    #region Syntax Errors (E-S1xxx)

    /// <summary>
    /// E‑S1001: Unexpected token '{0}'.
    /// </summary>
    SyntaxUnexpectedToken,

    /// <summary>
    /// E‑S1002: Unexpected end of file.
    /// </summary>
    SyntaxUnexpectedEOF,

    /// <summary>
    /// E‑S1003: Invalid literal '{0}'.
    /// </summary>
    SyntaxInvalidLiteral,

    /// <summary>
    /// E‑S1004: Unclosed string literal.
    /// </summary>
    SyntaxUnterminatedString,

    /// <summary>
    /// E‑S1005: Invalid identifier '{0}'.
    /// </summary>
    SyntaxInvalidIdentifier,

    #endregion

    #region Semantic Errors

    /// <summary>
    /// E‑T2001: Undefined variable '{0}'.
    /// </summary>
    SemanticUndefinedVariable,

    /// <summary>
    /// E‑T2002: Cannot assign '{0}' to '{1}'.
    /// </summary>
    SemanticTypeMismatch,

    /// <summary>
    /// E‑T2003: Invalid arguments for '{0}': expected {1}, got {2}.
    /// </summary>
    SemanticInvalidArguments,

    /// <summary>
    /// E‑T2004: Cannot assign to readonly variable '{0}'.
    /// </summary>
    SemanticReadonlyAssignment,

    /// <summary>
    /// E‑N3001: Duplicate declaration of '{0}'.
    /// </summary>
    SemanticDuplicateSymbol,

    /// <summary>
    /// E‑N3002: Invalid 'break' outside of loop.
    /// </summary>
    SemanticInvalidBreak,

    /// <summary>
    /// E‑N3003: Invalid 'continue' outside of loop.
    /// </summary>
    SemanticInvalidContinue,

    #endregion

    #region Runtime Errors (E-R4xxx)

    /// <summary>
    /// E‑R4001: Cannot divide by 0.
    /// </summary>
    RuntimeDivisionByZero,

    /// <summary>
    /// E‑R4002: Null reference.
    /// </summary>
    RuntimeNullReference,

    /// <summary>
    /// E‑R4003: Index out of bounds: {0}.
    /// </summary>
    RuntimeOutOfBounds,

    /// <summary>
    /// E‑R4004: Invalid function call: {0}.
    /// </summary>
    RuntimeInvalidFunctionCall,

    /// <summary>
    /// E‑R4005: Stack overflow.
    /// </summary>
    RuntimeStackOverflow,

    /// <summary>
    /// E‑R4006: Cannot cast from '{0}' to '{1}'.
    /// </summary>
    RuntimeTypeCastFailure,

    /// <summary>
    /// E‑R4007: Unhandled exception: {0}.
    /// </summary>
    RuntimeThrownException,

    /// <summary>
    /// E-R4008: Key '{0}' not found in table '{1}'.
    /// </summary>
    RuntimeKeyNotFound, //

    #endregion

    #region I/O & Environment (E-I5xxx)

    /// <summary>
    /// E‑I5001: File not found: '{0}'.
    /// </summary>
    IOFileNotFound,

    /// <summary>
    /// E‑I5002: Permission denied: '{0}'.
    /// </summary>
    IOPermissionDenied,

    /// <summary>
    /// E‑I5003: Error reading file: '{0}'.
    /// </summary>
    IOReadError,

    #endregion

    #region Internal / Tooling (E-X9xxx)

    /// <summary>
    /// E‑X9001: Internal assertion failed: {0}.
    /// </summary>
    InternalAssertionFailed,

    /// <summary>
    /// E‑X9002: Interpreter threw an internal error: {0}.
    /// </summary>
    InternalThrowDuringExecution,

    /// <summary>
    /// E-X9003: Unsupported expression type: {0}.
    /// </summary>
    InternalUnsupportedExpressionType, //

    #endregion
}
