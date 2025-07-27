namespace SaLang.Syntax;

public struct Token {
    public TokenType Type;
    public string Lexeme;
    public int Line;
    public int Column;
    public Token(TokenType type, string lexeme, int line, int col) {
        Type = type; Lexeme = lexeme; Line = line; Column = col;
    }
}
