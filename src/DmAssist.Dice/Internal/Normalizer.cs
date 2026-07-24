namespace DmAssist.Dice.Internal;

/// <summary>
/// Renders an AST back into the engine's one canonical textual form: lowercase <c>d</c>, no
/// whitespace, explicit dice count and explicit selector count (never omitted, even if the
/// source text omitted them), parentheses preserved exactly as parsed. This is the only place
/// "normalized form" is defined — <see cref="RollResult.Expression"/> is always this function's
/// output, never the caller's raw text.
/// </summary>
internal static class Normalizer
{
    public static string Render(AstNode node)
    {
        return RenderNode(node);
    }

    private static string RenderNode(AstNode node)
    {
        return node switch
        {
            NumberNode n => n.Value.ToString(),
            DiceNode d => RenderDice(d),
            BinaryNode b => RenderBinary(b),
            NegateNode n => $"-{RenderNode(n.Operand)}",
            _ => throw new InvalidOperationException($"Unknown node type: {node.GetType()}")
        };
    }

    private static string RenderDice(DiceNode dice)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(dice.Count);
        sb.Append('d');
        sb.Append(dice.Sides);

        if (dice.Selector.HasValue)
        {
            var selector = dice.Selector.Value switch
            {
                SelectorKind.KeepHighest => "kh",
                SelectorKind.KeepLowest => "kl",
                SelectorKind.DropHighest => "dh",
                SelectorKind.DropLowest => "dl",
                _ => throw new InvalidOperationException()
            };
            sb.Append(selector);
            sb.Append(dice.SelectorCount ?? 1);
        }

        return sb.ToString();
    }

    private static string RenderBinary(BinaryNode binary)
    {
        var op = binary.Operator switch
        {
            BinaryOperator.Add => "+",
            BinaryOperator.Subtract => "-",
            BinaryOperator.Multiply => "*",
            BinaryOperator.Divide => "/",
            _ => throw new InvalidOperationException()
        };

        // Need parentheses around binary operations to preserve structure
        // For left operand: needs parens if it's a lower-precedence binary op
        string left = RenderNode(binary.Left);
        if (NeedsParens(binary.Left, binary.Operator, isRight: false))
            left = $"({left})";

        // For right operand: needs parens if it's lower precedence, or same precedence on right side of -, /
        string right = RenderNode(binary.Right);
        if (NeedsParens(binary.Right, binary.Operator, isRight: true))
            right = $"({right})";

        return $"{left}{op}{right}";
    }

    private static bool NeedsParens(AstNode child, BinaryOperator parentOp, bool isRight)
    {
        if (child is not BinaryNode childBinary)
            return false;

        int childPrec = GetPrecedence(childBinary.Operator);
        int parentPrec = GetPrecedence(parentOp);

        if (childPrec < parentPrec)
            return true;

        // For same precedence: need parens on right side of - and / (left associativity)
        if (childPrec == parentPrec && isRight && (parentOp == BinaryOperator.Subtract || parentOp == BinaryOperator.Divide))
            return true;

        return false;
    }

    private static int GetPrecedence(BinaryOperator op)
    {
        return op switch
        {
            BinaryOperator.Add or BinaryOperator.Subtract => 1,
            BinaryOperator.Multiply or BinaryOperator.Divide => 2,
            _ => throw new InvalidOperationException()
        };
    }
}
