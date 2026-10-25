using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using LouisAgent.Evals.Puls.Evals.Cook.Grading;
using static LouisAgent.Evals.Puls.Tests.PulsTrials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Grading;

// F18 step 5.7 (Louis, 2026-10-25: "i want at least one image to render from the recipe"): the answer shows an image of the dish, and the image is one the
// recipe page the cook read gave (FetchUrl and ReadRenderedPage both return `![alt](url)`), never one made up. Written first: the grader did not exist.
public class RecipeImageTests
{
    private const string Expected = """{"recipe_image":true}""";
    private const string Page = "# Bobotie\n\n![Bobotie in a dish](https://marindakook.co.za/wp-content/bobotie.jpg)\n\nIngredients ...";
    private const string Shown = "![Bobotie](https://marindakook.co.za/wp-content/bobotie.jpg)";
    private static readonly SessionEvent Reads = SpecialistCall("FetchUrl", """{"url":"https://marindakook.co.za/bobotie/"}""");

    private static Task<Grade> Grade(string expected, params SessionEvent[] events) =>
        new RecipeImage().GradeAsync(Trial(CookMode, "cook-x", expected, null, Turn(1, events)), CancellationToken.None).AsTask();

    [Test]
    public void ItIsARequiredFixedRule_ThatReadsTheRecipeImageKey()
    {
        var grader = new RecipeImage();

        Assert.That((grader.Name, grader.Version, grader.Kind, grader.Required), Is.EqualTo(("recipe-image", 1, GraderKind.FixedRule, true)));
        Assert.That(grader.ExpectedKeys, Is.EquivalentTo(new[] { "recipe_image" }));
    }

    [Test]
    public async Task ItPasses_WhenTheAnswerShowsAnImageThePageGave() =>
        Assert.That((await Grade(Expected, Reads, SpecialistResult(Page), Text("### Recipe\n\n" + Shown + "\n\nServes 4"))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task ItPasses_WithAnImageFromARenderedPage() =>
        Assert.That((await Grade(Expected, SpecialistCall("ReadRenderedPage", """{"url":"https://marindakook.co.za/bobotie/"}"""), SpecialistResult(Page),
            Text(Shown))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task ItFails_WhenThePageHadAnImageAndTheAnswerShowsNone()
    {
        var grade = await Grade(Expected, Reads, SpecialistResult(Page), Text("### Recipe\n\nServes 4"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("image"));
    }

    [Test]
    public async Task ItFails_WhenTheImageIsNotOneThePageGave()
    {
        var grade = await Grade(Expected, Reads, SpecialistResult(Page), Text("![Bobotie](https://example.com/made-up.jpg)"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("page"));
    }

    [Test]
    public async Task ItIsUnknown_WhenNoPageTheCookReadHadAnImage() =>
        Assert.That((await Grade(Expected, Reads, SpecialistResult("# Bobotie\n\nIngredients ..."), Text("Serves 4"))).Outcome, Is.EqualTo(GradeOutcome.Unknown));

    [Test]
    public async Task ItPasses_WhenTheScenarioAsksForNoImage() =>
        Assert.That((await Grade("""{"routes_to":null}""", Text("Resep."))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    [Test]
    public async Task ItIsUnknown_ForAnInfrastructureFailure()
    {
        var failed = Trial(CookMode, "cook-x", Expected, null, Turn(1, [Reads, SpecialistResult(Page), Text(Shown)], TrialFailure.Timeout));

        Assert.That((await new RecipeImage().GradeAsync(failed, CancellationToken.None)).Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }
}
