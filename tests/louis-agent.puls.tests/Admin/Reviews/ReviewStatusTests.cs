using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Tests.Admin.Reviews;

// F18 step 6a.4d (Louis: "I need to know which ones to check and which ones not and which ones do not need it"): a conversation's review state for this reviewer. No review grade at all
// (the question doesn't apply, 6a.4a) is NoReview; a Pending review this reviewer hasn't answered is ToReview; one they have (stored under their name, or given this session) is Reviewed.
// Written first: nothing told the list apart.
public class ReviewStatusTests
{
    private static readonly GraderRow[] Order = [new("no-error", 1, "FixedRule"), new("meal-for-people", 1, "Model"), new("meal-for-people.human", 2, "Human", "Is this a meal?")];

    private static ConversationGrades With(bool pending, params HumanGradeRow[] humans) => new(
        pending
            ? [new GradeRow("no-error", 1, 1, GradeOutcome.Pass, "fine"), new GradeRow("meal-for-people", 1, 1, GradeOutcome.Pass, "feeds"), new GradeRow("meal-for-people.human", 2, 0, GradeOutcome.Pending, "waits")]
            : [new GradeRow("no-error", 1, 1, GradeOutcome.Pass, "fine"), new GradeRow("meal-for-people", 1, 1, GradeOutcome.Pass, "not asked")],
        GradeOutcome.Pass, humans);

    private static readonly HumanGradeRow Mine = new("meal-for-people.human", "meal-for-people", "pass", "", "louis", DateTimeOffset.UnixEpoch);

    [Test]
    public void AConversationWithNoReviewGrade_NeedsNoReview() =>
        Assert.That(ReviewStatus.Of(With(pending: false), Order, "Louis", _ => false), Is.EqualTo(ReviewState.NoReview));

    [Test]
    public void APendingReviewThisReviewerHasNotAnswered_IsToReview()
    {
        Assert.That(ReviewStatus.Of(With(pending: true), Order, "Louis", _ => false), Is.EqualTo(ReviewState.ToReview));
        Assert.That(ReviewStatus.Of(With(pending: true, Mine with { Reviewer = "Anna" }), Order, "Louis", _ => false), Is.EqualTo(ReviewState.ToReview), "Anna's grade is not Louis's");
        Assert.That(ReviewStatus.Of(With(pending: true), Order, null, _ => false), Is.EqualTo(ReviewState.ToReview), "no name yet: still to do");
    }

    [Test]
    public void APendingReviewThisReviewerAnswered_IsReviewed_WhateverTheNamesCase_OrGivenThisSession()
    {
        Assert.That(ReviewStatus.Of(With(pending: true, Mine), Order, "Louis", _ => false), Is.EqualTo(ReviewState.Reviewed));
        Assert.That(ReviewStatus.Of(With(pending: true), Order, "Louis", judge => judge == "meal-for-people"), Is.EqualTo(ReviewState.Reviewed));
    }

    [Test]
    public void ARunWithNoReviewGrader_NeedsNoReview() =>
        Assert.That(ReviewStatus.Of(With(pending: true), [new("no-error", 1, "FixedRule")], "Louis", _ => false), Is.EqualTo(ReviewState.NoReview));

    [Test]
    public void EachStateHasItsWords()
    {
        Assert.That(ReviewStatus.Text(ReviewState.ToReview), Is.EqualTo("to review"));
        Assert.That(ReviewStatus.Text(ReviewState.Reviewed), Is.EqualTo("reviewed ✓"));
        Assert.That(ReviewStatus.Text(ReviewState.NoReview), Is.EqualTo("no review"));
    }
}
