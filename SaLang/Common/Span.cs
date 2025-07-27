namespace SaLang.Common;

/// <summary>
/// Represents a source location for error reporting and diagnostics.
/// </summary>
public record Span(string File, int Line, int Column);