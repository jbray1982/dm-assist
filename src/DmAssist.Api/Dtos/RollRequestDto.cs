namespace DmAssist.Api.Dtos;

/// <summary>
/// Wire shape for <c>POST /api/rolls</c>. Deliberately excludes source and visibility — the API
/// stamps <c>Source = Dm</c> / <c>Visibility = Shown</c> itself, so a caller can never claim a
/// privileged attribution through this front door (design decision D1). A future front door
/// (MCP, stat block) gets its own endpoint that stamps its own source; it does not add fields
/// here.
/// </summary>
/// <param name="Expression">Dice expression text, e.g. <c>"4d6kh3"</c>.</param>
/// <param name="Reason">Optional free-text note; null or omitted for a bare roll.</param>
public sealed record RollRequestDto(string Expression, string? Reason);
