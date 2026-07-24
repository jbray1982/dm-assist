namespace DmAssist.Api.Dtos;

/// <summary>Wire-side mirror of <see cref="DmAssist.Dice.DieRoll"/>.</summary>
public sealed record DieRollDto(int Sides, int Face, bool Kept);
