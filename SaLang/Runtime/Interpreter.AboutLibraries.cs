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
    public void AddBuiltin(string name, Func<List<Value>, Value> implementation,
        string friendlySource = "<memory>", int line = 1, int column = 1)
    {
        Value Wrapped(List<Value> args)
        {
            _callStack.Push(new TraceFrame(name, friendlySource, line, column));
            try { return implementation(args); }
            finally { _callStack.Pop(); }
        }
        _globals.Define(name, Value.FromFunc(Wrapped));
    }

    public void AddBuiltinLibrary(string tableName, Dictionary<string, Func<List<Value>, Value>> impls,
        string friendlySource = "<memory>", int line = 1, int column = 1)
    {
        var table = new Dictionary<string, Value>();
        foreach (var (fn, impl) in impls)
            AddTableEntry(table, tableName, fn, impl, friendlySource, line, column);
        _globals.Define(tableName, Value.FromTable(table));
    }

    private void AddTableEntry(Dictionary<string, Value> table,
        string tableName, string fn, Func<List<Value>, Value> impl,
        string source, int line, int column)
    {
        Value Wrapped(List<Value> args)
        {
            var fullName = $"{tableName}.{fn}";
            _callStack.Push(new TraceFrame(fullName, source, line, column));
            try { return impl(args); }
            finally { _callStack.Pop(); }
        }
        table[fn] = Value.FromFunc(Wrapped);
    }

    private void AddBinaryOp(string name, Func<double, double, double> op)
    {
        AddBuiltin(name, args =>
        {
            if (args.Count != 2) return ArgumentError(name, 2, args.Count);

            var a = args[0].Number;
            var b = args[1].Number;
            if (!a.HasValue || !b.HasValue) return TypeError(name, "Number, Number", args[0], args[1]);

            return Value.FromNumber(op(a.Value, b.Value));
        });
    }

    private void AddUnaryOp<T>(string name, Func<T, Value> op, Func<Value, T?> extractor,
        string expectedType) where T : struct
    {
        AddBuiltin(name, args =>
        {
            if (args.Count != 1) return ArgumentError(name, 1, args.Count);

            var raw = args[0];
            var maybe = extractor(raw);
            if (!maybe.HasValue) return TypeError(name, expectedType, raw);

            return op(maybe.Value);
        });
    }

    private void AddBinaryValueOp(
        string name,
        Func<Value, Value, Value> op)
    {
        AddBuiltin(name, args =>
        {
            if (args.Count != 2)
                return ArgumentError(name, 2, args.Count);
            return op(args[0], args[1]);
        });
    }

    private Value ArgumentError(string fn, int expected, int got)
        => Value.FromError(new Error(
            ErrorCode.SemanticArgumentsMismatch,
            errorStack: [.._callStack],
            args: [$"{fn}()", expected, got]
        ));

    private Value TypeError(string fn, string expected, params Value[] got)
        => Value.FromError(new Error(
            ErrorCode.SemanticInvalidArguments,
            errorStack: [.._callStack],
            args: new object[] { $"{fn}()", expected, FormatKinds(got) }
        ));

    private static string FormatKinds(Value[] values)
        => string.Join(
            ", ",
            Array.ConvertAll(values, v => v.Kind.ToString())
        );

    // Register all builtins using helpers
    private void RegisterDefaultBuiltins()
    {
        // Standard library
        AddBuiltinLibrary("std", new Dictionary<string, Func<List<Value>, Value>>
        {
            ["print"] = args => { Console.WriteLine(args.Count > 0 ? args[0].ToString() : string.Empty); return Value.Nil(); },
            ["read"] = args => { Console.Write(args.Count > 0 ? args[0].ToString() : string.Empty); var input = Console.ReadLine() ?? string.Empty; return Value.FromString(input); },
            ["error"] = args => Value.FromError(new Error(ErrorCode.RuntimeThrownException,
                                    errorStack: [.. _callStack], args: [args.Count > 0 ? args[0] : Value.FromString("?")])),
        });

        AddBuiltin("require", args =>{
            if (args.Count == 0 || args[0].String is not string moduleName)
                return Value.FromError(new Error(
                    ErrorCode.SemanticInvalidArguments,
                    errorStack: [.. _callStack],
                    args: ["require()", "string", args.Count > 0 ? args[0] : "null"]
                ));

            var already = _globals.Get(moduleName);
            if (already is not null)
                return already.Value; // If the module is already among the globals

            if (!moduleName.EndsWith(".sal"))
                moduleName += ".sal";

            string baseDir = CurrentScriptPath is not null
                ? Path.GetDirectoryName(CurrentScriptPath)!
                : Directory.GetCurrentDirectory();

            string candidate = Path.Combine(baseDir, "modules", moduleName);
            if (!File.Exists(candidate))
                candidate = Path.Combine(Directory.GetCurrentDirectory(), "modules", moduleName);

            if (!File.Exists(candidate))
            {
                return Value.FromError(new Error(
                    ErrorCode.IOFileNotFound,
                    errorStack: [.. _callStack],
                    args: [candidate]
                ));
            }

            string source = File.ReadAllText(candidate);

            var tokens = new Lexer(source).Tokenize();
            var progRes = new Parser(candidate).Parse(tokens);
            if (progRes.IsError)
            {
                progRes.TryGetError(out var err);
                return Value.FromError(err);
            }
            progRes.TryGetValue(out ProgramNode programNode);

            var moduleInterp2 = new Interpreter(candidate);
            var result = moduleInterp2.Interpret(programNode);

            if (result.IsError) return result;

            if (result.Kind != ValueKind.Table)
                return Value.FromError(new Error(
                    ErrorCode.IOReadError,
                    errorStack: [.. _callStack],
                    args: [$"Expected a table return, got {result.Kind}"]
                ));

            var table = result.Table;
            return Value.FromTable(table);
        });

        AddBuiltin("tonumber", args =>{
            if (args.Count != 1) return ArgumentError("tonumber", 1, args.Count);
            if (args[0].Number.HasValue) return args[0];
            if (args[0].String is string s && double.TryParse(s, out var v)) return Value.FromNumber(v);
            return TypeError("tonumber", "string | number", args[0]);
        });
        AddBuiltin("tostr", args =>{
            if (args.Count != 1) return ArgumentError("tostr", 1, args.Count);
            return Value.FromString(args[0].ToString());
        });
        AddBuiltin("tobool", args =>{
            if (args.Count != 1) return ArgumentError("tobool", 1, args.Count);
            if (args[0].Bool.HasValue) return Value.FromBool(args[0].Bool.Value);
            if (args[0].String == "true") return Value.FromBool(true);
            if (args[0].String == "false") return Value.FromBool(false);
            return TypeError("tobool", "bool | string", args[0]);
        });

        AddBinaryOp("__sum", (a, b) => a + b);
        AddBinaryOp("__sub", (a, b) => a - b);
        AddBinaryOp("__mul", (a, b) => a * b);
        AddBinaryOp("__div", (a, b) => a / b);
        AddBinaryOp("__module", (a, b) => a % b);
        AddBinaryValueOp("__concat", (l, r) => Value.FromString(l.ToString() + r.ToString()));

        AddBinaryValueOp("__greater", (l, r) =>{
            var a = l.Number; var b = r.Number;
            if (!a.HasValue || !b.HasValue) return TypeError("__greater", "Number, Number", l, r);
            return Value.FromBool(a.Value > b.Value);
        });
        AddBinaryValueOp("__less", (l, r) =>{
            var a = l.Number; var b = r.Number;
            if (!a.HasValue || !b.HasValue) return TypeError("__less", "Number, Number", l, r);
            return Value.FromBool(a.Value < b.Value);
        });
        AddBinaryValueOp("__lessEquals", (l, r) =>{
            var a = l.Number; var b = r.Number;
            if (!a.HasValue || !b.HasValue) return TypeError("__lessEquals", "Number, Number", l, r);
            return Value.FromBool(a.Value <= b.Value);
        });
        AddBinaryValueOp("__greaterEquals", (l, r) =>{
            var a = l.Number; var b = r.Number;
            if (!a.HasValue || !b.HasValue) return TypeError("__greaterEquals", "Number, Number", l, r);
            return Value.FromBool(a.Value >= b.Value);
        });

        AddBinaryValueOp("__equals", (l, r) =>{
            var eq = TryCompareNumbers(l, r, out var cmp)
                ? cmp == 0
                : string.Equals(l.ToString(), r.ToString(), StringComparison.Ordinal);
            return Value.FromBool(eq);
        });
        AddBinaryValueOp("__lessEquals", (l, r) => Value.FromBool(TryCompareNumbers(l, r, out var c1) ? c1 <= 0 : string.Compare(l.ToString(), r.ToString(), StringComparison.Ordinal) <= 0));
        AddBinaryValueOp("__greaterEquals", (l, r) => Value.FromBool(TryCompareNumbers(l, r, out var c2) ? c2 >= 0 : string.Compare(l.ToString(), r.ToString(), StringComparison.Ordinal) >= 0));
        AddBinaryValueOp("__notEquals", (l, r) => Value.FromBool(!(
                TryCompareNumbers(l, r, out var c3) ? c3 == 0
                : string.Equals(l.ToString(), r.ToString(), StringComparison.Ordinal)
            )));

        // Approximate equals tolerant to mixed types
        AddBinaryValueOp("__approxmate", (l, r) =>{
            if (TryParseMixed(l, r, out var x, out var y)) return Value.FromBool(Math.Abs(x - y) < 1e-9);
            return Value.FromBool(string.Equals(l.ToString(), r.ToString(), StringComparison.Ordinal));
        });

        AddBinaryValueOp("__or", (l, r) => Value.FromBool((l.Bool ?? false) || (r.Bool ?? false)));
        AddBinaryValueOp("__and", (l, r) => Value.FromBool((l.Bool ?? false) && (r.Bool ?? false)));

        AddUnaryOp<int>("__len",
            len => Value.FromNumber(len),
            v => v.String?.Length,
            expectedType: "string | table");
        AddUnaryOp<bool>("__not",
            b => Value.FromBool(!b),
            v => v.Bool,
            expectedType: "bool");
    }

    private static bool TryCompareNumbers(Value l, Value r, out int result)
    {
        if (l.Number.HasValue && r.Number.HasValue){
            result = l.Number.Value.CompareTo(r.Number.Value); return true;
        }
        result = 0;
        return false;
    }

    private static bool TryParseMixed(Value l, Value r, out double x, out double y)
    {
        if (l.Number.HasValue && r.Number.HasValue)
        {
            x = l.Number.Value; y = r.Number.Value;
            return true;
        }
        if (double.TryParse(l.ToString(), out x) && double.TryParse(r.ToString(), out y)) return true;
        x = y = 0;
        return false;
    }
}
