using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Grades.Chips;

namespace LouisAgent.Puls.Tests.Admin.Grades.Chips;

// The chips of a conversation's grades (F18 step 4.5c): in the run's grader order, an unlisted grade last; the kinds from the outcomes,
// a partial score beside the name, the reason as the title, and "no grades" with no kind when there are none. Written first:
// GradeChipList.Of threw NotImplementedException on the red run.
public class GradeChipListTests
{
    private static readonly GraderRow[] Order =
        [new("no-error", 1, "FixedRule"), new("within-time", 1, "FixedRule"), new("priced", 1, "FixedRule")];

    private static GradeRow Grade(string grader, GradeOutcome outcome, double? score = null,
        string reason = "because") =>
        new(grader, 1, score ?? (outcome == GradeOutcome.Pass ? 1 : 0), outcome, reason);

    [Test]
    public void TheChipsFollowTheGraderOrder_AndAnUnlistedGradeGoesLast()
    {
        var grades = new ConversationGrades(
            [
                Grade("extra", GradeOutcome.Pass), Grade("priced", GradeOutcome.Pass),
                Grade("no-error", GradeOutcome.Pass), Grade("within-time", GradeOutcome.Pass)
            ],
            GradeOutcome.Pass);

        var chips = GradeChipList.Of(grades, Order);

        Assert.That(chips.Select(c => c.Text), Is.EqualTo(new[] { "no-error", "within-time", "priced", "extra" }));
    }

    [Test]
    public void TheKindsAreTheOutcomes_APartialScoreShows_AndTheReasonIsTheTitle()
    {
        var grades = new ConversationGrades(
            [
                Grade("no-error", GradeOutcome.Pass, reason: "no turn raised an error"),
                Grade("within-time", GradeOutcome.Unknown, reason: "timed out"),
                Grade("priced", GradeOutcome.Fail, score: 0.6, reason: "3 of 5 priced")
            ],
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
        var grades =
            new ConversationGrades(
                [Grade("meal-for-people.human", GradeOutcome.Pending, score: 0, reason: "waits for a person")],
                GradeOutcome.Pass);

        Assert.That(GradeChipList.Of(grades, Order),
            Is.EqualTo(new[]
                { new GradeChip("meal-for-people.human · waiting for review", "", "waits for a person") }));
    }

    // 6a.4e (Louis, on a card that said "reviewed" beside a chip that said "waiting for review"): the review grader's chip shows this reviewer's own verdict once they have graded (the
    // run's line only ever says "waiting", since the grade lives in the API's store); another reviewer's grade keeps it "waiting", so nobody is anchored (blind review).
    [Test]
    public void TheReviewChip_ShowsThisReviewersVerdict_AndStaysWaitingOnAnotherReviewers()
    {
        var mine = new HumanGradeRow("meal-for-people.human", "meal-for-people", "pass", "feeds four", "louis",
            DateTimeOffset.UnixEpoch);
        var later = mine with { Verdict = "unsure", Note = "", At = DateTimeOffset.UnixEpoch.AddHours(1) };
        var anna = mine with { Reviewer = "Anna" };

        ConversationGrades With(params HumanGradeRow[] humans) =>
            new([Grade("meal-for-people.human", GradeOutcome.Pending, score: 0, reason: "waits for a person")],
                GradeOutcome.Pass, humans);

        Assert.That(GradeChipList.Of(With(mine), Order, reviewer: "Louis").Single(),
            Is.EqualTo(new GradeChip("meal-for-people.human · pass (Louis)", "ok", "feeds four")));
        Assert.That(GradeChipList.Of(With(mine, later), Order, reviewer: "Louis").Single().Text,
            Is.EqualTo("meal-for-people.human · unsure (Louis)"), "the latest one");
        Assert.That(GradeChipList.Of(With(mine with { Verdict = "fail" }), Order, reviewer: "Louis").Single().Kind,
            Is.EqualTo("bad"));
        Assert.That(GradeChipList.Of(With(anna), Order, reviewer: "Louis").Single().Text,
            Is.EqualTo("meal-for-people.human · waiting for review"), "Anna's verdict stays out of sight");
        Assert.That(GradeChipList.Of(With(mine), Order, reviewer: null).Single().Text,
            Is.EqualTo("meal-for-people.human · waiting for review"));
    }

    [Test]
    public void AScoreOf1Or0OrNone_ShowsOnlyTheName()
    {
        var grades = new ConversationGrades(
            [
                Grade("no-error", GradeOutcome.Pass, score: 1), Grade("priced", GradeOutcome.Fail, score: 0),
                new GradeRow("meal", null, null, GradeOutcome.Unknown, "pending")
            ],
            GradeOutcome.Fail);

        Assert.That(GradeChipList.Of(grades, Order).Select(c => c.Text),
            Is.EqualTo(new[] { "no-error", "priced", "meal" }));
    }

    [Test]
    public void NoGrades_IsOneChipWithNoKind_AndNeverAZero()
    {
        Assert.That(GradeChipList.Of(ConversationGrades.None, Order), Is.EqualTo(new[] { new GradeChip("no grades") }));
        Assert.That(GradeChipList.Of(new ConversationGrades([], GradeOutcome.Unknown), null),
            Is.EqualTo(new[] { new GradeChip("no grades") }));
    }

    [Test]
    public void WithNoGraderOrder_TheStoredOrderHolds()
    {
        var grades = new ConversationGrades([Grade("priced", GradeOutcome.Pass), Grade("no-error", GradeOutcome.Pass)],
            GradeOutcome.Pass);

        Assert.That(GradeChipList.Of(grades, null).Select(c => c.Text), Is.EqualTo(new[] { "priced", "no-error" }));
    }
}
