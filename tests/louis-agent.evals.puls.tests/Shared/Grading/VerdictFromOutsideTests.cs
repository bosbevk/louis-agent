using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Grading.Common;

namespace LouisAgent.Evals.Puls.Tests.Shared.Grading;

// The grade helpers are public (F18 step 4.4 review): a grader in an identity's own project builds its grades with Verdict, as the
// common graders do. Written first: Verdict was internal, so this project didn't compile on the red run (CS0122).
public class VerdictFromOutsideTests
{
    [Test]
    public void AGraderOutsideTheHarnessBuildsItsGradesWithVerdict()
    {
        Assert.That(Verdict.Pass("grader", 1, "yes").Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(Verdict.Fail("grader", 1, "no").Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(Verdict.Unknown("grader", 1, "unclear").Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }
}
