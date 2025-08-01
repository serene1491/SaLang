using System.Collections.Generic;
using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
using SaLang.Analyzers.Semantic;
using SaLang.Common;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private readonly Environment _globals = new();
    private Environment _env;
    private readonly Stack<TraceFrame> _callStack = new();
    private static bool IsTruthy(Value v)
        => !(v.IsError || v.Kind == ValueKind.Nil
                     || v.Kind == ValueKind.Bool && v.Bool == false
                     || (v.Number <= 0 && v.Kind == ValueKind.Number)); 

    public Interpreter()
    {
        _env = _globals;
        RegisterDefaultBuiltins();
    }

    public Value Interpret(ProgramNode prog)
    {
        foreach (var stmt in prog.Stmts)
        {
            var res = ExecStmt(stmt);
            if (res.IsError) return res.Value;
            if (res.IsReturn) return res.Value;
        }
        return Value.FromNumber(0);
    }

    private RuntimeResult ExecStmt(Ast node)
    {
        RuntimeResult val;
        switch (node)
        {
            case VarDeclaration vd:
                val = ExecVarDeclaration(vd);
                break;
            case VarAssign va:
                val = ExecVarAssign(va);
                break;
            case IfStmt iff:
                val = ExecIf(iff);
                break;
            case ForInStmt iff:
                val = ExecForIn(iff);
                break;
            case WhileStmt iff:
                val = ExecWhile(iff);
                break;
            case FuncDecl fd:
                val = ExecFunc(fd);
                break;
            case ExpressionStmt es:
                val = EvalExpr(es.Expr);
                break;
            case ReturnStmt rs:
                var rv = EvalExpr(rs.Expr);
                if (rv.IsError)
                    return RuntimeResult.Error(rv.Value);
                return RuntimeResult.Return(rv.Value); ;
            default:
                return RuntimeResult.Error(Value.FromError(new Error(
                    ErrorCode.InternalUnsupportedExpressionType, errorStack: [.. _callStack],
                    args: [$"statement {node.GetType().Name}"]
                )));
        }

        if (val.IsError)
            return val;

        return val;
    }

    private RuntimeResult ExecVarDeclaration(VarDeclaration vd)
    {
        var res = EvalExpr(vd.Expr);
        if (res.IsError) return res;
        _env.Define(vd.Name, res.Value, vd.IsReadonly);
        return RuntimeResult.Nothing();
    }

    private RuntimeResult ExecVarAssign(VarAssign aa)
    {
        var res = EvalExpr(aa.Expr);
        if (res.IsError) return RuntimeResult.Error(res.Value);

        if (aa.Table != null)
        {
            var tableValRes = EvalTableAccess(new TableAccess { Table = aa.Table.Table, Key = aa.Table.Key });
            if (tableValRes.IsError)
            {
                var baseTableRes = ResolveIdentifier(aa.Table.Table);
                if (baseTableRes.IsError)
                    return baseTableRes;

                var baseTable = baseTableRes.Value.Table;
                if (baseTable == null)
                    return RuntimeResult.Error(Value.FromError(new Error(
                        ErrorCode.RuntimeInvalidFunctionCall, errorStack: [.. _callStack],
                        args: [$"Variable '{aa.Table.Table}' is not a table"]
                    )));

                baseTable[aa.Table.Key] = res.Value;
                return RuntimeResult.Nothing();
            }
            else
            {
                var tblRes = ResolveIdentifier(aa.Table.Table);
                if (tblRes.IsError) return tblRes;

                var tbl = tblRes.Value.Table;
                if (tbl == null)
                    return RuntimeResult.Error(Value.FromError(new Error(
                        ErrorCode.RuntimeInvalidFunctionCall, errorStack: [.. _callStack],
                        args: [$"Variable '{aa.Table.Table}' is not a table"]
                    )));

                tbl[aa.Table.Key] = res.Value;
                return RuntimeResult.Nothing();
            }
        }
        else{
            SemanticResult r = _env.Assign(aa.Name, res.Value);
            if (r.IsError)
                return RuntimeResult.Error(Value.FromError(new Error(
                    r.Error.Value, errorStack: [.. _callStack],
                    args: [aa.Name]
                )));
            
            return RuntimeResult.Nothing();
        }
    }

    private RuntimeResult ExecIf(IfStmt iff)
    {
        foreach (var clause in iff.Clauses)
        {
            if (clause.Condition != null)
            {
                var condRes = EvalExpr(clause.Condition);
                if (condRes.IsError)
                    return RuntimeResult.Error(condRes.Value);
                if (!IsTruthy(condRes.Value))
                    continue;
            }

            foreach (var stmt in clause.Body)
            {
                var r = ExecStmt(stmt);
                if (r.IsError || r.IsReturn) return r;
            }
            return RuntimeResult.Nothing();
        }

        return RuntimeResult.Nothing();
    }

    private RuntimeResult ExecWhile(WhileStmt stmt)
    {
        // While cond do body end
        while (true)
        {
            var condRes = EvalExpr(stmt.Condition);
            if (condRes.IsError) return RuntimeResult.Error(condRes.Value);
            var condVal = condRes.Value;
            if (!IsTruthy(condVal)) break;

            foreach (var s in stmt.Body)
            {
                var res = ExecStmt(s);
                if (res.IsError || res.IsReturn)
                    return res;
            }
        }
        return RuntimeResult.Nothing();
    }

    private RuntimeResult ExecForIn(ForInStmt stmt)
    {
        var iterRes = EvalExpr(stmt.Iterable);
        if (iterRes.IsError) return RuntimeResult.Error(iterRes.Value);
        var iterable = iterRes.Value;
        if (iterable.Table == null)
            if (iterable.Table == null) return RuntimeResult.Error(Value.FromError(new Error(
                ErrorCode.SemanticInvalidArguments, errorStack: [.. _callStack],
                args: ["for-statement", "table", iterable.Kind]
            )));

        var originalEnv = _env;

        foreach (var kv in iterable.Table){
        try
        {
            _env = new Environment(originalEnv);
            _env.Define(stmt.VarName, kv.Value);

            foreach (var innerKv in iterable.Table)
            {
                var savedEnvInner = _env;
                try
                {
                    _env = new Environment(savedEnvInner);
                    _env.Define(stmt.VarName, innerKv.Value);
                    foreach (var s in stmt.Body)
                    {
                        var res = ExecStmt(s);
                        if (res.IsError || res.IsReturn) return res;
                    }
                }
                finally{
                    _env = savedEnvInner;
                }
            }
        }
        finally{
            _env = originalEnv;
        }}

        return RuntimeResult.Nothing();
    }

    private RuntimeResult ExecFunc(FuncDecl fd)
    {
        var func = new FuncValue(args =>
        {
            // Push frame
            _callStack.Push(new TraceFrame(
                fd.Name,
                fd.Span.File,
                fd.Span.Line,
                fd.Span.Column
            ));

            var local = new Environment(_env);
            var thisTbl = ResolveIdentifier(fd.Table);
            if (thisTbl.IsError) return thisTbl.Value;
            
            local.Define("this", Value.FromTable(thisTbl.Value.Table));
            for (int i = 0; i < fd.Params.Count; i++)
                local.Define(fd.Params[i], i < args.Count ? args[i] : Value.Nil());

            var prev = _env;
            _env = local;
            RuntimeResult execRes = RuntimeResult.Nothing();
            foreach (var s in fd.Body)
            {
                execRes = ExecStmt(s);
                if (execRes.IsError || execRes.IsReturn)
                    break;
            }
            _env = prev;

            // Pop frame
            _callStack.Pop();

            // Return type
            if (execRes.IsError || execRes.IsReturn)
                return execRes.Value;
            return Value.Nil();
        });

        var tblVal = ResolveIdentifier(fd.Table);
        if (tblVal.IsError) return tblVal;

        var tbl = tblVal.Value.Table ?? new Dictionary<string, Value>();
        _env.Define(fd.Table, Value.FromTable(tbl));
        tbl[fd.Name] = Value.FromFunc(func);

        return RuntimeResult.Nothing();
    }

    private RuntimeResult EvalExpr(Ast expr) => expr switch
    {
        LiteralNumber ln => RuntimeResult.Normal(Value.FromNumber(ln.Value)),
        LiteralNil       => RuntimeResult.Normal(Value.Nil()),
        LiteralBool lb   => RuntimeResult.Normal(Value.FromBool(lb.Value)),
        LiteralString ls => RuntimeResult.Normal(Value.FromString(ls.Value)),
        Ident id         => ResolveIdentifier(id.Name),
        TableLiteral tl  => EvalTable(tl),
        TableAccess ta   => EvalTableAccess(ta),
        CallExpr ce      => EvalCall(ce),
        _ => RuntimeResult.Error(Value.FromError(new Error(
                ErrorCode.InternalUnsupportedExpressionType, errorStack: [.. _callStack],
                args: [$"{expr.GetType().Name}"]
            )))
    };

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

    private RuntimeResult EvalTable(TableLiteral tl)
    {
        var d = new Dictionary<string, Value>();
        foreach (var kv in tl.Pairs)
        {
            var v = EvalExpr(kv.Value);
            if (v.IsError) return v;
            d[kv.Key] = v.Value;
        }
        return RuntimeResult.Normal(Value.FromTable(d));
    }

    private RuntimeResult EvalTableAccess(TableAccess ta)
    {
        var tblVal = ResolveIdentifier(ta.Table);
        if (tblVal.IsError) return tblVal;

        var tbl = tblVal.Value.Table;
        if (tbl != null && tbl.TryGetValue(ta.Key, out var v))
            return RuntimeResult.Normal(v);

        return RuntimeResult.Error(Value.FromError(new Error(
            ErrorCode.RuntimeKeyNotFound, errorStack: [.. _callStack],
            args: [ta.Key, ta.Table]
        )));
    }

    private RuntimeResult EvalCall(CallExpr ce)
    {
        string name = ce.Callee switch
        {
            TableAccess t => t.Table + "." + t.Key,
            Ident i       => i.Name,
            _             => "<anonymous>"
        };

        var fnVal = EvalExpr(ce.Callee);
        if (fnVal.IsError) return fnVal;
        var fn = fnVal.Value.Func;
        if (fn == null)
            return RuntimeResult.Error(Value.FromError(new Error(
                ErrorCode.RuntimeInvalidFunctionCall, errorStack: [.. _callStack],
                args: [name]
            )));

        var args = new List<Value>();
        foreach (var a in ce.Args)
        {
            var av = EvalExpr(a);
            if (av.IsError) return av;
            args.Add(av.Value);
        }

        // Invoke
        var result = fn(args);
        if (result.IsError)
            return RuntimeResult.Error(result); //TODO: Allow handling functions with errors

        return RuntimeResult.Normal(result);
    }
}
