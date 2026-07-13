namespace DmAssist.Dice.Tests;

/// <summary>Test-only <see cref="TimeProvider"/> that always returns a fixed instant.</summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FakeTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow() => _now;
}
