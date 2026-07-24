using DmAssist.Dice;

namespace DmAssist.Dice.Tests;

/// <summary>Keep/drop selectors, including the advantage/disadvantage special cases.</summary>
public class DiceRollerKeepDropTests
{
    private static RollRequest Req(string expr) => new(expr, RollSource.Dm, null, RollVisibility.Shown);

    [Fact]
    public void Roll_KeepHighest_MarksExactlyRequestedCountKept()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(6, 5, 3, 1));

        var result = roller.Roll(Req("4d6kh3"));

        Assert.Equal(4, result.Dice.Count);
        Assert.Equal(3, result.Dice.Count(d => d.Kept));
        Assert.Equal(14, result.Total); // 6 + 5 + 3, dropping the 1
        Assert.False(result.Dice[3].Kept);
        Assert.Equal(1, result.Dice[3].Face);
    }

    [Fact]
    public void Roll_KeepHighest_PreservesRollOrderIncludingDropped()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(6, 5, 3, 1));

        var result = roller.Roll(Req("4d6kh3"));

        Assert.Equal(new[] { 6, 5, 3, 1 }, result.Dice.Select(d => d.Face));
    }

    [Fact]
    public void Roll_KeepLowest_KeepsTheLowestDice()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(5, 2, 6));

        var result = roller.Roll(Req("3d6kl2"));

        Assert.Equal(7, result.Total); // 2 + 5, dropping the 6
    }

    [Fact]
    public void Roll_DropHighest_DropsTheHighestDice()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(6, 2, 4));

        var result = roller.Roll(Req("3d6dh1"));

        Assert.Equal(6, result.Total); // 2 + 4, dropping the 6
    }

    [Fact]
    public void Roll_DropLowest_DropsTheLowestDice()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(6, 2, 4));

        var result = roller.Roll(Req("3d6dl1"));

        Assert.Equal(10, result.Total); // 6 + 4, dropping the 2
    }

    [Fact]
    public void Roll_Advantage_KeepsTheHigherD20()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(15, 8));

        var result = roller.Roll(Req("2d20kh1"));

        Assert.Equal(15, result.Total);
        Assert.True(result.Dice[0].Kept);
        Assert.False(result.Dice[1].Kept);
    }

    [Fact]
    public void Roll_Disadvantage_KeepsTheLowerD20()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(15, 8));

        var result = roller.Roll(Req("2d20kl1"));

        Assert.Equal(8, result.Total);
        Assert.False(result.Dice[0].Kept);
        Assert.True(result.Dice[1].Kept);
    }

    [Fact]
    public void Roll_SelectorWithOmittedCount_DefaultsToOne()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(12, 19));

        var result = roller.Roll(Req("2d20kh"));

        Assert.Equal(19, result.Total);
        Assert.Equal("2d20kh1", result.Expression);
    }
}
