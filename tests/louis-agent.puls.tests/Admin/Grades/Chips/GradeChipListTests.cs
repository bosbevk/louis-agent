using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Grades.Chips;

namespace LouisAgent.Puls.Tests.Admin.Grades.Chips;

// The chips of a conversation's grades (F18 step 4.5c): in the run's grader order, an unlisted grade last; the kinds from the outcomes,
// a partial score beside the name, the reason as the title, and "no grades" with no kind when there are none. Written first:
// GradeChipList.Of threw NotImplementedException on the red run.
public class GradeChipListTests
{
    private static readonly GraderRow[] Order = [new("no-error", 1, "FixedRule"), new("within-time", 1, "FixedRule"), new("priced", 1, "FixedRule")];

    private static GradeRow Grade(string grader, GradeOutcome outcome, double? score = null, string reason = "because") =>
        new(grader, 1, score ?? (outcome == GradeOutcome.Pass ? 1 : 0), outcome, reason);

    [Test]
    public void TheChipsFollowTheGraderOrder_AndAnUnlistedGradeGoesLast()
    {
        var grades = new ConversationGrades(
            [Grade("extra", GradeOutcome.Pass), Grade("priced", GradeOutcome.Pass), Grade("no-error", GradeOutcome.Pass), Grade("within-time", GradeOutcome.Pass)],
            GradeOutcome.Pass);

        var chips = GradeChipList.Of(grades, Order);

        Assert.That(chips.Select(c => c.Text), Is.EqualTo(new[] { "no-error", "within-time", "priced", "extra" }));
    }

    [Test]
    public void TheKindsAreTheOutcomes_APartialScoreShows_AndTheReasonIsTheTitle()
    {
        var grades = new ConversationGrades(
            [Grade("no-error", GradeOutcome.Pass, reason: "no turn raised an error"), Grade("within-time", GradeOutcome.Unknown, reason: "timed out"),
             Grade("priced", GradeOutcome.Fail, score: 0.6, reason: "3 of 5 priced")],
            GradeOutcome.Fail);

        var chips = GradeChipList.Of(grades, Order);

        Assert.That(chips, Is.EqualTo(new[]
        {
            new GradeChip("no-error", "ok", "no turn raised an error"),
            new GradeChip("within-time", "warn", "timed out"),
            new GradeChip("priced 0.6", "bad", "3 of 5 priced"),
        }));
    }

    // F18 step 6a.1: a grade nobody has given yet is "waiting for review", not unknown (a person has not been asked and failed to say).
    [Test]
    public void APendingGrade_ReadsAsWaitingForReview_WithNoKind()
    {
        var grades = new ConversationGrades([Grade("meal-for-people.human", GradeOutcome.Pending, score: 0, reason: "waits for a person")], GradeOutcome.Pass);

        Assert.That(GradeChipList.Of(grades, Order), Is.EqualTo(new[] { new GradeChip("meal-for-people.human · waiting for review", "", "waits for a person") }));
    }

    [Test]
    public void AScoreOf1Or0OrNone_ShowsOnlyTheName()
    {
        var grades = new ConversationGrades(
            [Grade("no-error", GradeOutcome.Pass, score: 1), Grade("priced", GradeOutcome.Fail, score: 0), new GradeRow("meal", null, null, GradeOutcome.Unknown, "pending")],
            GradeOutcome.Fail);

        Assert.That(GradeChipList.Of(grades, Order).Select(c => c.Text), Is.EqualTo(new[] { "no-error", "priced", "meal" }));
    }

    [Test]
    public void NoGrades_IsOneChipWithNoKind_AndNeverAZero()
    {
        Assert.That(GradeChipList.Of(ConversationGrades.None, Order), Is.EqualTo(new[] { new GradeChip("no grades") }));
        Assert.That(GradeChipList.Of(new ConversationGrades([], GradeOutcome.Unknown), null), Is.EqualTo(new[] { new GradeChip("no grades") }));
    }

    [Test]
    public void WithNoGraderOrder_TheStoredOrderHolds()
    {
        var grades = new ConversationGrades([Grade("priced", GradeOutcome.Pass), Grade("no-error", GradeOutcome.Pass)], GradeOutcome.Pass);

        Assert.That(GradeChipList.Of(grades, null).Select(c => c.Text), Is.EqualTo(new[] { "priced", "no-error" }));
    }
}
