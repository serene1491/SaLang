using System.Collections.Generic;
using SaLang.Syntax;
namespace SaLang.Lexing;

// Lexer: transforma fonte em tokens
public class Lexer
{
    public Lexer(string code) { this.src = code; }
    private readonly string src;
    private int i = 0, line = 1, col = 1;
    private readonly List<Token> tokens = new();

    private char Curr => i < src.Length ? src[i] : '\0';
    private void Advance() { if (Curr == '\n') { line++; col = 1; } else col++; i++; }

    public List<Token> Tokenize()
    {
        while (i < src.Length)
        {
            if (char.IsWhiteSpace(Curr)) { Advance(); continue; }
            if (Curr == '/' && Peek() == '/') { SkipLine(); continue; }
            if (char.IsLetter(Curr) || Curr == '_') { ReadIdentifier(); continue; }
            if (char.IsDigit(Curr)) { ReadNumber(); continue; }
            if (Curr == '\'') { ReadString(); continue; }
            ReadSymbol();
        }
        tokens.Add(new Token(TokenType.EOF, "", line, col));
        return tokens;
    }
    private char Peek(int k = 1) => (i + k) < src.Length ? src[i + k] : '\0';
    private void SkipLine() { while (Curr != '\n' && Curr != '\0') Advance(); }

    private void ReadIdentifier()
    {
        int st = i, c0 = col;
        while (char.IsLetterOrDigit(Curr) || Curr == '_') Advance();
        var lex = src[st..i];
        var type = (
            lex == "var" || lex == "function" || lex == "as"     ||
            lex == "do"  || lex == "end"      || lex == "return" ||
            lex == "if"  || lex == "not"      || lex == "else"   ||
            lex == "so"  || lex == "elseif"   || lex == "then"   ||
            lex == "for" || lex == "in"     ||lex == "while" 
        ) ? TokenType.Keyword : TokenType.Identifier;
        tokens.Add(new Token(type, lex, line, c0));
    }
    private void ReadNumber()
    {
        int st = i, c0 = col;
        while (char.IsDigit(Curr)) Advance();
        if (Curr == '.') { Advance(); while (char.IsDigit(Curr)) Advance(); }
        tokens.Add(new Token(TokenType.Number, src[st..i], line, c0));
    }
    private void ReadString()
    {
        int c0 = col; Advance(); int st = i;
        while (Curr != '\'' && Curr != '\0') Advance();
        var str = src[st..i]; Advance();
        tokens.Add(new Token(TokenType.String, str, line, c0));
    }
    private void ReadSymbol()
    {
        int c0 = col; char c = Curr; Advance();
        tokens.Add(new Token(TokenType.Symbol, c.ToString(), line, c0));
    }
}
