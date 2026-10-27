using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Grades.Chips;

namespace LouisAgent.Puls.Tests.Admin.Grades.Chips;

// An outcome's chip class is the view's choice (F18 step 4.5c review): the existing .chip kinds.
public class ChipKindTests
{
    [Test]
    public void TheKindsAreTheExistingChipClasses()
    {
        Assert.That(ChipKind.Of(GradeOutcome.Pass), Is.EqualTo("ok"));
        Assert.That(ChipKind.Of(GradeOutcome.Fail), Is.EqualTo("bad"));
        Assert.That(ChipKind.Of(GradeOutcome.Unknown), Is.EqualTo("warn"));
        Assert.That(ChipKind.Of(GradeOutcome.Pending), Is.Empty,
            "waiting for a person is neither good nor bad nor a warning");
    }
}
