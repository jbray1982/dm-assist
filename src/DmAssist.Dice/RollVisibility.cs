namespace DmAssist.Dice;

/// <summary>
/// Whether a roll is visible to the whole table or only to the DM. Only <see cref="Shown"/> is
/// ever produced in the MVP; nothing reads <see cref="Hidden"/> yet, but the field exists so a
/// future player portal does not require a data-model change to arrive.
/// </summary>
public enum RollVisibility
{
    Shown,
    Hidden
}
