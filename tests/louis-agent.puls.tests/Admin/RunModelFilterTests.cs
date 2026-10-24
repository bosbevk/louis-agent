using LouisAgent.Puls.Admin;

namespace LouisAgent.Puls.Tests.Admin;

// F18 step 2a: the portal filters runs by the model they ran on. A model a run didn't name ("unknown") is never listed as a
// model, and a run that named none shows only when no model is picked.
public class RunModelFilterTests
{
    private static readonly string[] Haiku = ["claude-haiku-5-5"];
    private static readonly string[] Sonnet = ["claude-sonnet-5-5", "claude-haiku-5-5"];
    private static readonly string[] Unknown = ["unknown"];

    [Test]
    public void TheModelsListed_AreTheNamedOnes_DistinctAndInOrder() =>
        Assert.That(RunModelFilter.ModelsOf([Haiku, null, Unknown, Sonnet]), Is.EqualTo(new[] { "claude-haiku-5-5", "claude-sonnet-5-5" }));

    [Test]
    public void APickedModel_ShowsTheRunsThatNamedIt_AndNoOthers()
    {
        Assert.That(RunModelFilter.Matches(Haiku, "claude-haiku-5-5"), Is.True);
        Assert.That(RunModelFilter.Matches(Sonnet, "claude-haiku-5-5"), Is.True, "a run with the model in any of its modes");
        Assert.That(RunModelFilter.Matches(Sonnet, "claude-sonnet-5-5"), Is.True);
        Assert.That(RunModelFilter.Matches(Haiku, "claude-sonnet-5-5"), Is.False);
    }

    [Test]
    public void WithNoModelPicked_EveryRunShows_AndAnUnknownRunOnlyThen() =>
        Assert.That(new[] { RunModelFilter.Matches(Unknown, null), RunModelFilter.Matches(null, null) }, Is.EqualTo(new[] { true, true }));

    [Test]
    public void AnUnknownRun_IsNotUnderAnyModel() =>
        Assert.That(RunModelFilter.Matches(Unknown, "claude-haiku-5-5"), Is.False);
}
