namespace DmAssist.Dice;

/// <summary>
/// One physical die roll: the die's size, the face it landed on, and whether a keep/drop
/// selector kept it in the total. Dice discarded by a selector (e.g. the lowest of a
/// <c>4d6kh3</c>) still appear here with <see cref="Kept"/> false — the caller needs the full
/// set to render "here's what you rolled, here's what counted" legibly.
/// </summary>
/// <param name="Sides">Number of sides on the die that produced this roll.</param>
/// <param name="Face">The face rolled, 1-indexed.</param>
/// <param name="Kept">Whether this die's face contributed to <see cref="RollResult.Total"/>.</param>
public sealed record DieRoll(int Sides, int Face, bool Kept);
