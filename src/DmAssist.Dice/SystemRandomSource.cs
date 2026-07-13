namespace DmAssist.Dice;

/// <summary>
/// Default production <see cref="IRandomSource"/>: wraps <see cref="Random.Shared"/>. This is
/// the only place the engine touches the framework RNG; swapping it for a CSPRNG is a one-line
/// DI change, not an engine change.
/// </summary>
public sealed class SystemRandomSource : IRandomSource
{
    public int NextFace(int sides) => Random.Shared.Next(1, sides + 1);
}
