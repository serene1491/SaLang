using System;
using System.Collections.Generic;
using System.IO;
using SaLang.Analyzers;
using SaLang.Common;
using SaLang.Lexing;
using SaLang.Parsing;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
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
            try { return implementation(args); }
            finally { _callStack.Pop(); }
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
                var v = args.Count > 0 ? args[0] : Value.FromString("");
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

        AddBuiltin("sum", args =>{
            var a = GetNumberArg(args, 0, "sum", 2); if (a.IsError) return a;
            var b = GetNumberArg(args, 1, "sum", 2); if (b.IsError) return b;
            return Value.FromNumber(a.Number.Value + b.Number.Value);
        });
        AddBuiltin("sub", args =>{
            var a = GetNumberArg(args, 0, "sub", 2); if (a.IsError) return a;
            var b = GetNumberArg(args, 1, "sub", 2); if (b.IsError) return b;
            return Value.FromNumber(a.Number.Value - b.Number.Value);
        });
        AddBuiltin("mul", args =>{
            var a = GetNumberArg(args, 0, "mul", 2); if (a.IsError) return a;
            var b = GetNumberArg(args, 1, "mul", 2); if (b.IsError) return b;
            return Value.FromNumber(a.Number.Value * b.Number.Value);
        });
        AddBuiltin("div", args =>{
            var a = GetNumberArg(args, 0, "div", 2); if (a.IsError) return a;
            var b = GetNumberArg(args, 1, "div", 2); if (b.IsError) return b;
            return Value.FromNumber(a.Number.Value / b.Number.Value);
        });
        AddBuiltin("len", args =>{
            var obj = args.Count > 0 ? args[0] : Value.Nil();
            if (obj.String != null) return Value.FromNumber(obj.String.Length);
            if (obj.Table  != null) return Value.FromNumber(obj.Table.Count);
            return Value.FromError(new Error(
                ErrorCode.SemanticInvalidArguments,
                errorStack: [.. _callStack],
                args: ["len()", "string | table", obj]
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
            
            var already = _globals.Get(moduleName);
            if (already is not null)
                return already.Value; // If the module is already among the globals

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
            if (prog.IsError)
            {
                prog.TryGetError(out Error error);
                return Value.FromError(error);
            }
            prog.TryGetValue(out ProgramNode programNode);

            var moduleInterp = new Interpreter();
            var result = moduleInterp.Interpret(programNode);
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

    private static string ResolveModulePath(string moduleName)
    {
        if (!moduleName.EndsWith(".sal")) moduleName += ".sal";
        return Path.Combine("modules", moduleName);
    }
    
    private Value GetNumberArg(List<Value> args, int idx, string funcName, int paramsIdx)
    {
        if (paramsIdx != args.Count)
            return Value.FromError(new Error(
                ErrorCode.SemanticArgumentsMismatch, errorStack: [.. _callStack],
                args: [$"{funcName}()", paramsIdx, args.Count]
            ));
        
        else if (args[idx].Kind != ValueKind.Number || !args[idx].Number.HasValue)
        {
            return Value.FromError(new Error(
                ErrorCode.SemanticInvalidArguments, errorStack: [.. _callStack],
                args: [$"{funcName}()", "(Number, Number)", $"({args[0].Kind}, {args[1].Kind})"]
            ));
        }
        return args[idx];
    }
}