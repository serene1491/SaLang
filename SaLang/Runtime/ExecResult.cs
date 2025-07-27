namespace SaLang.Runtime;

public class ExecResult
{
    public bool IsReturn { get; }
    public bool IsError  { get; }
    public Value Value   { get; }

    private ExecResult(Value v, bool isReturn, bool isError)
    {
        Value = v;
        IsReturn = isReturn;
        IsError = isError;
    }

    public static ExecResult Normal(Value v) => new ExecResult(v, false, false);
    public static ExecResult Return(Value v) => new ExecResult(v, true,  false);
    public static ExecResult Error(Value v)  => new ExecResult(v, false, true);
}
