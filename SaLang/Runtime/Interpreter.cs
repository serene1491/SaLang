using System;
using System.Collections.Generic;
using System.IO;
using SaLang.Lexing;
using SaLang.Parsing;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public class Interpreter
{
    private readonly Environment globals = new();
    private Environment env;
    private readonly Stack<TraceFrame> callStack = new();

    public Interpreter()
    {
        env = globals;
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
            callStack.Push(new TraceFrame(name, friendlySource, line, column));
            try{ return implementation(args); }
            finally{ callStack.Pop(); }
        }

        globals.Define(name, Value.FromFunc(wrapped));
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
                callStack.Push(new TraceFrame($"{tableName}.{kv.Key}", friendlySource, line, column));
                try { return kv.Value(args); }
                finally { callStack.Pop(); }
            }
            tbl[kv.Key] = Value.FromFunc(wrapped);
        }
        globals.Define(tableName, Value.FromTable(tbl));
    }

    private void RegisterDefaultBuiltins()
    {
        var stdImpls = new Dictionary<string, Func<List<Value>, Value>>
        {
            ["print"] = args =>{
                var v = args.Count > 0 ? args[0] : Value.Nil();
                Console.WriteLine(v.ToString());
                return Value.Nil();
            },
            ["read"] = args =>{
                var v = args.Count > 0 ? args[0] : Value.Nil();
                Console.WriteLine(v);
                return Value.FromString(Console.ReadLine() ?? "nil");
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
            return Value.Error("len() expects a string or table", [.. callStack]);
        });

        AddBuiltin("require", args =>
        {
            if (args.Count == 0 || args[0].String == null)
                return Value.Error("require() expects a module name string", [.. callStack]);
            string moduleName = args[0].String!;
            
            try{
                var already = globals.Get(moduleName);
                return already;
            } catch{}

            string filename = ResolveModulePath(moduleName);
            string fullPath = Path.GetFullPath(filename);

            if (!File.Exists(fullPath))
                return Value.Error($"Module '{moduleName}' not found at '{fullPath}'", [.. callStack]);
            string source = File.ReadAllText(filename);

            var tokens = new Lexer(source).Tokenize();
            var prog = new Parser(filename).Parse(tokens);

            var moduleInterp = new Interpreter();
            var result = moduleInterp.Interpret(prog);
            if (result.IsError)
                return result;
            if (result.Kind != ValueKind.Table)
                return Value.Error($"Module '{moduleName}' must return a table");

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

    private ExecResult ExecStmt(Ast node)
    {
        Value val;
        switch (node)
        {
            case VarDeclaration vd:
                val = ExecVar(vd);
                break;
            case AssignAs aa:
                val = ExecAs(aa);
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
                    return ExecResult.Error(rv);
                return ExecResult.Return(rv);
            default:
                return ExecResult.Error(Value.Error(
                    $"Unknown statement {node.GetType().Name}", 
                    [.. callStack]
                ));
        }

        if (val.IsError)
            return ExecResult.Error(val);

        return ExecResult.Normal(val);
    }

    private Value ExecVar(VarDeclaration vd)
    {
        var val = EvalExpr(vd.Expr);
        if (val.IsError) return val;
        env.Define(vd.Name, val);
        return Value.Nil();
    }

    private Value ExecAs(AssignAs aa)
    {
        var val = EvalExpr(aa.Expr);
        if (val.IsError) return val;
        env.Define(aa.Name, val);
        return Value.Nil();
    }

    private Value ExecFunc(FuncDecl fd)
    {
        var func = new FuncValue(args =>
        {
            // Push frame
            callStack.Push(new TraceFrame(
                fd.Name,
                fd.Span.File,
                fd.Span.Line,
                fd.Span.Column
            ));

            var local = new Environment(env);
            var thisTbl = env.Get(fd.Table).Table;
            if (thisTbl != null)
                local.Define("this", Value.FromTable(thisTbl));
            for (int i = 0; i < fd.Params.Count; i++)
                local.Define(fd.Params[i], i < args.Count ? args[i] : Value.Nil());

            var prev = env;
            env = local;
            ExecResult execRes = ExecResult.Normal(Value.Nil());
            foreach (var s in fd.Body)
            {
                execRes = ExecStmt(s);
                if (execRes.IsError || execRes.IsReturn)
                    break;
            }
            env = prev;

            // Pop frame
            callStack.Pop();

            // Return type
            if (execRes.IsError)
                return execRes.Value;
            if (execRes.IsReturn)
                return execRes.Value;
            return Value.Nil();
        });

        var tblVal = env.Get(fd.Table);
        var tbl    = tblVal.Table ?? new Dictionary<string, Value>();
        env.Define(fd.Table, Value.FromTable(tbl));
        tbl[fd.Name] = Value.FromFunc(func);

        return Value.Nil();
    }

    private Value EvalExpr(Ast expr) => expr switch
    {
        LiteralNumber ln => Value.FromNumber(ln.Value),
        LiteralString ls => Value.FromString(ls.Value),
        Ident id         => env.Get(id.Name),
        TableLiteral tl  => EvalTable(tl),
        TableAccess ta   => EvalTableAccess(ta),
        CallExpr ce      => EvalCall(ce),
        _ => Value.Error(
                $"Cannot evaluate expression type {expr.GetType().Name}",
                [.. callStack]
                )
    };

    private Value EvalTable(TableLiteral tl)
    {
        var d = new Dictionary<string, Value>();
        foreach (var kv in tl.Pairs)
        {
            var v = EvalExpr(kv.Value);
            if (v.IsError) return v;
            d[kv.Key] = v;
        }
        return Value.FromTable(d);
    }

    private Value EvalTableAccess(TableAccess ta)
    {
        var tblVal = env.Get(ta.Table);
        if (tblVal.IsError) return tblVal;

        var tbl = tblVal.Table;
        if (tbl != null && tbl.TryGetValue(ta.Key, out var v))
            return v;

        return Value.Error(
            $"Key '{ta.Key}' not found in table '{ta.Table}'",
            [.. callStack]
        );
    }

    private Value EvalCall(CallExpr ce)
    {
        string name = ce.Callee switch
        {
            TableAccess t => t.Table + "." + t.Key,
            Ident i       => i.Name,
            _             => "<anon>"
        };

        var fnVal = EvalExpr(ce.Callee);
        if (fnVal.IsError) return fnVal;
        var fn = fnVal.Func;
        if (fn == null)
            return Value.Error(
                $"Attempt to call non-function '{name}'",
                [.. callStack]
            );

        var args = new List<Value>();
        foreach (var a in ce.Args)
        {
            var av = EvalExpr(a);
            if (av.IsError) return av;
            args.Add(av);
        }

        // Invoke
        var result = fn(args);
        if (result.IsError)
            // If native or user fn returned an error already, bubble it
            return result;

        return result;
    }
}
