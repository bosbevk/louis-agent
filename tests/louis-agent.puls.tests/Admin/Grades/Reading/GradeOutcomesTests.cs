using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Grades.Reading;

namespace LouisAgent.Puls.Tests.Admin.Grades.Reading;

// The outcome parsed once where it enters (ADR 0024): the case ignored, a word it doesn't know Unknown, never a pass. Written first:
// Parse threw NotImplementedException on the red run.
public class GradeOutcomesTests
{
    [TestCase("Pass", GradeOutcome.Pass)]
    [TestCase("pass", GradeOutcome.Pass)]
    [TestCase("FAIL", GradeOutcome.Fail)]
    [TestCase("unknown", GradeOutcome.Unknown)]
    [TestCase("Pending", GradeOutcome.Pending)]
    [TestCase("pending", GradeOutcome.Pending)]
    public void TheCaseIsIgnored(string text, GradeOutcome expected) => Assert.That(GradeOutcomes.Parse(text), Is.EqualTo(expected));

    [TestCase("Passed")]
    [TestCase("0")]
    [TestCase("")]
    [TestCase(null)]
    public void AWordItDoesNotKnow_IsUnknown(string? text) => Assert.That(GradeOutcomes.Parse(text), Is.EqualTo(GradeOutcome.Unknown));
}
