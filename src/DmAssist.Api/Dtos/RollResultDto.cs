namespace DmAssist.Api.Dtos;

/// <summary>
/// Wire shape returned by all three <c>/api/rolls</c> endpoints. Enums serialize as camelCase
/// strings (configured in <c>Program.cs</c> via <c>JsonStringEnumConverter</c>).
/// </summary>
public sealed record RollResultDto(
    Guid Id,
    string Expression,
    IReadOnlyList<DieRollDto> Dice,
    int Total,
    RollSourceDto Source,
    string? Reason,
    RollVisibilityDto Visibility,
    DateTimeOffset RolledAt);
