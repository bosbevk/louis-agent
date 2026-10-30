using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading;
using LouisAgent.SessionStream;
using static LouisAgent.Identities.Puls.Tests.Evals.Checks.PulsTrials;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook.Grading;

/// <summary>
/// The answer shows a photo of the dish, and it's one the recipe page the cook read gave (<c>FetchUrl</c> and
/// <c>ReadRenderedPage</c> both return <c>![alt](url)</c>), never one made up. Louis, 2026-10-25: "i want at least one
/// image to render from the recipe".
/// </summary>
public class RecipeImageTests
{
    /// <summary>A scenario that wants a photo of the recipe.</summary>
    private const string Expected = """{"recipe_image":true}""";

    /// <summary>A recipe page with a photo of the dish.</summary>
    private const string Page =
        "# Bobotie\n\n![Bobotie in a dish](https://marindakook.co.za/wp-content/bobotie.jpg)\n\nIngredients ...";

    /// <summary>The page's photo, as the answer shows it.</summary>
    private const string Shown = "![Bobotie](https://marindakook.co.za/wp-content/bobotie.jpg)";

    /// <summary>The cook reading the recipe page.</summary>
    private static readonly SessionEvent Reads =
        SpecialistCall("FetchUrl", """{"url":"https://marindakook.co.za/bobotie/"}""");

    /// <summary>The check's grade of a one-turn trial made of these events.</summary>
    private static Task<Grade> Grade(string expected, params SessionEvent[] events) =>
        new RecipeImage()
            .GradeAsync(Trial(CookEvalMode, "cook-x", expected, null, Turn(1, events)), CancellationToken.None)
            .AsTask();

    /// <summary>It's a required fixed rule, at version 2, that reads only <c>recipe_image</c>.</summary>
    [Test]
    public void ItIsARequiredFixedRule_ThatReadsTheRecipeImageKey()
    {
        var grader = new RecipeImage();

        Assert.That((grader.Name, grader.Version, grader.Kind, grader.Required),
            Is.EqualTo(("recipe-image", 2, CheckKind.FixedRule, true)));
        Assert.That(grader.ExpectedKeys, Is.EquivalentTo(new[] { "recipe_image" }));
    }

    /// <summary>Showing the photo the page gave passes.</summary>
    [Test]
    public async Task ItPasses_WhenTheAnswerShowsAnImageThePageGave() =>
        Assert.That(
            (await Grade(Expected, Reads, SpecialistResult(Page), Text("### Recipe\n\n" + Shown + "\n\nServes 4")))
            .Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>A photo from a page read in the browser counts too.</summary>
    [Test]
    public async Task ItPasses_WithAnImageFromARenderedPage() =>
        Assert.That((await Grade(Expected,
            SpecialistCall("ReadRenderedPage", """{"url":"https://marindakook.co.za/bobotie/"}"""),
            SpecialistResult(Page),
            Text(Shown))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>The page had a photo and the answer shows none: a fail that says so.</summary>
    [Test]
    public async Task ItFails_WhenThePageHadAnImageAndTheAnswerShowsNone()
    {
        var grade = await Grade(Expected, Reads, SpecialistResult(Page), Text("### Recipe\n\nServes 4"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("image"));
    }

    /// <summary>A photo the page never gave is made up, and fails.</summary>
    [Test]
    public async Task ItFails_WhenTheImageIsNotOneThePageGave()
    {
        var grade = await Grade(Expected, Reads, SpecialistResult(Page),
            Text("![Bobotie](https://example.com/made-up.jpg)"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("page"));
    }

    /// <summary>If no page the cook read had a photo, there was none to show: Unknown.</summary>
    [Test]
    public async Task ItIsUnknown_WhenNoPageTheCookReadHadAnImage() =>
        Assert.That(
            (await Grade(Expected, Reads, SpecialistResult("# Bobotie\n\nIngredients ..."), Text("Serves 4"))).Outcome,
            Is.EqualTo(GradeOutcome.Unknown));

    /// <summary>A scenario that asks for no photo passes.</summary>
    [Test]
    public async Task ItPasses_WhenTheScenarioAsksForNoImage() =>
        Assert.That((await Grade("""{"routes_to":null}""", Text("Resep."))).Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>A turn the infrastructure broke is Unknown, even with the photo shown.</summary>
    [Test]
    public async Task ItIsUnknown_ForAnInfrastructureFailure()
    {
        var failed = Trial(CookEvalMode, "cook-x", Expected, null,
            Turn(1, [Reads, SpecialistResult(Page), Text(Shown)], TurnFailure.Timeout));

        Assert.That((await new RecipeImage().GradeAsync(failed, CancellationToken.None)).Outcome,
            Is.EqualTo(GradeOutcome.Unknown));
    }

    // Version 2 (Louis, 2026-10-26, after the first cook run on the new prompt): the image is judged against the page
    // the answer links to, and a share button, print icon or logo is no photo of the dish. Version 1 counted every
    // image on every page the cook read, so an answer built on a page with no photo failed because another page had
    // one.

    /// <summary>The recipe page the answer links to, with no photo.</summary>
    private const string CitedPage =
        "URL: https://yourrecipeblog.com/recipes/pap-en-wors-tert/\nTitle: PAP EN WORS TERT\n\n### PAP EN WORS TERT\n\n- 500 g wors";

    /// <summary>Another page the cook read, which has a photo of another dish.</summary>
    private const string OtherPage =
        "URL: https://marindakook.co.za/bobotie/\nTitle: Bobotie\n\n![Bobotie in a dish](https://marindakook.co.za/wp-content/bobotie.jpg)\n\nIngredients";

    /// <summary>A photo of the linked page's dish.</summary>
    private const string Photo = "![Pap en wors tert](https://yourrecipeblog.com/wp-content/uploads/pap.jpg)";

    /// <summary>The answer's link to the recipe page.</summary>
    private const string Cites = "Skakel: [Pap en wors tert](https://yourrecipeblog.com/recipes/pap-en-wors-tert/)";

    /// <summary>
    /// The linked page had no photo, so there was none to show, even if another page the cook read had one.
    /// </summary>
    [Test]
    public async Task ItIsUnknown_WhenThePageTheAnswerLinksToHadNoPhoto_EvenIfAnotherPageTheCookReadHadOne()
    {
        var grade = await Grade(Expected, Reads, SpecialistResult(OtherPage), SpecialistResult(CitedPage),
            Text("### Recipe\n\n" + Cites));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grade.Reason, Does.Contain("image"));
    }

    /// <summary>The linked page had a photo and the answer shows none: a fail.</summary>
    [Test]
    public async Task ItFails_WhenThePageTheAnswerLinksToHadAPhoto_AndTheAnswerShowsNone()
    {
        var grade = await Grade(Expected, Reads, SpecialistResult(OtherPage),
            SpecialistResult(CitedPage + "\n\n" + Photo), Text(Cites));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>Showing the linked page's photo passes.</summary>
    [Test]
    public async Task ItPasses_WhenTheAnswerShowsThePhotoOfThePageItLinksTo()
    {
        var grade = await Grade(Expected, Reads, SpecialistResult(OtherPage),
            SpecialistResult(CitedPage + "\n\n" + Photo), Text(Photo + "\n\n" + Cites));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
    }

    /// <summary>A print icon, a share button or a logo is no photo of the dish.</summary>
    [TestCase("https://www.foodloversrecipes.com/wp-content/plugins/pdf-print/images/pdf.png")]
    [TestCase(
        "https://www.foodloversrecipes.com/wp-content/plugins/simple-share-buttons-adder/buttons/somacro/facebook.png")]
    [TestCase("https://example.co.za/images/logo.png")]
    [TestCase("https://example.co.za/assets/print-icon.png")]
    public async Task AnIconOrAShareButton_IsNoPhotoOfTheDish(string icon) =>
        Assert.That(
            (await Grade(Expected, Reads, SpecialistResult("# Pap\n\n![x](" + icon + ")\n\nIngredients"),
                Text("Serves 4"))).Outcome, Is.EqualTo(GradeOutcome.Unknown));
}
