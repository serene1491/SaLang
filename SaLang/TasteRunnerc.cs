using System;
using System.IO;
using SaLang.Analyzers;
using SaLang.Lexing;
using SaLang.Parsing;
using SaLang.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang;

public static class TestRunner
{
    public static void RunAllTests(string testDir = "Tastes")
    {
        var files = Directory.GetFiles(testDir, "*.sal", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            Console.WriteLine($"\nTasting: {file}");
            string code = File.ReadAllText(file);

            var lexer = new Lexer(code);
            var tokens = lexer.Tokenize();

            var parser = new Parser(file);
            var ast = parser.Parse(tokens);
            if (ast.IsError)
            {
                ast.TryGetError(out Error error);
                Console.WriteLine($"❌ Bitter {Path.GetFullPath(file)}: Syntax Exception");
                Console.WriteLine($"{error}");
                continue;
            }
            ast.TryGetValue(out ProgramNode programNode);

            var moduleInterp = new Interpreter();
            var result = moduleInterp.Interpret(programNode);
            if (result.IsError)
            {
                Console.WriteLine($"❌ Bitter {Path.GetFullPath(file)}: Runtime Exception");
                Console.WriteLine($"{result}");
                continue;
            }

            Console.WriteLine($"✅ Sweet. {Path.GetFullPath(file)}");
        }
    }
}
