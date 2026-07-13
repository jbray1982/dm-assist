namespace DmAssist.Dice;

/// <summary>
/// The engine's single failure channel. Every way a roll can fail to produce a
/// <see cref="RollResult"/> — parse errors and semantic errors alike — surfaces as this one
/// exception type, distinguished by <see cref="Code"/>. Callers can rely on there being no other
/// failure mode from <see cref="DiceRoller.Roll"/>: if it doesn't throw this, it returned a
/// result.
/// </summary>
public sealed class DiceExpressionException : Exception
{
    /// <summary>Which specific failure occurred.</summary>
    public DiceErrorCode Code { get; }

    /// <summary>
    /// 0-based index into the original request expression where the failure is best explained,
    /// or null when the failure cannot be tied to any location (e.g. a missing expression).
    /// Cap violations DO carry a position — the start of the offending number — so a UI caret
    /// can underline it (contract decision from the review of issue #2).
    /// </summary>
    public int? Position { get; }

    public DiceExpressionException(DiceErrorCode code, string message, int? position = null)
        : base(message)
    {
        Code = code;
        Position = position;
    }
}
