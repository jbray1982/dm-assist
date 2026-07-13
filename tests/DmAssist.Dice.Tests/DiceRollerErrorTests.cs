using DmAssist.Dice;

namespace DmAssist.Dice.Tests;

/// <summary>
/// Every documented error case. All of these must throw before any die is rolled, which is what
/// makes "log nothing on error" true by construction — a thrown exception never reaches a caller
/// with a <see cref="RollResult"/> to log. (The API's own "log stays empty on error" behavior is
/// proven separately in <c>DmAssist.Api.Tests</c>, since the log lives there.)
/// </summary>
public class DiceRollerErrorTests
{
    private static RollRequest Req(string expr) => new(expr, RollSource.Dm, null, RollVisibility.Shown);

    [Fact]
    public void Roll_InvalidSyntax_ThrowsWithPositionOfTheOffendingToken()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        // "1d" is missing the sides count; the failure is at the end of input, position 2.
        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req("1d")));

        Assert.Equal(DiceErrorCode.InvalidSyntax, ex.Code);
        Assert.NotNull(ex.Position);
    }

    [Fact]
    public void Roll_ZeroDiceCount_ThrowsNonPositiveDiceCount()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req("0d6")));

        Assert.Equal(DiceErrorCode.NonPositiveDiceCount, ex.Code);
    }

    [Fact]
    public void Roll_ZeroSides_ThrowsNonPositiveSides()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req("2d0")));

        Assert.Equal(DiceErrorCode.NonPositiveSides, ex.Code);
    }

    [Fact]
    public void Roll_SelectorKeepsMoreDiceThanRolled_ThrowsSelectorTooLarge()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req("2d6kh3")));

        Assert.Equal(DiceErrorCode.SelectorTooLarge, ex.Code);
    }

    [Fact]
    public void Roll_DiceCountExceedsCap_ThrowsDiceCapExceeded()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req($"{DiceRoller.MaxDice + 1}d6")));

        Assert.Equal(DiceErrorCode.DiceCapExceeded, ex.Code);
    }

    [Fact]
    public void Roll_SidesExceedsCap_ThrowsSidesCapExceeded()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req($"1d{DiceRoller.MaxSides + 1}")));

        Assert.Equal(DiceErrorCode.SidesCapExceeded, ex.Code);
    }

    [Fact]
    public void Roll_AtExactlyTheCaps_Succeeds()
    {
        // Boundary check: the cap is inclusive. Uses the real RNG since only dice/face counts matter.
        var roller = new DiceRoller();

        var result = roller.Roll(Req($"{DiceRoller.MaxDice}d{DiceRoller.MaxSides}"));

        Assert.Equal(DiceRoller.MaxDice, result.Dice.Count);
    }

    [Fact]
    public void Roll_DiceCountBeyondIntRange_ThrowsDiceCapExceededNotOverflow()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        // 99999999999 overflows int; it must surface as a cap violation, never an OverflowException.
        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req("99999999999d6")));

        Assert.Equal(DiceErrorCode.DiceCapExceeded, ex.Code);
    }

    [Fact]
    public void Roll_SidesBeyondIntRange_ThrowsSidesCapExceededNotOverflow()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req("2d99999999999")));

        Assert.Equal(DiceErrorCode.SidesCapExceeded, ex.Code);
    }

    [Fact]
    public void Roll_BareNumberBeyondIntRange_ThrowsInvalidSyntaxNotOverflow()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req("1d6+99999999999")));

        Assert.Equal(DiceErrorCode.InvalidSyntax, ex.Code);
        Assert.NotNull(ex.Position);
    }

    [Fact]
    public void Roll_SelectorCountBeyondIntRange_ThrowsSelectorTooLargeNotOverflow()
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req("4d6kh99999999999")));

        Assert.Equal(DiceErrorCode.SelectorTooLarge, ex.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Roll_MissingExpression_ThrowsInvalidSyntax(string? expression)
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        var ex = Assert.Throws<DiceExpressionException>(() => roller.Roll(Req(expression!)));

        Assert.Equal(DiceErrorCode.InvalidSyntax, ex.Code);
    }

    [Theory]
    [InlineData("0d6")]
    [InlineData("2d0")]
    [InlineData("2d6kh3")]
    [InlineData("1/0")]
    [InlineData("not a dice expression")]
    public void Roll_OnAnyError_ThrowsRatherThanReturningAResult(string expression)
    {
        var roller = new DiceRoller(new ScriptedRandomSource());

        Assert.Throws<DiceExpressionException>(() => roller.Roll(Req(expression)));
    }
}
