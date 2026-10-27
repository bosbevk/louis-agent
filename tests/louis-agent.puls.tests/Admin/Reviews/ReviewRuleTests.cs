using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Tests.Admin.Reviews;

// F18 step 6a.3b (the review of 6a.3 to 6a.5): the rules of the review each lived in two or three places. "Has this reviewer answered?" was spelt by BlindReview, ReviewStatus and the review
// panel; ".human" by three classes and a page; the page's lookup of which review to show, and its filter, were inline. Now one place each, in BlindReview and ReviewStatus. Written first.
public class ReviewRuleTests
{
    private static HumanGradeRow Human(string reviewer, string verdict = "pass", string judge = "meal-for-people", int second = 0) =>
        new(judge + ".human", judge, verdict, "", reviewer, DateTimeOffset.UnixEpoch.AddSeconds(second));

    private static ConversationGrades Grades(params HumanGradeRow[] humans) => new(null, null, humans);

    [Test]
    public void AReview_IsAHumanGraderNamedForItsJudge_AndTheJudgeIsTheNameWithoutIt()
    {
        Assert.That(BlindReview.IsReview(new GraderRow("meal-for-people.human", 1, "Human")), Is.True);
        Assert.That(BlindReview.IsReview(new GraderRow("meal-for-people", 1, "Model")), Is.False);
        Assert.That(BlindReview.IsReview(new GraderRow("odd-name", 1, "Human")), Is.False, "a Human grader not named .human is no review");
        Assert.That(BlindReview.IsReview(new GraderRow("x.human", 1, "FixedRule")), Is.False, "the kind counts too");
        Assert.That(BlindReview.JudgeOf("cook-judge.human"), Is.EqualTo("cook-judge"));
        Assert.That(BlindReview.JudgeOf("plain"), Is.EqualTo("plain"), "a name without the suffix is its own judge");
    }

    [Test]
    public void TheReviewersOwnGrades_AreTheOnesStoredUnderTheirName_WhateverItsCase_ForThatJudge()
    {
        var grades = Grades(Human("Louis", second: 1), Human("louis", "fail", second: 5), Human("Anna"), Human("Louis", judge: "another-judge"));

        var own = BlindReview.OwnGrades(grades, "meal-for-people", " LOUIS ");

        Assert.That(own.Select(h => h.Verdict), Is.EquivalentTo(new[] { "pass", "fail" }));
        Assert.That(BlindReview.OwnGrades(grades, "meal-for-people", null), Is.Empty, "no name: nothing is theirs");
        Assert.That(BlindReview.OwnGrades(grades, "meal-for-people", "  "), Is.Empty);
    }

    [Test]
    public void Answered_IsAStoredGradeUnderTheName_OrOneGivenThisSession_NeverWithoutAName()
    {
        Assert.That(BlindReview.Answered(Grades(Human("louis")), "meal-for-people", "Louis", _ => false), Is.True);
        Assert.That(BlindReview.Answered(Grades(Human("Anna")), "meal-for-people", "Louis", _ => false), Is.False);
        Assert.That(BlindReview.Answered(Grades(), "meal-for-people", "Louis", judge => judge == "meal-for-people"), Is.True, "given this session, not yet stored");
        Assert.That(BlindReview.Answered(Grades(Human("Louis")), "meal-for-people", null, _ => true), Is.False, "no name yet: nobody has");
    }

    private static readonly GraderRow Review = new("meal-for-people.human", 2, "Human", "Is this a meal?");

    private static ConversationGrades WithGrades(params GradeRow[] grades) => new(grades, GradeOutcome.Pass);

    private static GradeRow Pending(string grader = "meal-for-people.human") => new(grader, 2, 0, GradeOutcome.Pending, "waits");

    [Test]
    public void TheReviewToShow_IsTheRunsReviewThatHasAPendingGradeHere()
    {
        Assert.That(BlindReview.ReviewOf(null, WithGrades(Pending())), Is.Null, "no graders");
        Assert.That(BlindReview.ReviewOf([new("no-error", 1, "FixedRule"), new("meal-for-people", 1, "Model")], WithGrades(Pending())), Is.Null, "none human");
        Assert.That(BlindReview.ReviewOf([new("odd-name", 1, "Human")], WithGrades(Pending("odd-name"))), Is.Null, "a Human grader not named .human");
        Assert.That(BlindReview.ReviewOf([Review], WithGrades(Pending())), Is.EqualTo(Review), "x.human with a Pending grade here");
        Assert.That(BlindReview.ReviewOf([Review], WithGrades(new GradeRow("no-error", 1, 1, GradeOutcome.Pass, "fine"))), Is.Null, "x.human with no grade here: a control (6a.4a)");
        Assert.That(BlindReview.ReviewOf([Review], ConversationGrades.None), Is.Null);
    }

    [Test]
    public void TheFilter_KeepsATask_WhenItIsOffOrAnyModeHasItToReview()
    {
        Assert.That(ReviewStatus.Keeps(onlyToReview: false, [ReviewState.NoReview]), Is.True);
        Assert.That(ReviewStatus.Keeps(onlyToReview: true, [ReviewState.Reviewed, ReviewState.ToReview]), Is.True);
        Assert.That(ReviewStatus.Keeps(onlyToReview: true, [ReviewState.Reviewed, ReviewState.NoReview]), Is.False);
        Assert.That(ReviewStatus.Keeps(onlyToReview: true, []), Is.False, "a task no mode has");
    }
}
