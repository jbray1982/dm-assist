using DmAssist.Dice;

namespace DmAssist.Api;

/// <summary>
/// Thread-safe, process-lifetime implementation of <see cref="IRollLog"/>. Registered as a
/// singleton — the log is the session (per spec); it does not survive a process restart, and
/// there is exactly one log for the whole process, not one per request.
/// </summary>
public sealed class InMemoryRollLog : IRollLog
{
    /// <summary>Maximum number of rolls retained; the oldest falls off beyond this.</summary>
    public const int Capacity = 100;

    private readonly object _gate = new();
    private readonly LinkedList<RollResult> _rolls = new();

    public void Add(RollResult result)
    {
        lock (_gate)
        {
            _rolls.AddFirst(result);
            while (_rolls.Count > Capacity)
                _rolls.RemoveLast();
        }
    }

    public IReadOnlyList<RollResult> Snapshot()
    {
        lock (_gate)
        {
            return _rolls.ToList();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _rolls.Clear();
        }
    }
}
