using System;
using System.IO;
using SaLang.Lexing;
using SaLang.Parsing;
using SaLang.Runtime;
namespace SaLang;

public static class TestRunner
{
    public static void RunAllTests(string testDir = "Tastes")
    {
        var files = Directory.GetFiles(testDir, "*.sal", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            Console.WriteLine($"\nTasting: {file}");

            try
            {
                string code = File.ReadAllText(file);

                var lexer = new Lexer(code);
                var tokens = lexer.Tokenize();

                var parser = new Parser(file);
                var ast = parser.Parse(tokens);

                var interpreter = new Interpreter();
                var result = interpreter.Interpret(ast);
                if (result.IsError)
                {
                    Console.WriteLine($"❌ Bitter {Path.GetFullPath(file)}:");
                    Console.WriteLine($"{result}");
                    continue;
                }

                Console.WriteLine($"✅ Sweet. {Path.GetFullPath(file)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Bitter. {Path.GetFullPath(file)}:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
