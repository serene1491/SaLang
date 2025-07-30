using System.Collections.Generic;
using SaLang.Syntax.Nodes;
namespace SaLang.Analyzers.Syntax;

public static class SyntaxResultExtensions
{
    public static SyntaxResult<To> Upcast<From, To>(this SyntaxResult<From> self)
        where From : To
        where To : Ast
    {
        if (self.TryGetError(out var err))
            return SyntaxResult<To>.Fail(err.Code, err.Args, err.ErrorStack);
        self.TryGetValue(out var v);
        return SyntaxResult<To>.Ok(v);
    }

    public static SyntaxResult<List<T>> Sequence<T>(this IEnumerable<SyntaxResult<T>> results)
        where T : Ast
    {
        var list = new List<T>();
        foreach (var r in results)
        {
            if (r.TryGetError(out var err))
                return SyntaxResult<List<T>>
                    .Fail(err.Code, err.Args, err.ErrorStack);
            list.Add(r.Expect());
        }
        return SyntaxResult<List<T>>.Ok(list);
    }
}
