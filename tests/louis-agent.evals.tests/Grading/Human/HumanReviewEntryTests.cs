using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Facts;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Grading.Models.Rubrics;

namespace LouisAgent.Evals.Tests.Grading.Human;

/// <summary>
/// The <c>human</c> entry: a person's review of a judge the eval built before it, asking the judge's own question
/// about the scenarios the judge has something to judge in.
/// </summary>
public class HumanReviewEntryTests
{
    /// <summary>A judge asking about <c>tone</c>, with a question in its rubric's title.</summary>
    private static readonly JudgeCheck Judge =
        new("judge", 1, "# Judge: is the answer polite\n", ["tone"], () => null);

    /// <summary>The review is made for the judge built before it: its name, its question, its keys.</summary>
    [Test]
    public void TheEntry_ReviewsTheJudgeBuiltBeforeIt()
    {
        var review = (HumanReviewCheck)Build("""{"reviews": "judge"}""", Judge);

        Assert.That((review.Name, review.Reviews, review.Question),
            Is.EqualTo(("judge.human", "judge", "Is the answer polite?")));
        Assert.That(review.ExpectedKeys, Is.EqualTo(new[] { "tone" }));
    }

    /// <summary>A judge the eval renamed is reviewed under its new name, so the review's name matches it.</summary>
    [Test]
    public void ARenamedJudge_IsReviewedUnderItsName()
    {
        var review = (HumanReviewCheck)Build("""{"reviews": "warmth"}""", new NamedCheck("warmth", Judge));

        Assert.That((review.Name, review.Question), Is.EqualTo(("warmth.human", "Is the answer polite?")));
    }

    /// <summary>A review of a check that isn't built before it, or isn't a judge, is refused, naming it.</summary>
    [TestCase("judge")]
    [TestCase("no-error")]
    public void AReviewOfNoJudge_IsRefused(string reviews)
    {
        var refusal = Assert.Throws<InvalidDataException>(() =>
            Build($$"""{"reviews": "{{reviews}}"}""", new NoErrorCheck()));

        Assert.That(refusal!.Message, Does.Contain(reviews));
    }

    /// <summary>Builds the review the way an eval file will, after these checks.</summary>
    private static ICheck Build(string settings, params ICheck[] before) =>
        new CheckCatalog(GeneralChecks.All).Get("human")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), new CheckContext("", before));
}
