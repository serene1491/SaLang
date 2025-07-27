namespace SaLang.Runtime;

public record TraceFrame(string FunctionName, string File, int Line, int Column);
