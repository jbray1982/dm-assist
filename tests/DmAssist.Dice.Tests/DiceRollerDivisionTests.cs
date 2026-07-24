using DmAssist.Dice;

namespace DmAssist.Dice.Tests;

/// <summary>Integer division semantics: truncation toward zero, and division-by-zero as an error.</summary>
public class DiceRollerDivisionTests
{
    private static RollRequest Req(string expr) => new(expr, RollSource.Dm, null, RollVisibility.Shown);

    [Fact]
    public void Roll_Division_TruncatesTowardZero_Positive()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var result = roller.Roll(Req("7/2"));

        Assert.Equal(3, result.Total);
    }

    [Fact]
    public void Roll_Division_TruncatesTowardZero_Negative()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        // C# integer division truncates toward zero: -7/2 == -3, not floor's -4.
        var result = roller.Roll(Req("-7/2"));

        Assert.Equal(-3, result.Total);
    }

    [Fact]
    public void Roll_DivisionByZero_Throws()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req("1/0")));

        Assert.Equal(DiceErrorCode.DivisionByZero, ex.Code);
    }

    [Fact]
    public void Roll_DivisionByZero_WhenDivisorIsADiceExpressionThatEvaluatesToZero_Throws()
    {
        // 1d6 rolls 3; 1d4 rolls 4, so (1d4 - 4) evaluates to 0 at evaluation time.
        var roller = new DiceRoller(new ScriptedRandomSource(3, 4));

        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req("1d6/(1d4-4)")));

        Assert.Equal(DiceErrorCode.DivisionByZero, ex.Code);
    }
}
