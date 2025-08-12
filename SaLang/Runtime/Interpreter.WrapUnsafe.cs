using System.Collections.Generic;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private static Value WrapUnsafe(Value raw, bool isUnsafe)
    {
        if (!isUnsafe)
            return raw; // Safe functions don't do this

        bool hadError = raw.IsError;
        var ok = hadError ? Value.Nil() : raw;

        var fail = hadError
            ? Value.FromTable(new Dictionary<string, Value>
            {
                ["code"] = Value.FromNumber((int)raw.Error.Value.Code),
                ["message"] = Value.FromString(raw.Error.Value.Message),
                ["stack"] = Value.FromString(raw.Error.Value.GetStack())
            })
            : Value.Nil();

        return Value.FromTable(new Dictionary<string, Value>
        {
            ["ok"  ] = ok,
            ["fail"] = fail
        });
    }
}
