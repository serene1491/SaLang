using SaLang.Runtime;
namespace SaLang.Analyzers.Runtime;

public class RuntimeResult
{
    public bool IsReturn { get; }
    public bool IsError  { get; }
    public Value Value   { get; }

    private RuntimeResult(Value v, bool isReturn, bool isError)
    {
        Value = v;
        IsReturn = isReturn;
        IsError = isError;
    }

    public static RuntimeResult Normal(Value v) => new RuntimeResult(v, false, false);
    public static RuntimeResult Nothing() => Normal(Value.Nil());
    public static RuntimeResult Return(Value v) => new RuntimeResult(v, true, false);
    public static RuntimeResult Error(Value v)  => new RuntimeResult(v, false, true);
}
