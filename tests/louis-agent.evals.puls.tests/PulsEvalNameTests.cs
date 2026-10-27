using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Shared;

namespace LouisAgent.Evals.Puls.Tests;

// F18 step 7.5.4b (ADR 0043): the evals the Puls set offers are an enum of the set's own; the portal, the store and a run's protocol carry their text.
public class PulsEvalNameTests
{
    [Test]
    public void TheEvalsTheSetOffers_AreExactlyTheEnum_UnderTheirRecordedNames()
    {
        Assert.That(new PulsEvalSet().Evals.Select(e => e.Name), Is.EqualTo(Enum.GetValues<PulsEvalName>().Select(n => n.Wire())));
        Assert.That(Enum.GetValues<PulsEvalName>().Select(n => n.Wire()), Is.EqualTo(new[] { "puls-cook", "puls-routing" }));
    }
}
