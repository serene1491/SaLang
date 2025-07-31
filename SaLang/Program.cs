using SaLang.Analyzers;
using SaLang.Lexing;
using SaLang.Parsing;
using SaLang.Runtime;
using SaLang.Syntax.Nodes;
using System;
namespace SaLang;

// Engine
public static class Engine
{
    public static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            if (args[0] == "taste-mode")
            {
                TestRunner.RunAllTests();
                return;
            }
        }

        while (true)
        {
            Console.Write("> ");
            string code = Console.ReadLine();
            if (string.IsNullOrEmpty(code))
                break;

            var lex = new Lexer(code);
            var toks = lex.Tokenize();
            var parser = new Parser();
            var ast = parser.Parse(toks);
            if (ast.IsError)
            {
                ast.TryGetError(out Error error);
                Console.WriteLine($"{error}");
                continue;
            }
            ast.TryGetValue(out ProgramNode programNode);

            var moduleInterp = new Interpreter();
            var result = moduleInterp.Interpret(programNode);
            if (result.IsError)
            {
                Console.WriteLine($"{result}");
                continue;
            }
            Console.WriteLine($"[finished] << {result}");
        }
    }
}
