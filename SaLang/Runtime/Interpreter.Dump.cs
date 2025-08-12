using System.Collections.Generic;
using SaLang.Analyzers;
using SaLang.Analyzers.Runtime;
using SaLang.Common;
using SaLang.Syntax.Nodes;
namespace SaLang.Runtime;

public partial class Interpreter
{
    private static string Dump(Value v)
    {
        switch (v.Kind)
        {
            case ValueKind.Nil: return "Nil";
            case ValueKind.Number: return $"Number({v.Number})";
            case ValueKind.Bool: return $"Bool({v.Bool})";
            case ValueKind.String: return $"String(\"{v.String}\")";
            case ValueKind.Function: return $"Func(#{v.Func?.GetHashCode() ?? 0})";
            case ValueKind.Table:
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append("Table{");
                    bool first = true;
                    foreach (var kv in v.Table)
                    {
                        if (!first) sb.Append(", ");
                        first = false;
                        sb.Append(kv.Key);
                        sb.Append(":");
                        if (kv.Value.Kind == ValueKind.Table) sb.Append("Table(...)");
                        else if (kv.Value.Kind == ValueKind.String) sb.Append($"\"{kv.Value.String}\"");
                        else if (kv.Value.Kind == ValueKind.Number) sb.Append(kv.Value.Number);
                        else sb.Append(kv.Value.Kind);
                    }
                    sb.Append("}");
                    return sb.ToString();
                }
            case ValueKind.Error:
                return $"Error(code={(int)v.Error.Value.Code}, msg=\"{v.Error.Value.Message}\")";
            default:
                return $"UnknownKind({v.Kind})";
        }
    }
}