using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Trials;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Human;

// F18 step 6a.1: a human review is a grader that waits for a person. It belongs to one model grader (so the person's grade and the judge's are about the same question), it is named
// `{grader}.human`, it is never required, and it grades every trial Pending: a grade nobody has given yet, never a pass. The person's grade lives in the API's store, not in the transcript.
// Written first: HumanReviewGrader and ReviewQuestion did not exist.
public class HumanReviewGraderTests
{
    private sealed class ModelGrader(string name = "meal-for-people", GraderKind kind = GraderKind.Model) : IGrader
    {
        public string Name => name;
        public int Version => 1;
        public GraderKind Kind => kind;
        public bool Required => false;
        public IReadOnlySet<string> ExpectedKeys { get; } = new HashSet<string>();
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private const string Question = "Is this a meal for the people asked for?";

    [Test]
    public void ItIsNamedForTheModelGraderItReviews_AHumanOne_ThatIsNeverRequired()
    {
        var grader = HumanReviewGrader.For(new ModelGrader(), Question);

        Assert.That((grader.Name, grader.Kind, grader.Required, grader.Version), Is.EqualTo(("meal-for-people.human", GraderKind.Human, false, 1)));
        Assert.That((grader.Reviews, grader.Question), Is.EqualTo(("meal-for-people", Question)));
        Assert.That(grader.ExpectedKeys, Is.Empty);
    }

    [Test]
    public void ItCanBeMadeForAGradersName()
    {
        var grader = HumanReviewGrader.For("meal-for-people", Question);

        Assert.That((grader.Name, grader.Reviews), Is.EqualTo(("meal-for-people.human", "meal-for-people")));
    }

    [Test]
    public void ItReviewsOnlyAModelGrader_ACodeRuleNeedsNoPerson()
    {
        Assert.That(() => HumanReviewGrader.For(new ModelGrader("no-error", GraderKind.FixedRule), Question),
            Throws.TypeOf<ArgumentException>().With.Message.Contains("no-error"));
    }

    [Test]
    public async Task EveryTrialIsPending_WithAReasonThatSaysItWaitsForAPerson_AndTheQuestion()
    {
        var grader = HumanReviewGrader.For("meal-for-people", Question);

        var grade = await grader.GradeAsync(Trial(Turn(1)), CancellationToken.None);

        Assert.That((grade.Grader, grade.Outcome, grade.Score), Is.EqualTo(("meal-for-people.human", GradeOutcome.Pending, 0d)));
        Assert.That(grade.Judge, Is.Null);
        Assert.That(grade.Reason, Does.Contain("person").And.Contain(Question));
    }

    [Test]
    public async Task ItIsPendingEvenForAFailedTurn_ThereIsNothingElseItCouldBe()
    {
        var grader = HumanReviewGrader.For("meal-for-people", Question);

        var grade = await grader.GradeAsync(Trial(Turn(1, failure: TrialFailure.Transport)), CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pending));
    }

    // The question a person is asked is the judge prompt's own title, read from it, so it is not typed twice.
    [TestCase("# Judge: is this a meal for the people asked for\n\nYou check one answer.", "Is this a meal for the people asked for?")]
    [TestCase("# Is the recipe safe to eat?\n\nBody", "Is the recipe safe to eat?")]
    [TestCase("# Judge: does the plant need water?  \r\n\r\nBody", "Does the plant need water?")]
    public void TheQuestionIsThePromptsTitleLine(string prompt, string expected)
    {
        Assert.That(ReviewQuestion.From(prompt), Is.EqualTo(expected));
    }

    [TestCase("# Judge:")]
    [TestCase("# Judge:   \n\nBody")]
    [TestCase("#  \n\nBody")]
    public void ATitleWithNothingAfterIt_IsNoTitle_NotACrash(string prompt)
    {
        Assert.That(() => ReviewQuestion.From(prompt), Throws.TypeOf<InvalidDataException>().With.Message.Contains("title"));
    }

    [Test]
    public void APromptWithNoTitle_HasNoQuestion_SoNoReviewIsMadeForIt()
    {
        Assert.That(() => ReviewQuestion.From("You check one answer."), Throws.TypeOf<InvalidDataException>().With.Message.Contains("title"));
    }
}
