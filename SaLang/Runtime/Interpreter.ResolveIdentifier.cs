using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private RuntimeResult ResolveIdentifier(string name)
    {
        var maybe = _env.Get(name);
        if (maybe == null)
        {
            return RuntimeResult.Error(
                Value.FromError(new Error(
                    ErrorCode.SemanticUndefinedVariable,
                    errorStack: [.. _callStack],
                    args: [name]
                ))
            );
        }
        return RuntimeResult.Normal(maybe.Value);
    }
}
