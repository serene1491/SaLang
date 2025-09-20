using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using SaLang.Analyzers.Syntax;
using SaLang.Common;
using SaLang.Syntax.Nodes;

namespace SaLang.Parsing
{
    // Partial extension to Parser that adds AST pretty-printing / tree dumping.
    public partial class Parser
    {
        /// <summary>
        /// Parse the tokens and print the generated AST to the console.
        /// Returns the original parse result so callers can inspect diagnostics if needed.
        /// </summary>
        public SyntaxResult<ProgramNode> ParseAndPrintTree(List<Token> tokens)
        {
            var res = Parse(tokens);
            if (res.TryGetValue(out var prog))
            {
                var tree = GetAstTreeString(prog);
                Console.WriteLine(tree);
            }
            else if (res.TryGetError(out var err))
            {
                Console.WriteLine($"Parse failed: {err.Code}");
            }

            return res;
        }

        /// <summary>
        /// Produces a human-readable string that represents the AST tree.
        /// This printer focuses on showing meaningful AST structure and values,
        /// while omitting CLR-internal/list-implementation noise (Capacity, _items, version, etc.).
        /// </summary>
        public string GetAstTreeString(ProgramNode prog)
        {
            var sb = new StringBuilder();
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            PrintValue(prog, 0, sb, visited);
            return sb.ToString();
        }

        private static bool IsAstType(Type t)
        {
            if (t == null) return false;
            if (typeof(Ast).IsAssignableFrom(t)) return true;
            if (t.Namespace != null && (t.Namespace.StartsWith("SaLang.Syntax") || t.Namespace.StartsWith("SaLang")))
                return true;
            return false;
        }

        private static bool IsCollectionType(Type t)
        {
            if (t == null) return false;
            return typeof(System.Collections.IEnumerable).IsAssignableFrom(t) && t != typeof(string);
        }

        private void PrintValue(object obj, int indent, StringBuilder sb, HashSet<object> visited)
        {
            string pad(int n) => new string(' ', n);

            if (obj == null)
            {
                sb.AppendLine(pad(indent) + "<null>");
                return;
            }

            var t = obj.GetType();

            // Simple primitives and strings
            if (t.IsPrimitive || obj is decimal)
            {
                sb.AppendLine(pad(indent) + obj.ToString());
                return;
            }
            if (obj is string s)
            {
                sb.AppendLine(pad(indent) + $"\"{s}\"");
                return;
            }

            // Special case: Span-like objects (show File/Line/Column if available)
            if (t.Name == "Span" || t.Name.EndsWith("Span"))
            {
                sb.AppendLine(pad(indent) + t.Name);
                var fileProp = t.GetProperty("File") ?? t.GetProperty("_file", BindingFlags.NonPublic | BindingFlags.Instance);
                var lineProp = t.GetProperty("Line") ?? t.GetProperty("_line", BindingFlags.NonPublic | BindingFlags.Instance);
                var colProp = t.GetProperty("Column") ?? t.GetProperty("_column", BindingFlags.NonPublic | BindingFlags.Instance);
                if (fileProp != null)
                {
                    var fileVal = fileProp.GetValue(obj) as string;
                    sb.AppendLine(pad(indent + 2) + $"File: \"{fileVal}\"");
                }
                if (lineProp != null)
                    sb.AppendLine(pad(indent + 2) + $"Line: {lineProp.GetValue(obj)}");
                if (colProp != null)
                    sb.AppendLine(pad(indent + 2) + $"Column: {colProp.GetValue(obj)}");
                return;
            }

            // If this object is not an AST type and not a collection of AST types,
            // print a short summary instead of descending — prevents walking runtime structures.
            if (!IsAstType(t) && !IsCollectionType(t))
            {
                sb.AppendLine(pad(indent) + $"{t.Name} (summary)");
                return;
            }

            // Cycle guard
            if (visited.Contains(obj))
            {
                sb.AppendLine(pad(indent) + $"<seen {t.Name}>");
                return;
            }

            // Header
            // For lists/collections show a concise header with element type and count when possible
            if (IsCollectionType(t))
            {
                var count = TryGetCount(obj);
                var elementTypeName = TryGetElementTypeName(t);
                sb.AppendLine(pad(indent) + $"{t.Name}{(elementTypeName != null ? $"<{elementTypeName}>" : "")} (Count: {count?.ToString() ?? "?"})");
                visited.Add(obj);

                // If the collection contains AST elements, enumerate and print them; otherwise summarize.
                if (EnumerableHasAstElements(obj as System.Collections.IEnumerable ?? Enumerable.Empty<object>()))
                {
                    int i = 0;
                    foreach (var item in (obj as System.Collections.IEnumerable))
                    {
                        sb.AppendLine(pad(indent + 2) + $"[{i}]:");
                        PrintValue(item, indent + 4, sb, visited);
                        i++;
                    }
                }

                visited.Remove(obj);
                return;
            }

            sb.AppendLine(pad(indent) + t.Name);
            visited.Add(obj);

            // Print only meaningful properties/fields: primitives, strings, AST types, and collections of ASTs.
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                         .Where(p => p.GetIndexParameters().Length == 0)
                         .OrderBy(p => p.Name);

            foreach (var p in props)
            {
                // Skip internal CLR collection properties like System.Collections.* interfaces
                if (p.PropertyType != null && p.PropertyType.Namespace != null && p.PropertyType.Namespace.StartsWith("System.Collections"))
                    continue;

                // Skip compiler/backing-field-like properties
                if (p.Name.StartsWith("<") || p.Name.Contains("EqualityContract"))
                    continue;

                object val;
                try { val = p.GetValue(obj); }
                catch { continue; }

                if (val == null)
                {
                    sb.AppendLine(pad(indent + 2) + $"{p.Name}: null");
                    continue;
                }

                var vt = val.GetType();

                // Show primitives and strings inline
                if (vt.IsPrimitive || val is decimal)
                {
                    sb.AppendLine(pad(indent + 2) + $"{p.Name}: {val}");
                    continue;
                }
                if (val is string ss)
                {
                    sb.AppendLine(pad(indent + 2) + $"{p.Name}: \"{ss}\"");
                    continue;
                }

                // If property is AST or collection of AST, descend
                if (IsAstType(vt) || (val is System.Collections.IEnumerable ie && EnumerableHasAstElements(ie)))
                {
                    sb.AppendLine(pad(indent + 2) + $"{p.Name}:");
                    PrintValue(val, indent + 4, sb, visited);
                }
                else
                {
                    // Otherwise print a short summary only
                    sb.AppendLine(pad(indent + 2) + $"{p.Name}: {vt.Name} (summary)");
                }
            }

            // Fields (some AST implementations may use fields)
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                          .OrderBy(f => f.Name);

            foreach (var f in fields)
            {
                // Skip internal list backing fields
                if (f.Name.StartsWith("_") && (f.Name.Contains("items") || f.Name.Contains("version") || f.Name.Contains("size")))
                    continue;

                object val;
                try { val = f.GetValue(obj); }
                catch { continue; }

                if (val == null)
                {
                    sb.AppendLine(pad(indent + 2) + $"{f.Name}: null");
                    continue;
                }

                var vt = val.GetType();
                if (vt.IsPrimitive || val is decimal)
                {
                    sb.AppendLine(pad(indent + 2) + $"{f.Name}: {val}");
                    continue;
                }
                if (val is string sff)
                {
                    sb.AppendLine(pad(indent + 2) + $"{f.Name}: \"{sff}\"");
                    continue;
                }

                if (IsAstType(vt) || (val is System.Collections.IEnumerable ie2 && EnumerableHasAstElements(ie2)))
                {
                    sb.AppendLine(pad(indent + 2) + $"{f.Name}:");
                    PrintValue(val, indent + 4, sb, visited);
                }
                else
                {
                    sb.AppendLine(pad(indent + 2) + $"{f.Name}: {vt.Name} (summary)");
                }
            }

            visited.Remove(obj);
        }

        private static string TryGetElementTypeName(Type t)
        {
            if (t.IsGenericType)
            {
                var args = t.GetGenericArguments();
                if (args.Length == 1) return args[0].Name;
            }
            return null;
        }

        private static int? TryGetCount(object obj)
        {
            if (obj is System.Collections.ICollection c) return c.Count;
            var t = obj.GetType();
            var countProp = t.GetProperty("Count") ?? t.GetProperty("Length");
            if (countProp != null)
            {
                try { var v = countProp.GetValue(obj); if (v is int i) return i; }
                catch { }
            }
            return null;
        }

        // Heuristic: does the enumerable contain any AST elements? Sample at most 16 items.
        private static bool EnumerableHasAstElements(System.Collections.IEnumerable ie)
        {
            if (ie == null) return false;
            int tried = 0;
            foreach (var it in ie)
            {
                tried++;
                if (it == null) continue;
                if (IsAstType(it.GetType())) return true;
                if (tried > 16) break; // sample limit
            }
            return false;
        }

        // Simple reference-equality comparer used by the visited set
        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static ReferenceEqualityComparer Instance { get; } = new ReferenceEqualityComparer();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
