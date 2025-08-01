namespace SaLang.Analyzers;

public static class ErrorCodeExtensions
{
    public static string GetCode(this ErrorCode code)
    {
        return code switch
        {
            #region Syntax Errors (E-S1xxx)
            ErrorCode.SyntaxUnexpectedToken      => "E-S1001",
            ErrorCode.SyntaxUnexpectedEOF        => "E-S1002",
            ErrorCode.SyntaxInvalidLiteral       => "E-S1003",
            ErrorCode.SyntaxUnterminatedString   => "E-S1004",
            ErrorCode.SyntaxInvalidIdentifier    => "E-S1005",
            #endregion

            #region Semantic Errors (E-T2xxx / E-N3xxx)
            ErrorCode.SemanticUndefinedVariable  => "E-T2001",
            ErrorCode.SemanticTypeMismatch       => "E-T2002",
            ErrorCode.SemanticInvalidArguments   => "E-T2003",
            ErrorCode.SemanticReadonlyAssignment => "E-T2004",
            ErrorCode.SemanticArgumentsMismatch  => "E-T2005",
            ErrorCode.SemanticDuplicateSymbol    => "E-N3001",
            ErrorCode.SemanticInvalidBreak       => "E-N3002",
            ErrorCode.SemanticInvalidContinue    => "E-N3003",
            #endregion

            #region Runtime Errors (E-R4xxx)
            ErrorCode.RuntimeDivisionByZero      => "E-R4001",
            ErrorCode.RuntimeNullReference       => "E-R4002",
            ErrorCode.RuntimeOutOfBounds         => "E-R4003",
            ErrorCode.RuntimeInvalidFunctionCall => "E-R4004",
            ErrorCode.RuntimeStackOverflow       => "E-R4005",
            ErrorCode.RuntimeTypeCastFailure     => "E-R4006",
            ErrorCode.RuntimeThrownException     => "E-R4007",
            ErrorCode.RuntimeKeyNotFound         => "E-R4008",
            #endregion

            #region I/O & Environment (E-I5xxx)
            ErrorCode.IOFileNotFound             => "E-I5001",
            ErrorCode.IOPermissionDenied         => "E-I5002",
            ErrorCode.IOReadError                => "E-I5003",
            #endregion

            #region Internal / Tooling (E-X9xxx)
            ErrorCode.InternalAssertionFailed           => "E-X9001",
            ErrorCode.InternalThrowDuringExecution      => "E-X9002",
            ErrorCode.InternalUnsupportedExpressionType => "E-X9003",
            #endregion

            _ => $"E-UNKNOWN {code}"
        };
    }

    public static string GetMessage(this ErrorCode code, params object[] args)
    {
        string template = code switch
        {
            // Syntax Errors (E-S1xxx)
            ErrorCode.SyntaxUnexpectedToken      => "Unexpected token '{0}'.",
            ErrorCode.SyntaxUnexpectedEOF        => "Unexpected end of file.",
            ErrorCode.SyntaxInvalidLiteral       => "Invalid literal '{0}'.",
            ErrorCode.SyntaxUnterminatedString   => "Unterminated string literal.",
            ErrorCode.SyntaxInvalidIdentifier    => "Invalid identifier '{0}'.",

            // Semantic Errors (E-T2xxx / E-N3xxx)
            ErrorCode.SemanticUndefinedVariable  => "Undefined variable '{0}'.",
            ErrorCode.SemanticTypeMismatch       => "Cannot assign '{0}' to '{1}'.",
            ErrorCode.SemanticInvalidArguments   => "Invalid arguments for '{0}': expected {1}, got {2}.",
            ErrorCode.SemanticReadonlyAssignment => "Cannot assign to readonly variable '{0}'.",
            ErrorCode.SemanticArgumentsMismatch  => "Mismatch arguments for '{0}': expected {1}, got {2}.",
            ErrorCode.SemanticDuplicateSymbol    => "Duplicate declaration of '{0}'.",
            ErrorCode.SemanticInvalidBreak       => "Invalid 'break' outside of loop.",
            ErrorCode.SemanticInvalidContinue    => "Invalid 'continue' outside of loop.",

            // Runtime Errors (E-R4xxx)
            ErrorCode.RuntimeDivisionByZero      => "Cannot divide by 0.",
            ErrorCode.RuntimeNullReference       => "Null reference.",
            ErrorCode.RuntimeOutOfBounds         => "Index out of bounds: {0}.",
            ErrorCode.RuntimeInvalidFunctionCall => "Invalid function call: {0}.",
            ErrorCode.RuntimeStackOverflow       => "Stack overflow.",
            ErrorCode.RuntimeTypeCastFailure     => "Cannot cast from '{0}' to '{1}'.",
            ErrorCode.RuntimeThrownException     => "Unhandled exception: {0}.",
            ErrorCode.RuntimeKeyNotFound         => "Key '{0}' not found in table '{1}'.",

            // I/O & Environment (E-I5xxx)
            ErrorCode.IOFileNotFound             => "File not found: '{0}'.",
            ErrorCode.IOPermissionDenied         => "Permission denied: '{0}'.",
            ErrorCode.IOReadError                => "Error reading file: '{0}'.",

            // Internal / Tooling (E-X9xxx)
            ErrorCode.InternalAssertionFailed    => "Internal assertion failed: {0}.",
            ErrorCode.InternalThrowDuringExecution
                                                => "Interpreter threw an internal error: {0}.",
            ErrorCode.InternalUnsupportedExpressionType
                                                => "Unsupported expression type: {0}.",
                
            _ => $"E-UNKNOWN {code}"
        };

        return args.Length > 0
            ? string.Format(template, args)
            : template;
    }
}
