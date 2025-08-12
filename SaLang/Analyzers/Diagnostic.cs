using SaLang.Common;
namespace SaLang.Analyzers;

public class Diagnostic
{
    public string Message { get; set; }
    public Span Span { get; set; }
    public ErrorCode Code { get; set; }
}