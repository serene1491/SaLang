namespace SaLang.Analyzers.Semantic;

public readonly struct SemanticResult
{
    public bool IsError  { get; }
    public ErrorCode? Error   { get; }

    private SemanticResult(bool isError, ErrorCode? error = null)
    {
        IsError = isError;
        Error = error;
    }

    public static SemanticResult Nothing() => new SemanticResult(false);
    public static SemanticResult Fail(ErrorCode error)  => new SemanticResult(true, error);
}
