namespace DmAssist.Api.Dtos;

/// <summary>
/// Wire-side mirror of <see cref="DmAssist.Dice.RollSource"/>. Kept as a distinct type (rather
/// than serializing the engine enum directly) so the wire contract's casing/shape can change
/// independently of the engine's vocabulary — engine types never serialize directly.
/// </summary>
public enum RollSourceDto { Dm, StatBlock, Llm }

/// <summary>Wire-side mirror of <see cref="DmAssist.Dice.RollVisibility"/>.</summary>
public enum RollVisibilityDto { Shown, Hidden }
