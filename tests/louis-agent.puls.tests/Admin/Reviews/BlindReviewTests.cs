using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Grades.Chips;
using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Tests.Admin.Reviews;

// F18 step 6a.3 (Louis's pick: "hide the judge's chip too"): a review is blind only if the judge's verdict is hidden wherever it shows, not just in the review panel. For a judge that
// has a human review, its chip shows only its name until the reviewer has graded that conversation (or when no reviewer name is set, since then nobody has); a judge with no human review,
// the fixed rules and the human review's own chip are never hidden. Written first: BlindReview did not exist, and GradeChipList had no way to hide a chip.
public class BlindReviewTests
{
    private static readonly GraderRow[] Order =
    [
        new("no-error", 1, "FixedRule"), new("meal-for-people", 1, "Model"), new("meal-for-people.human", 1, "Human", "Is this a meal?"), new("another-judge", 1, "Model"),
    ];

    private static GradeRow Grade(string grader, GradeOutcome outcome, string reason = "because") => new(grader, 1, outcome == GradeOutcome.Pass ? 1 : 0, outcome, reason);

    private static HumanGradeRow Human(string reviewer, string verdict = "pass") =>
        new("meal-for-people.human", "meal-for-people", verdict, "", reviewer, DateTimeOffset.UnixEpoch);

    private static ConversationGrades Grades(params HumanGradeRow[] humans) =>
        new([Grade("no-error", GradeOutcome.Pass), Grade("meal-for-people", GradeOutcome.Pass, "feeds four"), Grade("another-judge", GradeOutcome.Fail),
             new("meal-for-people.human", 1, 0, GradeOutcome.Pending, "waits")], GradeOutcome.Pass, humans);

    private static IReadOnlySet<string> Hidden(ConversationGrades grades, string? reviewer, bool gradedThisSession = false) =>
        BlindReview.HiddenJudges(grades, Order, reviewer, _ => gradedThisSession);

    [Test]
    public void AJudgeWithAHumanReview_IsHidden_UntilTheReviewerHasGradedTheConversation()
    {
        Assert.That(Hidden(Grades(), "Louis"), Is.EquivalentTo(new[] { "meal-for-people" }));
        Assert.That(Hidden(Grades(Human("Louis")), "Louis"), Is.Empty, "graded: the judge shows");
    }

    [Test]
    public void WithNoReviewerName_TheJudgeStaysHidden_NobodyHasGradedAnything()
    {
        Assert.That(Hidden(Grades(Human("Louis")), null), Is.EquivalentTo(new[] { "meal-for-people" }));
        Assert.That(Hidden(Grades(Human("Louis")), "  "), Is.EquivalentTo(new[] { "meal-for-people" }));
    }

    [Test]
    public void AnotherPersonsGrade_DoesNotUnhideTheJudgeForThisReviewer()
    {
        Assert.That(Hidden(Grades(Human("Anna")), "Louis"), Is.EquivalentTo(new[] { "meal-for-people" }));
    }

    [Test]
    public void TheReviewersNameIsTheSameWhateverItsCase()
    {
        Assert.That(Hidden(Grades(Human("Louis")), "louis"), Is.Empty);
    }

    [Test]
    public void AGradeGivenThisSession_UnhidesTheJudgeAtOnce_BeforeAnyReload()
    {
        Assert.That(Hidden(Grades(), "Louis", gradedThisSession: true), Is.Empty);
    }

    [Test]
    public void ARunWithNoHumanReview_HidesNothing()
    {
        var noReview = Order.Where(g => g.Kind != "Human").ToArray();

        Assert.That(BlindReview.HiddenJudges(Grades(), noReview, null, _ => false), Is.Empty);
        Assert.That(BlindReview.HiddenJudges(Grades(), null, null, _ => false), Is.Empty, "a run from before the graders were recorded");
    }

    [Test]
    public void AHiddenJudgesChip_ShowsOnlyItsName_NoColourAndNoReason()
    {
        var chips = GradeChipList.Of(Grades(), Order, Hidden(Grades(), "Louis"));

        Assert.That(chips.Single(c => c.Text == "meal-for-people"), Is.EqualTo(new GradeChip("meal-for-people", "", null)));
        Assert.That(chips.Single(c => c.Text == "no-error").Kind, Is.EqualTo("ok"), "a fixed rule is never hidden");
        Assert.That(chips.Single(c => c.Text == "another-judge").Kind, Is.EqualTo("bad"), "nor a judge nobody reviews");
        Assert.That(chips.Single(c => c.Text.StartsWith("meal-for-people.human")).Text, Does.Contain("waiting for review"));
        Assert.That(string.Join(" ", chips.Select(c => c.Title)), Does.Not.Contain("feeds four"), "the judge's reason is nowhere in the chips");
    }

    [Test]
    public void AfterTheReviewerGrades_TheJudgesChipIsWhatItWas()
    {
        var chips = GradeChipList.Of(Grades(Human("Louis")), Order, Hidden(Grades(Human("Louis")), "Louis"));

        Assert.That(chips.Single(c => c.Text == "meal-for-people"), Is.EqualTo(new GradeChip("meal-for-people", "ok", "feeds four")));
    }

    [TestCase("pass", GradeOutcome.Pass, ReviewVerdictKind.Agreed)]
    [TestCase("fail", GradeOutcome.Fail, ReviewVerdictKind.Agreed)]
    [TestCase("pass", GradeOutcome.Fail, ReviewVerdictKind.Disagreed)]
    [TestCase("fail", GradeOutcome.Pass, ReviewVerdictKind.Disagreed)]
    [TestCase("unsure", GradeOutcome.Pass, ReviewVerdictKind.YouWereUnsure)]
    [TestCase("pass", GradeOutcome.Unknown, ReviewVerdictKind.JudgeCouldNotTell)]
    [TestCase("fail", GradeOutcome.Pending, ReviewVerdictKind.JudgeCouldNotTell)]
    public void WhatTheReviewerSees_AfterGrading_IsWhetherTheyAgreedWithTheJudge(string person, GradeOutcome judge, ReviewVerdictKind expected)
    {
        Assert.That(BlindReview.Compare(person, judge), Is.EqualTo(expected));
    }
}
