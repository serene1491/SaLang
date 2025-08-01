using SaLang.Lexing;
using SaLang.Parsing;
using SaLang.Runtime;
using SaLang.Syntax.Nodes;
namespace SaLang;

public static partial class Interpreter
{
    /// <summary>
    /// Executes a piece of SaLang code and returns the result
    /// </summary>
    public static Value Execute(string source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();

        var parser = new Parser();
        var syntaxRes = parser.Parse(tokens);
        if (syntaxRes.IsError){
            syntaxRes.TryGetError(out var err);
            return Value.FromError(err);
        }
        syntaxRes.TryGetValue(out ProgramNode program);

        var interp = new Runtime.Interpreter();
        var result = interp.Interpret(program);
        if (result.IsError){
            syntaxRes.TryGetError(out var err);
            return Value.FromError(err);
        }

        return result;
    }
}
