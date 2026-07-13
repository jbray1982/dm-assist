using DmAssist.Dice;

namespace DmAssist.Api;

/// <summary>
/// Owns the session's roll retention & ordering policy — bounded size, newest-first — so no
/// caller re-implements trimming or ordering. Deliberately lives in the API host, not the engine:
/// retention is process/session state on a different change axis than dice-expression semantics,
/// and keeping <c>DmAssist.Dice</c> stateless is what lets a future MCP server wrap it untouched.
/// </summary>
public interface IRollLog
{
    /// <summary>Records a roll, evicting the oldest entry once the log is at capacity.</summary>
    void Add(RollResult result);

    /// <summary>Every retained roll, newest first.</summary>
    IReadOnlyList<RollResult> Snapshot();

    /// <summary>Empties the log. No confirmation, no soft-delete — matches the spec.</summary>
    void Clear();
}
