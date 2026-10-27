using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Evals.Cook;

namespace LouisAgent.Evals.Puls.Tests;

// F18 step 7.5.4 (ADR 0043): the modes the Puls cook eval runs in are an enum of the eval set's own; a run's protocol, the store and the portal carry their text.
public class PulsModeTests
{
    [Test]
    public void TheModesTheCookEvalRunsIn_TravelUnderTheirRecordedNames() =>
        Assert.That(Enum.GetValues<PulsMode>().Select(m => m.Wire()), Is.EqualTo(new[] { "puls-tools", "puls-cook" }));

    [Test]
    public void TheCookEvalsModes_AreExactlyTheEnum() =>
        Assert.That(CookSetup.Modes.Keys, Is.EquivalentTo(Enum.GetValues<PulsMode>().Select(m => m.Wire())));
}
