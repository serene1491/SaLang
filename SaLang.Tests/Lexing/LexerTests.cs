using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using SaLang.Lexing;
using SaLang.Syntax;
namespace SaLang.Tests.Lexing;

public class LexerTests
{
    private static List<Token> Tokenize(string src)
        => new Lexer(src).Tokenize();


    [Fact]
    public void EmptyInput_ProducesOnlyEof()
    {
        var tokens = Tokenize("");
        Assert.Single(tokens);
        Assert.Equal(TokenType.EOF, tokens[0].Type);
    }

    [Theory]
    [InlineData("var foo = 123", new[] {"var", "foo", "=", "123", ""})]
    [InlineData("true false nil", new[] {"true", "false", "nil", ""})]
    [InlineData("_ident123 other_ident", new[] {"_ident123", "other_ident", ""})]
    public void IdentifiersAndKeywords_AreClassifiedCorrectly(string src, string[] expectedLexemes)
    {
        var tokens = Tokenize(src);
        var lexemes = tokens.Select(t => t.Lexeme).ToArray();
        Assert.Equal(expectedLexemes, lexemes);
        for (int i = 0; i < expectedLexemes.Length; i++)
        {
            var lex = expectedLexemes[i];
            var tok = tokens[i];

            if (lex == "")
                Assert.Equal(TokenType.EOF, tok.Type);
            else if (lex == "var" || lex == "function" || lex == "as"     ||
                     lex == "do"  || lex == "end"      || lex == "return" ||
                     lex == "if"  || lex == "not"      || lex == "else"   ||
                     lex == "so"  || lex == "elseif"   || lex == "then"   ||
                     lex == "nil" || lex == "true"     || lex == "false"  ||
                     lex == "for" || lex == "unsafe"   ||lex == "while"   ||
                     lex == "in")
                Assert.Equal(TokenType.Keyword, tok.Type);
            else if (char.IsLetter(lex[0]) || lex[0] == '_')
                Assert.Equal(TokenType.Identifier, tok.Type);
            else if (char.IsDigit(lex[0]))
                Assert.Equal(TokenType.Number, tok.Type);
            else
                Assert.Equal(TokenType.Symbol, tok.Type);
        }
    }

    [Theory]
    [InlineData("123", "123")]
    [InlineData("0.456", "0.456")]
    [InlineData("7890 12.34", new[]{"7890","12.34"})]
    public void Numbers_AreTokenizedCorrectly(string src, object expected)
    {
        var tokens = Tokenize(src);
        var numbers = tokens.Where(t => t.Type == TokenType.Number).Select(t => t.Lexeme).ToArray();
        if (expected is string s)
            Assert.Single(numbers, s);
        else if (expected is string[] arr)
            Assert.Equal(arr, numbers);
        Assert.Equal(TokenType.EOF, tokens.Last().Type);
    }

    [Fact]
    public void StringLiteral_IsTokenizedWithoutQuotes()
    {
        var tokens = Tokenize("'hello world'");
        Assert.Equal(TokenType.String, tokens[0].Type);
        Assert.Equal("hello world", tokens[0].Lexeme);
        Assert.Equal(TokenType.EOF, tokens[1].Type);
    }

    [Theory]
    [InlineData("+")]
    [InlineData("-*/#={},.")] // multiple symbols
    public void Symbols_AreTokenizedAsIndividualTokens(string symbols)
    {
        var tokens = Tokenize(symbols);
        var syms = tokens.Where(t => t.Type == TokenType.Symbol).Select(t => t.Lexeme).ToArray();
        var expected = symbols.Select(c => c.ToString()).ToArray();
        Assert.Equal(expected, syms);
        Assert.Equal(TokenType.EOF, tokens.Last().Type);
    }

    [Fact]
    public void Comments_AreSkipped()
    {
        var code = "var x = 1 // this is a comment\nvar y = 2";
        var tokens = Tokenize(code);
        var lexemes = tokens.Select(t => t.Lexeme).ToArray();
        // expect var,x,=,1,var,y,=,2,""
        var expected = new [] {"var","x","=","1","var","y","=","2",""};
        Assert.Equal(expected, lexemes);
    }
}
