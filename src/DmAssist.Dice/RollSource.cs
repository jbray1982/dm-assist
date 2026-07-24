namespace DmAssist.Dice;

/// <summary>
/// Identifies which front door produced a roll (the DM builder, a stat-block click, or an LLM
/// tool call). Only <see cref="Dm"/> is ever produced in the MVP; the other members exist so the
/// wire shape and downstream auditing do not need to change when those front doors ship.
/// </summary>
public enum RollSource
{
    Dm,
    StatBlock,
    Llm
}
