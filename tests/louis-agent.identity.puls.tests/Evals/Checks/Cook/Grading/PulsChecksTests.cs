using LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook.Grading;

/// <summary>
/// Puls keeps no fixed rule of its own: its shop rules are the shops pack's, and the rest of the cook's rules are
/// general checks over Puls's readers; its judge is in the judges' own list, which needs the harness.
/// </summary>
public class PulsChecksTests
{
    /// <summary>Puls's list is empty.</summary>
    [Test]
    public void PulssList_IsEmpty() => Assert.That(PulsChecks.All, Is.Empty);
}
