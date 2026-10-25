using LouisAgent.Puls.Admin.Grades;

namespace LouisAgent.Puls.Tests.Admin.Grades;

// The outcome parsed once where it enters (ADR 0024): the case ignored, a word it doesn't know Unknown, never a pass. Written first:
// Parse and KindOf threw NotImplementedException on the red run.
public class GradeOutcomesTests
{
    [TestCase("Pass", GradeOutcome.Pass)]
    [TestCase("pass", GradeOutcome.Pass)]
    [TestCase("FAIL", GradeOutcome.Fail)]
    [TestCase("unknown", GradeOutcome.Unknown)]
    public void TheCaseIsIgnored(string text, GradeOutcome expected) => Assert.That(GradeOutcomes.Parse(text), Is.EqualTo(expected));

    [TestCase("Passed")]
    [TestCase("0")]
    [TestCase("")]
    [TestCase(null)]
    public void AWordItDoesNotKnow_IsUnknown(string? text) => Assert.That(GradeOutcomes.Parse(text), Is.EqualTo(GradeOutcome.Unknown));

    [Test]
    public void TheKindsAreTheExistingChipClasses()
    {
        Assert.That(GradeOutcomes.KindOf(GradeOutcome.Pass), Is.EqualTo("ok"));
        Assert.That(GradeOutcomes.KindOf(GradeOutcome.Fail), Is.EqualTo("bad"));
        Assert.That(GradeOutcomes.KindOf(GradeOutcome.Unknown), Is.EqualTo("warn"));
    }
}
