using DmAssist.Dice;

namespace DmAssist.Dice.Tests;

/// <summary>
/// A dice engine that cannot be driven deterministically cannot be trusted. These tests prove
/// determinism through the public seam (constructor-injected <see cref="IRandomSource"/>), not
/// through internals.
/// </summary>
public class DiceRollerDeterminismTests
{
    private static RollRequest Req(string expr) => new(expr, RollSource.Dm, null, RollVisibility.Shown);

    [Fact]
    public void Roll_WithIdenticallyScriptedSource_ProducesIdenticalResults()
    {
        var first = new DiceRoller(new ScriptedRandomSource(6, 5, 3, 1)).Roll(Req("4d6kh3"));
        var second = new DiceRoller(new ScriptedRandomSource(6, 5, 3, 1)).Roll(Req("4d6kh3"));

        Assert.Equal(first.Total, second.Total);
        Assert.Equal(first.Dice, second.Dice);
        Assert.Equal(first.Expression, second.Expression);
    }

    [Fact]
    public void Roll_DoesNotMutateStateBetweenCalls()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(4, 4));

        var first = roller.Roll(Req("1d6"));
        var second = roller.Roll(Req("1d6"));

        Assert.Equal(first.Total, second.Total);
    }
}
