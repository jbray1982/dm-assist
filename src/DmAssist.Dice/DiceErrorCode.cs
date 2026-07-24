namespace DmAssist.Dice;

/// <summary>
/// Every distinct way a dice expression can fail to produce a roll. Named per-case (rather than
/// a single generic error) so callers — in particular the API's ProblemDetails mapping — can
/// render a specific machine-readable <c>code</c> without parsing message text.
/// </summary>
public enum DiceErrorCode
{
    /// <summary>The expression could not be parsed. <see cref="DiceExpressionException.Position"/> is set.</summary>
    InvalidSyntax,

    /// <summary>A dice term specified zero or fewer dice, e.g. <c>0d6</c>.</summary>
    NonPositiveDiceCount,

    /// <summary>A dice term specified zero or fewer sides, e.g. <c>2d0</c>.</summary>
    NonPositiveSides,

    /// <summary>A keep/drop selector asked for more dice than were rolled, e.g. <c>2d6kh3</c>.</summary>
    SelectorTooLarge,

    /// <summary>A dice term rolled more than <see cref="DiceRoller.MaxDice"/> dice.</summary>
    DiceCapExceeded,

    /// <summary>A dice term used more than <see cref="DiceRoller.MaxSides"/> sides.</summary>
    SidesCapExceeded,

    /// <summary>Evaluation divided by a subexpression that evaluated to zero.</summary>
    DivisionByZero
}
