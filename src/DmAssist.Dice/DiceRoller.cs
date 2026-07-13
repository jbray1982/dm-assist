using DmAssist.Dice.Internal;

namespace DmAssist.Dice;

/// <summary>
/// The engine's entire public entry point. Hides tokenization, parsing, the AST, evaluation, and
/// normalization behind one method: give it a request, get back a result or a
/// <see cref="DiceExpressionException"/>. Holds no mutable state beyond its injected
/// collaborators, so a single instance can be shared (e.g. as a DI singleton) across concurrent
/// callers.
/// </summary>
public sealed class DiceRoller
{
    /// <summary>Maximum number of dice a single expression may roll.</summary>
    public const int MaxDice = 1000;

    /// <summary>Maximum number of sides a single die may have.</summary>
    public const int MaxSides = 1000;

    private readonly IRandomSource _random;
    private readonly TimeProvider _clock;

    /// <param name="random">Source of die faces. Defaults to <see cref="SystemRandomSource"/>.</param>
    /// <param name="clock">Source of <see cref="RollResult.RolledAt"/>. Defaults to <see cref="TimeProvider.System"/>.</param>
    public DiceRoller(IRandomSource? random = null, TimeProvider? clock = null)
    {
        _random = random ?? new SystemRandomSource();
        _clock = clock ?? TimeProvider.System;
    }

    /// <summary>
    /// Evaluates <paramref name="request"/>'s expression and returns the roll it produced.
    /// Nothing is rolled and nothing can be logged if this throws — validation (syntax, dice/side
    /// counts, selector size, size caps) happens before any die is rolled, so the only failure
    /// that can occur mid-evaluation is division by zero.
    /// </summary>
    /// <exception cref="DiceExpressionException">
    /// The expression is malformed, violates a semantic rule (see <see cref="DiceErrorCode"/>),
    /// or divides by zero during evaluation. This is the only failure mode.
    /// </exception>
    public RollResult Roll(RollRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Expression))
            throw new DiceExpressionException(DiceErrorCode.InvalidSyntax, "Expression is required.");

        var tokenizer = new Tokenizer();
        var tokens = tokenizer.Tokenize(request.Expression);

        var parser = new Parser();
        var ast = parser.Parse(tokens);

        var evaluator = new Evaluator(_random);
        var evalResult = evaluator.Evaluate(ast);

        var normalizedExpression = Normalizer.Render(ast);

        return new RollResult(
            Id: Guid.NewGuid(),
            Expression: normalizedExpression,
            Dice: evalResult.Dice,
            Total: evalResult.Total,
            Source: request.Source,
            Reason: request.Reason,
            Visibility: request.Visibility,
            RolledAt: _clock.GetUtcNow()
        );
    }
}
