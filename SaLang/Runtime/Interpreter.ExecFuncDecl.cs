using System.Collections.Generic;
using SaLang.Analyzers.Runtime;
using SaLang.Common;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private RuntimeResult ExecFuncDecl(FuncDecl fd)
    {
        var func = new FuncValue(args =>
        {
            _callStack.Push(new TraceFrame(
                fd.Name,
                fd.Span.File,
                fd.Span.Line,
                fd.Span.Column
            ));

            var outer = _env;
            try
            {
                var local = new Environment(outer);

                var thisTblRes = ResolveIdentifier(fd.Table);
                if (thisTblRes.IsError)
                    return thisTblRes.Value;
                local.Define("this", Value.FromTable(thisTblRes.Value.Table));

                for (int i = 0; i < fd.Params.Count; i++)
                    local.Define(fd.Params[i], i < args.Count ? args[i] : Value.Nil());

                _env = local;

                RuntimeResult execRes = RuntimeResult.Nothing();
                foreach (var stmt in fd.Body)
                {
                    execRes = ExecStmt(stmt);
                    if (execRes.IsError || execRes.IsReturn)
                        break;
                }

                if (execRes.IsReturn || execRes.IsError)
                    return execRes.Value;

                return Value.Nil();
            }
            finally
            {
                _env = outer;
                _callStack.Pop();
            }
        });

        var tblRes = ResolveIdentifier(fd.Table);
        if (tblRes.IsError) return tblRes;

        var tbl = tblRes.Value.Table ?? new Dictionary<string, Value>();
        _env.Define(fd.Table, Value.FromTable(tbl));
        tbl[fd.Name] = Value.FromFunc(func);

        return RuntimeResult.Nothing();
    }
}
