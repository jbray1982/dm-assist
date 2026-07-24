namespace DmAssist.Dice.Internal;

/// <summary>
/// Lexical token kinds for the dice-expression grammar. <c>D</c> is case-insensitive in source
/// text (<c>d</c>/<c>D</c>) but always represented by this single kind.
/// </summary>
internal enum TokenKind
{
    Number,
    D,
    KeepHigh,   // "kh"
    KeepLow,    // "kl"
    DropHigh,   // "dh"
    DropLow,    // "dl"
    Plus,
    Minus,
    Star,
    Slash,
    LParen,
    RParen,
    End
}

/// <summary>
/// One lexical token. <see cref="Position"/> is the 0-based index into the original expression
/// text where this token starts — the only place positions are captured, so every downstream
/// error position traces back here.
/// </summary>
internal readonly record struct Token(TokenKind Kind, string Text, int Position);

/// <summary>
/// Converts raw expression text into a flat token stream. Whitespace is insignificant and
/// consumed silently; any character that cannot start a valid token is a syntax error located at
/// its own position.
/// </summary>
internal sealed class Tokenizer
{
    /// <exception cref="DiceExpressionException">
    /// <see cref="DiceErrorCode.InvalidSyntax"/> — an unrecognized character was encountered.
    /// </exception>
    public IReadOnlyList<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        int i = 0;

        while (i < input.Length)
        {
            char c = input[i];

            // Skip whitespace
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            // Numbers
            if (char.IsDigit(c))
            {
                int start = i;
                while (i < input.Length && char.IsDigit(input[i]))
                    i++;
                tokens.Add(new Token(TokenKind.Number, input[start..i], start));
                continue;
            }

            // Single-character tokens
            switch (c)
            {
                case '+':
                    tokens.Add(new Token(TokenKind.Plus, "+", i));
                    i++;
                    continue;
                case '-':
                    tokens.Add(new Token(TokenKind.Minus, "-", i));
                    i++;
                    continue;
                case '*':
                    tokens.Add(new Token(TokenKind.Star, "*", i));
                    i++;
                    continue;
                case '/':
                    tokens.Add(new Token(TokenKind.Slash, "/", i));
                    i++;
                    continue;
                case '(':
                    tokens.Add(new Token(TokenKind.LParen, "(", i));
                    i++;
                    continue;
                case ')':
                    tokens.Add(new Token(TokenKind.RParen, ")", i));
                    i++;
                    continue;
            }

            // Two-character tokens (d, kh, kl, dh, dl)
            if (i + 1 < input.Length)
            {
                string twoChar = input[i..(i + 2)];
                if (twoChar.Equals("kh", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Add(new Token(TokenKind.KeepHigh, twoChar, i));
                    i += 2;
                    continue;
                }
                if (twoChar.Equals("kl", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Add(new Token(TokenKind.KeepLow, twoChar, i));
                    i += 2;
                    continue;
                }
                if (twoChar.Equals("dh", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Add(new Token(TokenKind.DropHigh, twoChar, i));
                    i += 2;
                    continue;
                }
                if (twoChar.Equals("dl", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Add(new Token(TokenKind.DropLow, twoChar, i));
                    i += 2;
                    continue;
                }
            }

            // Single-character 'd' (case-insensitive)
            if (c == 'd' || c == 'D')
            {
                tokens.Add(new Token(TokenKind.D, c.ToString(), i));
                i++;
                continue;
            }

            // Unrecognized character
            throw new DiceExpressionException(DiceErrorCode.InvalidSyntax, "Unrecognized character in expression.", i);
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, input.Length));
        return tokens;
    }
}
