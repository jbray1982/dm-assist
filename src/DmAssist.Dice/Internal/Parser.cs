namespace DmAssist.Dice.Internal;

/// <summary>
/// Recursive-descent parser for the grammar in <c>docs/dice-roller/001-mvp-spec.md</c>:
/// <code>
/// expression := term (('+' | '-') term)*
/// term       := factor (('*' | '/') factor)*
/// factor     := number | dice | '(' expression ')' | '-' factor
/// dice       := [number] 'd' number [selector]
/// selector   := ('kh' | 'kl' | 'dh' | 'dl') [number]
/// </code>
/// Resolves grammar-level defaults (omitted dice count, omitted selector count) into explicit
/// values on the produced nodes. Does not perform semantic validation (dice/side bounds,
/// selector-vs-count, size caps, division by zero) — that is the evaluator's job, so the parser's
/// only failure mode is a grammar violation.
/// </summary>
internal sealed class Parser
{
    private IReadOnlyList<Token> _tokens = null!;
    private int _current = 0;

    /// <exception cref="DiceExpressionException">
    /// <see cref="DiceErrorCode.InvalidSyntax"/> — the token stream does not match the grammar.
    /// </exception>
    public AstNode Parse(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
        _current = 0;
        var ast = ParseExpression();
        if (_current >= _tokens.Count || _tokens[_current].Kind != TokenKind.End)
            throw new DiceExpressionException(DiceErrorCode.InvalidSyntax, "Unexpected token in expression.", _tokens[_current].Position);
        return ast;
    }

    private AstNode ParseExpression()
    {
        var left = ParseTerm();
        while (_current < _tokens.Count && (_tokens[_current].Kind == TokenKind.Plus || _tokens[_current].Kind == TokenKind.Minus))
        {
            var op = _tokens[_current].Kind == TokenKind.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            int pos = _tokens[_current].Position;
            _current++;
            var right = ParseTerm();
            left = new BinaryNode(op, left, right, pos);
        }
        return left;
    }

    private AstNode ParseTerm()
    {
        var left = ParseFactor();
        while (_current < _tokens.Count && (_tokens[_current].Kind == TokenKind.Star || _tokens[_current].Kind == TokenKind.Slash))
        {
            var op = _tokens[_current].Kind == TokenKind.Star ? BinaryOperator.Multiply : BinaryOperator.Divide;
            int pos = _tokens[_current].Position;
            _current++;
            var right = ParseFactor();
            left = new BinaryNode(op, left, right, pos);
        }
        return left;
    }

    private AstNode ParseFactor()
    {
        if (_current >= _tokens.Count)
            throw new DiceExpressionException(DiceErrorCode.InvalidSyntax, "Unexpected end of expression.", _tokens[_tokens.Count - 1].Position);

        var token = _tokens[_current];

        // Unary negation
        if (token.Kind == TokenKind.Minus)
        {
            int pos = token.Position;
            _current++;
            var operand = ParseFactor();
            return new NegateNode(operand, pos);
        }

        // Parenthesized expression
        if (token.Kind == TokenKind.LParen)
        {
            _current++;
            var expr = ParseExpression();
            if (_current >= _tokens.Count || _tokens[_current].Kind != TokenKind.RParen)
                throw new DiceExpressionException(DiceErrorCode.InvalidSyntax, "Missing closing parenthesis.", _tokens[_current >= _tokens.Count ? _tokens.Count - 1 : _current].Position);
            _current++;
            return expr;
        }

        // Number
        if (token.Kind == TokenKind.Number)
        {
            int pos = token.Position;
            _current++;
            // Look ahead for 'd' first: an overflowing literal is reported as a cap violation
            // when it is a dice count, and as a syntax error when it is a bare number.
            bool isDiceCount = _current < _tokens.Count && _tokens[_current].Kind == TokenKind.D;
            int value = isDiceCount
                ? ParseInt(token, DiceErrorCode.DiceCapExceeded, $"Dice count exceeds the maximum of {DiceRoller.MaxDice}.")
                : ParseInt(token, DiceErrorCode.InvalidSyntax, "Number is too large.");
            if (isDiceCount)
                return ParseDiceWithCount(value, pos);
            return new NumberNode(value, pos);
        }

        // Dice term (omitted count)
        if (token.Kind == TokenKind.D)
            return ParseDiceWithCount(1, token.Position);

        throw new DiceExpressionException(DiceErrorCode.InvalidSyntax, "Expected a number, dice term, expression in parentheses, or negation.", token.Position);
    }

    private AstNode ParseDiceWithCount(int count, int position)
    {
        // We've already seen either 'number' or nothing before the 'd'
        if (_current >= _tokens.Count || _tokens[_current].Kind != TokenKind.D)
            throw new DiceExpressionException(DiceErrorCode.InvalidSyntax, "Expected 'd' in dice term.", _tokens[_current >= _tokens.Count ? _tokens.Count - 1 : _current].Position);

        _current++; // Consume 'd'

        // Now we must see a number for sides
        if (_current >= _tokens.Count || _tokens[_current].Kind != TokenKind.Number)
            throw new DiceExpressionException(DiceErrorCode.InvalidSyntax, "Expected number for dice sides.", _tokens[_current >= _tokens.Count ? _tokens.Count - 1 : _current].Position);

        int sides = ParseInt(_tokens[_current], DiceErrorCode.SidesCapExceeded, $"Dice sides exceed the maximum of {DiceRoller.MaxSides}.");
        _current++;

        // Optional selector
        SelectorKind? selector = null;
        int? selectorCount = null;

        if (_current < _tokens.Count && IsSelector(_tokens[_current].Kind))
        {
            selector = ParseSelector(out selectorCount);
        }

        return new DiceNode(count, sides, selector, selectorCount, position);
    }

    /// <summary>
    /// Parses a Number token's digits, translating overflow (a literal beyond int range — by
    /// definition far past every cap) into the given error code instead of an unhandled
    /// <see cref="OverflowException"/>. The tokenizer guarantees the text is all digits, so
    /// overflow is the only way this can fail.
    /// </summary>
    private static int ParseInt(Token token, DiceErrorCode overflowCode, string overflowMessage) =>
        int.TryParse(token.Text, out int value)
            ? value
            : throw new DiceExpressionException(overflowCode, overflowMessage, token.Position);

    private bool IsSelector(TokenKind kind) =>
        kind == TokenKind.KeepHigh || kind == TokenKind.KeepLow || kind == TokenKind.DropHigh || kind == TokenKind.DropLow;

    private SelectorKind ParseSelector(out int? count)
    {
        var token = _tokens[_current];
        var selectorKind = token.Kind switch
        {
            TokenKind.KeepHigh => SelectorKind.KeepHighest,
            TokenKind.KeepLow => SelectorKind.KeepLowest,
            TokenKind.DropHigh => SelectorKind.DropHighest,
            TokenKind.DropLow => SelectorKind.DropLowest,
            _ => throw new DiceExpressionException(DiceErrorCode.InvalidSyntax, "Expected keep/drop selector.", token.Position)
        };

        _current++;

        // Optional selector count
        if (_current < _tokens.Count && _tokens[_current].Kind == TokenKind.Number)
        {
            count = ParseInt(_tokens[_current], DiceErrorCode.SelectorTooLarge, "Selector count is too large.");
            _current++;
        }
        else
        {
            count = 1; // Default to 1
        }

        return selectorKind;
    }
}
