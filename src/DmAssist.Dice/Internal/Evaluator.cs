namespace DmAssist.Dice.Internal;

/// <summary>
/// Result of evaluating one AST: the running total and every individual die rolled, in roll
/// order, including dice a selector dropped.
/// </summary>
internal sealed record EvaluationResult(int Total, IReadOnlyList<DieRoll> Dice);

/// <summary>
/// Walks an <see cref="AstNode"/> tree and produces its numeric result plus the full record of
/// dice rolled. Owns every semantic rule the grammar itself doesn't encode: non-positive dice
/// count/sides, a selector asking for more dice than were rolled, the <see cref="DiceRoller.MaxDice"/>/
/// <see cref="DiceRoller.MaxSides"/> caps, and division by zero. All of these except division by
/// zero are checked before any die is rolled — the divisor can itself contain dice
/// (<c>1d6/(1d4-2)</c>), so division by zero can only be discovered mid-evaluation. Callers rely
/// on this ordering: a thrown <see cref="DiceExpressionException"/> means no die was rolled
/// (except when the code is <see cref="DiceErrorCode.DivisionByZero"/>, where earlier dice in the
/// same expression may have already been rolled but the throw prevents a result from ever
/// reaching the caller either way).
/// </summary>
internal sealed class Evaluator
{
    private readonly IRandomSource _random;

    public Evaluator(IRandomSource random)
    {
        _random = random;
    }

    /// <exception cref="DiceExpressionException">
    /// Any <see cref="DiceErrorCode"/> other than <see cref="DiceErrorCode.InvalidSyntax"/>.
    /// </exception>
    public EvaluationResult Evaluate(AstNode root)
    {
        var dice = new List<DieRoll>();
        int total = EvaluateNode(root, dice);
        return new EvaluationResult(total, dice);
    }

    private int EvaluateNode(AstNode node, List<DieRoll> collectedDice)
    {
        return node switch
        {
            NumberNode n => n.Value,
            DiceNode d => EvaluateDice(d, collectedDice),
            BinaryNode b => EvaluateBinary(b, collectedDice),
            NegateNode n => -EvaluateNode(n.Operand, collectedDice),
            _ => throw new InvalidOperationException($"Unknown node type: {node.GetType()}")
        };
    }

    private int EvaluateDice(DiceNode dice, List<DieRoll> collectedDice)
    {
        // Semantic checks (before rolling)
        if (dice.Count <= 0)
            throw new DiceExpressionException(DiceErrorCode.NonPositiveDiceCount, "Dice count must be positive.", dice.Position);
        if (dice.Sides <= 0)
            throw new DiceExpressionException(DiceErrorCode.NonPositiveSides, "Die sides must be positive.", dice.Position);
        if (dice.Count > DiceRoller.MaxDice)
            throw new DiceExpressionException(DiceErrorCode.DiceCapExceeded, "Dice count exceeds maximum.", dice.Position);
        if (dice.Sides > DiceRoller.MaxSides)
            throw new DiceExpressionException(DiceErrorCode.SidesCapExceeded, "Die sides exceed maximum.", dice.Position);

        // Check selector validity before rolling
        if (dice.Selector.HasValue)
        {
            var selectorCount = dice.SelectorCount ?? 1;
            if (selectorCount > dice.Count)
                throw new DiceExpressionException(DiceErrorCode.SelectorTooLarge, "Selector keeps or drops more dice than were rolled.", dice.Position);
        }

        // Roll all dice
        var rolls = new List<DieRoll>();
        for (int i = 0; i < dice.Count; i++)
        {
            int face = _random.NextFace(dice.Sides);
            rolls.Add(new DieRoll(dice.Sides, face, true)); // Default to kept
        }

        // Apply selector if present
        if (dice.Selector.HasValue)
        {
            var selectorCount = dice.SelectorCount ?? 1;

            // Determine which indices to mark as dropped
            var keptIndices = DetermineKeptIndices(rolls, dice.Selector.Value, selectorCount);

            // Mark dropped dice
            for (int i = 0; i < rolls.Count; i++)
            {
                rolls[i] = rolls[i] with { Kept = keptIndices.Contains(i) };
            }
        }

        // Add to collected dice and sum kept ones
        collectedDice.AddRange(rolls);
        return rolls.Where(d => d.Kept).Sum(d => d.Face);
    }

    private HashSet<int> DetermineKeptIndices(List<DieRoll> rolls, SelectorKind selector, int selectorCount)
    {
        var indices = Enumerable.Range(0, rolls.Count).ToList();

        var kept = selector switch
        {
            SelectorKind.KeepHighest => indices
                .OrderByDescending(i => rolls[i].Face)
                .ThenBy(i => i) // Stable sort by original order for ties
                .Take(selectorCount)
                .ToHashSet(),
            SelectorKind.KeepLowest => indices
                .OrderBy(i => rolls[i].Face)
                .ThenBy(i => i) // Stable sort by original order for ties
                .Take(selectorCount)
                .ToHashSet(),
            SelectorKind.DropHighest => indices
                .OrderByDescending(i => rolls[i].Face)
                .ThenBy(i => i) // Stable sort by original order for ties
                .Skip(selectorCount)
                .ToHashSet(),
            SelectorKind.DropLowest => indices
                .OrderBy(i => rolls[i].Face)
                .ThenBy(i => i) // Stable sort by original order for ties
                .Skip(selectorCount)
                .ToHashSet(),
            _ => throw new InvalidOperationException()
        };

        return kept;
    }

    private int EvaluateBinary(BinaryNode binary, List<DieRoll> collectedDice)
    {
        int left = EvaluateNode(binary.Left, collectedDice);
        int right = EvaluateNode(binary.Right, collectedDice);

        return binary.Operator switch
        {
            BinaryOperator.Add => left + right,
            BinaryOperator.Subtract => left - right,
            BinaryOperator.Multiply => left * right,
            BinaryOperator.Divide => right == 0
                ? throw new DiceExpressionException(DiceErrorCode.DivisionByZero, "Division by zero.", binary.Position)
                : left / right, // C# int division truncates toward zero, which is what we want
            _ => throw new InvalidOperationException()
        };
    }
}
