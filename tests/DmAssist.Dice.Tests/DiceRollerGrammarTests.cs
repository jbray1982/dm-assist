using DmAssist.Dice;

namespace DmAssist.Dice.Tests;

/// <summary>Basic grammar coverage: dice terms, arithmetic operators, parentheses.</summary>
public class DiceRollerGrammarTests
{
    private static RollRequest Req(string expr) => new(expr, RollSource.Dm, null, RollVisibility.Shown);

    [Fact]
    public void Roll_SimpleDiceTerm_ReturnsRolledFace()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(4));

        var result = roller.Roll(Req("1d6"));

        Assert.Equal(4, result.Total);
        Assert.Single(result.Dice);
        Assert.Equal(new DieRoll(6, 4, true), result.Dice[0]);
    }

    [Fact]
    public void Roll_OmittedDiceCount_TreatedAsOne()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(15));

        var result = roller.Roll(Req("d20"));

        Assert.Equal(15, result.Total);
        Assert.Single(result.Dice);
    }

    [Fact]
    public void Roll_Addition_AddsModifierToDiceTotal()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(2));

        var result = roller.Roll(Req("1d6+3"));

        Assert.Equal(5, result.Total);
    }

    [Fact]
    public void Roll_Subtraction_SubtractsModifierFromDiceTotal()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(5));

        var result = roller.Roll(Req("1d6-2"));

        Assert.Equal(3, result.Total);
    }

    [Fact]
    public void Roll_Multiplication_WithNoDiceTerms_EvaluatesArithmeticOnly()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var result = roller.Roll(Req("2*3"));

        Assert.Equal(6, result.Total);
        Assert.Empty(result.Dice);
    }

    [Fact]
    public void Roll_Parentheses_ControlEvaluationOrder()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var result = roller.Roll(Req("(1+2)*3"));

        Assert.Equal(9, result.Total);
    }

    [Fact]
    public void Roll_DiceInsideParentheses_EvaluatesBeforeOuterOperator()
    {
        var roller = new DiceRoller(new ScriptedRandomSource(3));

        var result = roller.Roll(Req("(1d6+2)*2"));

        Assert.Equal(10, result.Total);
    }

    [Fact]
    public void Roll_PopulatesAttributionFieldsFromRequest()
    {
        var fixedNow = new DateTimeOffset(2026, 7, 12, 22, 0, 0, TimeSpan.Zero);
        var roller = new DiceRoller(new ScriptedRandomSource(4), new FakeTimeProvider(fixedNow));

        var result = roller.Roll(new RollRequest("1d6", RollSource.Llm, "attack roll", RollVisibility.Hidden));

        Assert.Equal(RollSource.Llm, result.Source);
        Assert.Equal("attack roll", result.Reason);
        Assert.Equal(RollVisibility.Hidden, result.Visibility);
        Assert.Equal(fixedNow, result.RolledAt);
    }
}
