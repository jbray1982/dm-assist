using DmAssist.Dice;

namespace DmAssist.Dice.Tests;

/// <summary>
/// The normalized form is the engine's contract for <see cref="RollResult.Expression"/>:
/// lowercase <c>d</c>, no whitespace, explicit dice/selector counts, parentheses preserved.
/// </summary>
public class DiceRollerNormalizationTests
{
    private static RollRequest Req(string expr) => new(expr, RollSource.Dm, null, RollVisibility.Shown);

    [Fact]
    public void Normalizes_OmittedDiceCount_ToExplicitOne()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(10));

        var result = roller.Roll(Req("d20"));

        Assert.Equal("1d20", result.Expression);
    }

    [Fact]
    public void Normalizes_OmittedSelectorCount_ToExplicitOne()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(12, 19));

        var result = roller.Roll(Req("2d20kh"));

        Assert.Equal("2d20kh1", result.Expression);
    }

    [Fact]
    public void Normalizes_RemovesWhitespace()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(4));

        var result = roller.Roll(Req(" 1d6 + 2 "));

        Assert.Equal("1d6+2", result.Expression);
    }

    [Fact]
    public void Normalizes_UppercaseD_ToLowercase()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(4));

        var result = roller.Roll(Req("1D6"));

        Assert.Equal("1d6", result.Expression);
    }

    [Fact]
    public void Normalizes_PreservesParenthesesAsParsed()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(3));

        var result = roller.Roll(Req("(1d6+2)*3"));

        Assert.Equal("(1d6+2)*3", result.Expression);
    }
}
