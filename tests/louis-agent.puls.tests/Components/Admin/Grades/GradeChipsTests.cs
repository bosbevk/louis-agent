using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Components.Admin.Grades;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Grades;

// F18 step 4.5c: a conversation's grades as chips, pinned to GradeChips.grades.html (a pass, a partial fail and an unknown, with the
// outcome first) and GradeChips.none.html (no grades: one chip, no kind). Written first: the goldens weren't there on the red run.
public class GradeChipsTests
{
    private static readonly GraderRow[] Order = [new("no-error", 1, "FixedRule"), new("priced", 1, "FixedRule"), new("meal", 1, "Model")];

    [Test]
    public void ThreeGrades_MatchTheGoldenFile()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));
        var grades = new ConversationGrades(
        [
            new GradeRow("no-error", 1, 1, GradeOutcome.Pass, "no turn raised an error"),
            new GradeRow("priced", 1, 0.6, GradeOutcome.Fail, "3 of 5 priced with a source"),
            new GradeRow("meal", 1, 0, GradeOutcome.Unknown, "the judge could not tell"),
        ], GradeOutcome.Fail);

        var cut = ctx.Render<GradeChips>(p => p.Add(c => c.Grades, grades).Add(c => c.Order, Order));

        Assert.That(cut.FindAll(".chip"), Has.Count.EqualTo(4), "the outcome and three grades");
        Assert.That(cut.FindAll(".chip.bad"), Has.Count.EqualTo(2), "the outcome and the failed grade");
        Snapshots.Match("louis-agent.puls.tests", "Components/Admin/Grades/GradeChips.grades.html", cut.Markup);
    }

    [Test]
    public void NoGrades_MatchTheGoldenFile()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ReviewerState(ctx.JSInterop.JSRuntime));

        var cut = ctx.Render<GradeChips>(p => p.Add(c => c.Grades, ConversationGrades.None));

        Assert.That(cut.FindAll(".chip"), Has.Count.EqualTo(1));
        Assert.That(cut.Find(".chip").ClassList, Is.EqualTo(new[] { "chip" }), "no kind");
        Snapshots.Match("louis-agent.puls.tests", "Components/Admin/Grades/GradeChips.none.html", cut.Markup);
    }
}
