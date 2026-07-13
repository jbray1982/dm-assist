using DmAssist.Dice;

namespace DmAssist.Api.Dtos;

/// <summary>
/// The one place that turns an engine <see cref="RollResult"/> into its wire DTO. Owns the
/// engine-enum-to-wire-enum mapping so no endpoint has to know both vocabularies exist.
/// </summary>
internal static class RollResultMapper
{
    public static RollResultDto ToDto(this RollResult result)
    {
        var sourceDto = result.Source switch
        {
            RollSource.Dm => RollSourceDto.Dm,
            RollSource.StatBlock => RollSourceDto.StatBlock,
            RollSource.Llm => RollSourceDto.Llm,
            _ => throw new InvalidOperationException($"Unknown RollSource: {result.Source}")
        };

        var visibilityDto = result.Visibility switch
        {
            RollVisibility.Shown => RollVisibilityDto.Shown,
            RollVisibility.Hidden => RollVisibilityDto.Hidden,
            _ => throw new InvalidOperationException($"Unknown RollVisibility: {result.Visibility}")
        };

        var diceDto = result.Dice.Select(d => new DieRollDto(d.Sides, d.Face, d.Kept)).ToList();

        return new RollResultDto(
            result.Id,
            result.Expression,
            diceDto,
            result.Total,
            sourceDto,
            result.Reason,
            visibilityDto,
            result.RolledAt
        );
    }
}
