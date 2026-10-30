using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Shared;

namespace LouisAgent.Evals.Puls.Tests;

/// <summary>
/// The names of Puls's evals are an enum of the set's own (ADR 0043); the portal, the store and a run's protocol carry
/// their text, and the eval's file says it.
/// </summary>
public class PulsEvalNameTests
{
    /// <summary>The cook's file names the eval as the enum does, under the name its stored runs carry.</summary>
    [Test]
    public void TheCooksFile_NamesItAsTheEnumDoes_UnderItsRecordedName()
    {
        Assert.That(CookFile.Eval.Name, Is.EqualTo(PulsEvalName.Cook.Wire()));
        Assert.That(Enum.GetValues<PulsEvalName>().Select(n => n.Wire()), Is.EqualTo(new[] { "puls-cook" }));
    }
}
