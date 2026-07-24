namespace DmAssist.Dice;

/// <summary>
/// A request to evaluate one dice expression. <paramref name="Expression"/> is raw, caller-supplied
/// text in the engine's grammar (see the module's internal tokenizer/parser); it need not be
/// pre-normalized. <paramref name="Source"/> and <paramref name="Visibility"/> are asserted by the
/// caller's front door, not by the player — callers such as <c>DmAssist.Api</c> are expected to
/// stamp these themselves rather than let an end user choose them.
/// </summary>
/// <param name="Expression">Dice expression text, e.g. <c>"4d6kh3"</c> or <c>"d20+5"</c>.</param>
/// <param name="Source">Which front door is making this roll.</param>
/// <param name="Reason">Optional free-text note ("why" this roll happened); null for a bare roll.</param>
/// <param name="Visibility">Whether the roll is shown to the table or hidden.</param>
public sealed record RollRequest(
    string Expression,
    RollSource Source,
    string? Reason,
    RollVisibility Visibility);
