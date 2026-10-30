using LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading;

namespace LouisAgent.Composition.Tests;

/// <summary>
/// Every identity's own checks reach the runner through the composition, so the runner gathers them without naming an
/// identity.
/// </summary>
public class EvalChecksTests
{
    /// <summary>The list holds Puls's fixed rules, in Puls's order.</summary>
    [Test]
    public void TheList_HoldsPulssFixedRules() =>
        Assert.That(EvalChecks.All.Select(entry => entry.Type), Is.EqualTo(PulsChecks.All.Select(entry => entry.Type)));
}
