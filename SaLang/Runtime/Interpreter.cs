using System.Collections.Generic;
using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
using SaLang.Common;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    public string CurrentScriptPath { get; }
    private readonly Environment _globals = new();
    private Environment _env;
    private readonly Stack<TraceFrame> _callStack = new();
    private static bool IsTruthy(Value v) =>
        v.Kind != ValueKind.Nil &&
        !(v.Kind == ValueKind.Bool && v.Bool == false);
    public Interpreter(string currentScriptPath = null)
    {
        CurrentScriptPath = currentScriptPath;
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
                val = ExecFuncDecl(fd);
                break;
            case ExpressionStmt es:
                val = EvalExpr(es.Expr);
                break;
            case ReturnStmt rs:
                var rv = EvalExpr(rs.Expr);
                if (rv.IsError)
                    return RuntimeResult.Error(rv.Value);

                System.Console.WriteLine($"[ReturnStmt] returning -> {Dump(rv.Value)}");
                return RuntimeResult.Return(rv.Value);
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

    private RuntimeResult EvalExpr(Ast expr)
    {
        switch (expr)
        {
            case LiteralNumber ln:
                return RuntimeResult.Normal(Value.FromNumber(ln.Value));
            case LiteralNil:
                return RuntimeResult.Normal(Value.Nil());
            case LiteralBool lb:
                return RuntimeResult.Normal(Value.FromBool(lb.Value));
            case LiteralString ls:
                return RuntimeResult.Normal(Value.FromString(ls.Value));
            case Ident id:
                return ResolveIdentifier(id.Name);
            case TableLiteral tl:
                return EvalTable(tl);
            case TableAccess ta:
                return EvalTableAccess(ta);
            case CallExpr ce:
            {
                var val = EvalCall(ce);
                if (val.IsError) return val;
                return val;
            }
            default:
                return RuntimeResult.Error(Value.FromError(new Error(
                    ErrorCode.InternalUnsupportedExpressionType,
                    errorStack: [.. _callStack],
                    args: [$"{expr.GetType().Name}"]
                )));
        }
    }
}
