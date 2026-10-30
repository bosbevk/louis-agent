using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Shared;

namespace LouisAgent.Evals.Puls.Tests;

/// <summary>
/// The evals the Puls set offers in C# are an enum of the set's own (ADR 0043); the portal, the store and a run's
/// protocol carry their text.
/// </summary>
public class PulsEvalNameTests
{
    /// <summary>The set offers exactly the enum's evals, under the names their stored runs carry.</summary>
    [Test]
    public void TheEvalsTheSetOffers_AreExactlyTheEnum_UnderTheirRecordedNames()
    {
        Assert.That(new PulsEvalSet().Evals.Select(e => e.Name),
            Is.EqualTo(Enum.GetValues<PulsEvalName>().Select(n => n.Wire())));
        Assert.That(Enum.GetValues<PulsEvalName>().Select(n => n.Wire()), Is.EqualTo(new[] { "puls-cook" }));
    }
}
