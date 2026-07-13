namespace DmAssist.Dice;

/// <summary>
/// The outcome of a single evaluated roll. <see cref="Expression"/> is always the engine's
/// normalized rendering of the request's expression (lowercase, explicit dice/selector counts,
/// no whitespace) — callers never see their raw input echoed back, so two textually different
/// but semantically identical expressions (e.g. <c>"d20"</c> and <c>"1d20"</c>) always produce
/// the same logged form. <see cref="Dice"/> preserves roll order and includes dropped dice.
/// </summary>
/// <param name="Id">Unique identifier for this roll.</param>
/// <param name="Expression">Normalized form of the evaluated expression.</param>
/// <param name="Dice">Every individual die rolled, in roll order, including dropped dice.</param>
/// <param name="Total">Sum of kept dice combined per the expression's arithmetic.</param>
/// <param name="Source">Which front door produced this roll.</param>
/// <param name="Reason">Optional free-text note carried from the request.</param>
/// <param name="Visibility">Whether the roll is shown to the table or hidden.</param>
/// <param name="RolledAt">When the roll was evaluated.</param>
public sealed record RollResult(
    Guid Id,
    string Expression,
    IReadOnlyList<DieRoll> Dice,
    int Total,
    RollSource Source,
    string? Reason,
    RollVisibility Visibility,
    DateTimeOffset RolledAt);
