using System;
using System.Collections.Generic;
using System.IO;
using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
using SaLang.Common;
using SaLang.Lexing;
using SaLang.Parsing;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public class Interpreter
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

    /// <summary>
    /// Public hook for adding a single built‑in function under a friendly span.
    /// </summary>
    public void AddBuiltin(
        string name,
        Func<List<Value>, Value> implementation,
        string friendlySource = "<memory>",
        int line = 1,
        int column = 1
    )
    {
        Value wrapped(List<Value> args)
        {
            _callStack.Push(new TraceFrame(name, friendlySource, line, column));
            try{ return implementation(args); }
            finally{ _callStack.Pop(); }
        }

        _globals.Define(name, Value.FromFunc(wrapped));
    }

    /// <summary>
    /// Public hook for adding an entire table of functions as a library.
    /// </summary>
    public void AddBuiltinLibrary(
        string tableName,
        Dictionary<string, Func<List<Value>, Value>> impls,
        string friendlySource = "<memory>",
        int line = 1,
        int column = 1)
    {
        var tbl = new Dictionary<string, Value>();
        foreach (var kv in impls)
        {
            Value wrapped(List<Value> args)
            {
                _callStack.Push(new TraceFrame($"{tableName}.{kv.Key}", friendlySource, line, column));
                try { return kv.Value(args); }
                finally { _callStack.Pop(); }
            }
            tbl[kv.Key] = Value.FromFunc(wrapped);
        }
        _globals.Define(tableName, Value.FromTable(tbl));
    }

    private void RegisterDefaultBuiltins()
    {
        var stdImpls = new Dictionary<string, Func<List<Value>, Value>>
        {
            ["print"] = args =>
            {
                var v = args.Count > 0 ? args[0] : Value.Nil();
                Console.WriteLine(v.ToString());
                return Value.Nil();
            },
            ["read"] = args =>
            {
                var v = args.Count > 0 ? args[0] : Value.Nil();
                Console.WriteLine(v);
                return Value.FromString(Console.ReadLine() ?? "");
            },
            ["error"] = args =>
            {
                var v = args.Count > 0 ? args[0] : Value.FromString("?");
                return Value.FromError(new Error(ErrorCode.RuntimeThrownException, errorStack: [.. _callStack],
                args: [v]));
            }
        };
        AddBuiltinLibrary("std", stdImpls);

        AddBuiltin("sum", args => Value.FromNumber(args[0].Number.Value + args[1].Number.Value));
        AddBuiltin("sub", args => Value.FromNumber(args[0].Number.Value - args[1].Number.Value));
        AddBuiltin("mul", args => Value.FromNumber(args[0].Number.Value * args[1].Number.Value));
        AddBuiltin("div", args => Value.FromNumber(args[0].Number.Value / args[1].Number.Value));
        AddBuiltin("len", args =>
        {
            var obj = args.Count > 0 ? args[0] : Value.Nil();
            if (obj.String != null) return Value.FromNumber(obj.String.Length);
            if (obj.Table != null) return Value.FromNumber(obj.Table.Count);

            return Value.FromError(new Error(
                ErrorCode.SemanticInvalidArguments, errorStack: [.. _callStack],
                args: ["len()", "string | table", args[0]]
            ));
        });

        AddBuiltin("require", args =>
        {
            if (args.Count == 0 || args[0].String == null)
                return Value.FromError(new Error(
                    ErrorCode.SemanticInvalidArguments, errorStack: [.. _callStack],
                    args: ["require()", "string", args[0]]
                ));
            string moduleName = args[0].String!;
            
            try{
                var already = _globals.Get(moduleName);
                return already;
            } catch{}

            string filename = ResolveModulePath(moduleName);
            string fullPath = Path.GetFullPath(filename);

            if (!File.Exists(fullPath))
                return Value.FromError(new Error(
                    ErrorCode.IOFileNotFound, errorStack: [.. _callStack],
                    args: [fullPath]
                ));
            string source = File.ReadAllText(filename);

            var tokens = new Lexer(source).Tokenize();
            var prog = new Parser(filename).Parse(tokens);

            var moduleInterp = new Interpreter();
            var result = moduleInterp.Interpret(prog);
            if (result.IsError)
                return result;
            if (result.Kind != ValueKind.Table)
                return Value.FromError(new Error(
                    ErrorCode.IOReadError, errorStack: [.. _callStack],
                    args: [result.Kind == ValueKind.Error? result : $"Expected an table return, got {result.Kind}"]
                ));

            return Value.FromTable(result.Table);

        });
    }

    private string ResolveModulePath(string moduleName)
    {
        if (!moduleName.EndsWith(".sal")) moduleName += ".sal";
        return Path.Combine("modules", moduleName);
    }

    public Value Interpret(ProgramNode prog)
    {
        foreach (var stmt in prog.Stmts)
        {
            var res = ExecStmt(stmt);
            if (res.IsError) return res.Value;
            if (res.IsReturn) return res.Value;
        }
        return Value.Nil(); // Natural end of flow
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
                var rv    = EvalExpr(rs.Expr);
                if (rv.IsError)
                    return RuntimeResult.Error(rv.Value);
                return RuntimeResult.Return(rv.Value);;
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
                var baseTableRes = _env.Get(aa.Table.Table);
                if (baseTableRes.IsError) 
                    return RuntimeResult.Error(baseTableRes);

                var baseTable = baseTableRes.Table;
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
                var tbl = _env.Get(aa.Table.Table).Table;
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
            _env.Assign(aa.Name, res.Value);
            return RuntimeResult.Nothing();
        }
    }

    private RuntimeResult ExecIf(IfStmt iff)
    {
        foreach (var clause in iff.Clauses)
        {
            if (clause.Condition != null)
            {
                var condResult = EvalExpr(clause.Condition).Value;
                if (condResult.Kind == ValueKind.Error)
                    return RuntimeResult.Normal(condResult);
                if (!IsTruthy(condResult))
                    continue;
            }

            // Can execute the body (as else or as truthy result)
            foreach (var stmt in clause.Body)
            {
                var result = ExecStmt(stmt);
                if (result.IsError)
                    return RuntimeResult.Error(result.Value);
                if (result.IsReturn)
                    return RuntimeResult.Return(Value.Nil());
            }
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
                if (res.IsError) return RuntimeResult.Error(res.Value);
                if (res.IsReturn) return RuntimeResult.Return(res.Value);
            }
        }
        return RuntimeResult.Nothing();
    }

    private RuntimeResult ExecForIn(ForInStmt stmt)
    {
        // For varName in iterable do body end
        var iterRes = EvalExpr(stmt.Iterable);
        if (iterRes.IsError) return RuntimeResult.Error(iterRes.Value);
        var iterable = iterRes.Value;
        if (iterable.Table == null)
            return RuntimeResult.Error(Value.FromError(new Error(
                ErrorCode.SemanticInvalidArguments, errorStack: [.. _callStack],
                args: ["for-statement", "table", iterable.Kind]
            )));

        foreach (var kv in iterable.Table)
        {
            // Each entry: kv.Key (string), kv.Value
            var local = new Environment(_env);
            _env = local;
            // Assign loop variable
            local.Define(stmt.VarName, Value.FromString(kv.Key));
            local.Define("value", kv.Value);

            foreach (var s in stmt.Body)
            {
                var res = ExecStmt(s);
                if (res.IsError) return RuntimeResult.Error(res.Value);
                if (res.IsReturn)
                {
                    _env = _env.Parent!;
                    return RuntimeResult.Return(res.Value);
                }
            }

            // Restore
            _env = _env.Parent!;
        }
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
            var thisTbl = _env.Get(fd.Table).Table;
            if (thisTbl != null)
                local.Define("this", Value.FromTable(thisTbl));
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
            if (execRes.IsError)
                return execRes.Value;
            if (execRes.IsReturn)
                return execRes.Value;
            return Value.Nil();
        });

        var tblVal = _env.Get(fd.Table);
        var tbl = tblVal.Table ?? new Dictionary<string, Value>();
        _env.Define(fd.Table, Value.FromTable(tbl));
        tbl[fd.Name] = Value.FromFunc(func);

        return RuntimeResult.Nothing();
    }

    private RuntimeResult EvalExpr(Ast expr) => expr switch
    {
        LiteralNumber ln => RuntimeResult.Normal(Value.FromNumber(ln.Value)),
        LiteralString ls => RuntimeResult.Normal(Value.FromString(ls.Value)),
        Ident id         => RuntimeResult.Normal(_env.Get(id.Name)),
        TableLiteral tl  => EvalTable(tl),
        TableAccess ta   => EvalTableAccess(ta),
        CallExpr ce      => EvalCall(ce),
        _ => RuntimeResult.Error(Value.FromError(new Error(
                ErrorCode.InternalUnsupportedExpressionType, errorStack: [.. _callStack],
                args: [$"{expr.GetType().Name}"]
            )))
    };

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
        var tblVal = _env.Get(ta.Table);
        if (tblVal.IsError) return RuntimeResult.Error(tblVal);

        var tbl = tblVal.Table;
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
