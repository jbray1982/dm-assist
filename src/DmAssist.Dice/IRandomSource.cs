namespace DmAssist.Dice;

/// <summary>
/// Hides how random die faces are produced. The engine never touches an RNG directly, so a
/// deterministic/scripted source can be substituted in tests, and a CSPRNG can be substituted in
/// production, without either change reaching the evaluator.
/// </summary>
public interface IRandomSource
{
    /// <summary>
    /// Returns a uniformly distributed face in the inclusive range <c>[1, sides]</c>.
    /// </summary>
    /// <param name="sides">Number of sides on the die; always &gt;= 1 when called by the engine.</param>
    int NextFace(int sides);
}
