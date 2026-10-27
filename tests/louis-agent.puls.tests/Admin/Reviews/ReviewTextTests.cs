using LouisAgent.Puls.Admin.Grades;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Summary;

namespace LouisAgent.Puls.Tests.Admin.Reviews;

// F18 step 6a.3c (the review of 6a.3 to 6a.5): the review panel's sentences, the agreement card's hover and the summary's bar width were in code-behinds and a RenderTreeBuilder. Now they are
// plain classes with tests, beside GradesText and AgreementText. Written first: ReviewText, AgreementText.Hover, SummaryText.BarWidth and ReviewNote did not exist.
public class ReviewTextTests
{
    [Test]
    public void TheQuestion_IsTheGradersOwn_OrAFallbackNamingTheJudge()
    {
        Assert.That(ReviewText.Question("Is this a meal?", "meal-for-people"), Is.EqualTo("Is this a meal?"));
        Assert.That(ReviewText.Question(null, "meal-for-people"),
            Is.EqualTo("Is this answer good enough for meal-for-people?"));
    }

    [TestCase("pass", GradeOutcome.Pass, "You agreed with the judge.")]
    [TestCase("fail", GradeOutcome.Pass, "You disagreed with the judge.")]
    [TestCase("fail", GradeOutcome.Fail, "You agreed with the judge.")]
    [TestCase("unsure", GradeOutcome.Pass, "You were unsure.")]
    [TestCase("pass", GradeOutcome.Unknown, "The judge could not tell.")]
    public void TheResult_SaysWhetherYouAgreedWithTheJudge(string person, GradeOutcome judge, string expected) =>
        Assert.That(ReviewText.Result(person, judge), Is.EqualTo(expected));

    [Test]
    public void NoResult_UntilBothHaveSaid()
    {
        Assert.That(ReviewText.Result(null, GradeOutcome.Pass), Is.Empty, "you haven't graded");
        Assert.That(ReviewText.Result("pass", null), Is.Empty, "the judge gave no grade");
    }

    [TestCase(GradeOutcome.Pass, "The judge said pass.")]
    [TestCase(GradeOutcome.Fail, "The judge said fail.")]
    [TestCase(GradeOutcome.Unknown, "The judge could not tell.")]
    [TestCase(null, "The judge could not tell.")]
    public void TheJudgesWords(GradeOutcome? outcome, string expected) =>
        Assert.That(ReviewText.Judge(outcome), Is.EqualTo(expected));

    [Test]
    public void TheHover_GivesTheCountsBehindABar()
    {
        Assert.That(AgreementText.Hover("passed", new ClassAgreement(13, 1, 0, PersonPassed: true)),
            Is.EqualTo("Of the 14 answers you passed: the judge passed 13, failed 1, couldn't tell 0."));
    }

    [Test]
    public void TheNotesLimit_IsWhatTheAPIAccepts_AndTheBarOfASliceIsItsRateToOneDecimal()
    {
        Assert.That(ReviewNote.MaxLength, Is.EqualTo(2000), "the API's ReviewEndpoints.MaxNoteLength: change both");
        Assert.That(SummaryText.BarWidth(0.75), Is.EqualTo("75%"));
        Assert.That(SummaryText.BarWidth(0.3333), Is.EqualTo("33.3%"));
        Assert.That(SummaryText.BarWidth(1), Is.EqualTo("100%"));
    }
}
