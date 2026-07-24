using DmAssist.Dice;

namespace DmAssist.Dice.Tests;

/// <summary>
/// Test-only <see cref="IRandomSource"/> that returns a predetermined queue of faces in order,
/// one per call to <see cref="NextFace"/>, regardless of the requested <c>sides</c>. Proves the
/// engine's determinism through its public seam (constructor injection) rather than through
/// internals. Throws if more faces are requested than were scripted — an under-scripted test is a
/// bug in the test, not a reason to silently wrap around.
/// </summary>
public sealed class ScriptedRandomSource : IRandomSource
{
    private readonly Queue<int> _faces;

    public ScriptedRandomSource(params int[] faces)
    {
        _faces = new Queue<int>(faces);
    }

    public int NextFace(int sides)
    {
        if (_faces.Count == 0)
        {
            throw new InvalidOperationException(
                "ScriptedRandomSource exhausted: the expression under test rolled more dice than were scripted.");
        }

        return _faces.Dequeue();
    }
}
