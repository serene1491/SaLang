using SaLang.Lexing;
using SaLang.Parsing;
using SaLang.Runtime;
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
            string code = Console.ReadLine();
            if (string.IsNullOrEmpty(code))
                break;

            try
            {
                var lex = new Lexer(code);
                var toks = lex.Tokenize();
                var parser = new Parser();
                var ast = parser.Parse(toks);
                var interp = new Interpreter();
                var result = interp.Interpret(ast);
                if (result.IsError)
                    Console.WriteLine($"{result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.WriteLine("[finished]");
        }
    }
}
