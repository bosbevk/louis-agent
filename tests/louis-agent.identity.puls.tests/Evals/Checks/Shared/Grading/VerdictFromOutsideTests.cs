using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Shared.Grading;

/// <summary>
/// The grade helpers are public, so a check in an identity's own project builds its grades with <c>Verdict</c>, as the
/// common checks do.
/// </summary>
public class VerdictFromOutsideTests
{
    /// <summary>A check outside the harness can build a pass, a fail and an unknown with <c>Verdict</c>.</summary>
    [Test]
    public void AGraderOutsideTheHarnessBuildsItsGradesWithVerdict()
    {
        Assert.That(Verdict.Pass("grader", 1, "yes").Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(Verdict.Fail("grader", 1, "no").Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(Verdict.Unknown("grader", 1, "unclear").Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }
}
