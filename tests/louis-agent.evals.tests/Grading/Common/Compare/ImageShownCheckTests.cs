using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Compare;
using LouisAgent.Evals.Grading.Common.Compare.Readers;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.SessionStream;
using static LouisAgent.Evals.Tests.Trials.TrialBuilder;

namespace LouisAgent.Evals.Tests.Grading.Common.Compare;

/// <summary>
/// The <c>image-shown</c> check: the first answer shows an image, and it's one a page the agent read gave, never one
/// made up. These are the cook's <c>recipe-image</c> cases, one for one, under the new name; telling a share button
/// from a photo of the dish is the reader's job, so that case lives with Puls's reader.
/// </summary>
public class ImageShownCheckTests
{
    /// <summary>The scenario key the cook asks for an image with.</summary>
    private const string Key = "recipe_image";

    /// <summary>A scenario that wants an image.</summary>
    private const string Expected = """{"recipe_image":true}""";

    /// <summary>A recipe page with a photo of the dish.</summary>
    private const string Page =
        "# Bobotie\n\n![Bobotie in a dish](https://marindakook.co.za/wp-content/bobotie.jpg)\n\nIngredients ...";

    /// <summary>The page's photo, as the answer shows it.</summary>
    private const string Shown = "![Bobotie](https://marindakook.co.za/wp-content/bobotie.jpg)";

    /// <summary>The cook reading the recipe page.</summary>
    private static readonly SessionEvent Reads =
        SubToolCall("FetchUrl", """{"url":"https://marindakook.co.za/bobotie/"}""");

    /// <summary>The check over the plain markdown reader, asked for under the cook's key.</summary>
    private static readonly ImageShownCheck Check = new(new MarkdownImages(), Key);

    /// <summary>The check's grade of a one-turn trial made of these events.</summary>
    private static async Task<Grade> GradeAsync(string expected, params SessionEvent[] events) =>
        await Check.GradeAsync(Trial(Mode, expected, Turn(1, events)), CancellationToken.None);

    /// <summary>Showing the photo the page gave passes.</summary>
    [Test]
    public async Task ItPasses_WhenTheAnswerShowsAnImageThePageGave() =>
        Assert.That(
            (await GradeAsync(Expected, Reads, SubToolResult(Page), Text("### Recipe\n\n" + Shown + "\n\nServes 4")))
            .Outcome, Is.EqualTo(GradeOutcome.Pass));

    /// <summary>A photo from a page the agent read itself counts too, not only a specialist's.</summary>
    [Test]
    public async Task ItPasses_WithAnImageFromThePagesTheAgentRead() =>
        Assert.That((await GradeAsync(Expected, ToolCall("ReadRenderedPage"), ToolResult(Page), Text(Shown))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>The page had a photo and the answer shows none: a fail that says so.</summary>
    [Test]
    public async Task ItFails_WhenThePageHadAnImageAndTheAnswerShowsNone()
    {
        var grade = await GradeAsync(Expected, Reads, SubToolResult(Page), Text("### Recipe\n\nServes 4"));

        Assert.That((grade.Outcome, grade.Score), Is.EqualTo((GradeOutcome.Fail, 0.0)));
        Assert.That(grade.Reason, Does.Contain("image"));
    }

    /// <summary>A photo the page never gave is made up, and fails.</summary>
    [Test]
    public async Task ItFails_WhenTheImageIsNotOneThePageGave()
    {
        var grade = await GradeAsync(Expected, Reads, SubToolResult(Page),
            Text("![Bobotie](https://example.com/made-up.jpg)"));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
        Assert.That(grade.Reason, Does.Contain("page"));
    }

    /// <summary>If no page the agent read had a photo, there was none to show: Unknown.</summary>
    [Test]
    public async Task ItIsUnknown_WhenNoPageTheAgentReadHadAnImage() =>
        Assert.That(
            (await GradeAsync(Expected, Reads, SubToolResult("# Bobotie\n\nIngredients ..."), Text("Serves 4")))
            .Outcome, Is.EqualTo(GradeOutcome.Unknown));

    /// <summary>A scenario that doesn't ask under the key passes, saying so.</summary>
    [Test]
    public async Task ItPasses_WhenTheScenarioAsksForNoImage()
    {
        var grade = await GradeAsync("""{"routes_to":null}""", Text("Resep."));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Pass));
        Assert.That(grade.Reason, Does.Contain(Key));
    }

    /// <summary>Without a key the check asks every scenario for an image.</summary>
    [Test]
    public async Task WithoutAKey_EveryScenarioIsAsked()
    {
        var grade = await new ImageShownCheck(new MarkdownImages()).GradeAsync(
            Trial(Mode, "{}", Turn(1, [Reads, SubToolResult(Page), Text("Resep.")])), CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Fail));
    }

    /// <summary>A turn the infrastructure broke is Unknown, even with the photo shown.</summary>
    [Test]
    public async Task ItIsUnknown_ForAnInfrastructureFailure()
    {
        var failed = Trial(Mode, Expected,
            Turn(1, [Reads, SubToolResult(Page), Text(Shown)], failure: TurnFailure.Timeout));

        Assert.That((await Check.GradeAsync(failed, CancellationToken.None)).Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>A first turn that streamed an error, or gave no answer, has nothing to look in: Unknown.</summary>
    [Test]
    public async Task AFirstTurnWithNothingToRead_IsUnknown()
    {
        Assert.That((await GradeAsync(Expected, Reads, SubToolResult(Page), Text(Shown), Error())).Outcome,
            Is.EqualTo(GradeOutcome.Unknown));
        Assert.That((await GradeAsync(Expected, Reads, SubToolResult(Page))).Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    // The cook's version 2 (Louis, 2026-10-26): the image is judged against the page the answer links to, so an answer
    // built on a page with no photo isn't failed for the photo of another page the agent only read on the way.

    /// <summary>The recipe page the answer links to, with no photo.</summary>
    private const string CitedPage =
        "URL: https://yourrecipeblog.com/recipes/pap-en-wors-tert/\nTitle: PAP EN WORS TERT\n\n### PAP EN WORS TERT\n\n- 500 g wors";

    /// <summary>Another page the agent read, which has a photo of another dish.</summary>
    private const string OtherPage =
        "URL: https://marindakook.co.za/bobotie/\nTitle: Bobotie\n\n![Bobotie in a dish](https://marindakook.co.za/wp-content/bobotie.jpg)\n\nIngredients";

    /// <summary>A photo of the linked page's dish.</summary>
    private const string Photo = "![Pap en wors tert](https://yourrecipeblog.com/wp-content/uploads/pap.jpg)";

    /// <summary>The answer's link to the recipe page.</summary>
    private const string Cites = "Skakel: [Pap en wors tert](https://yourrecipeblog.com/recipes/pap-en-wors-tert/)";

    /// <summary>
    /// The linked page had no photo, so there was none to show, even if another page the agent read had one.
    /// </summary>
    [Test]
    public async Task ItIsUnknown_WhenThePageTheAnswerLinksToHadNoPhoto_EvenIfAnotherPageHadOne()
    {
        var grade = await GradeAsync(Expected, Reads, SubToolResult(OtherPage), SubToolResult(CitedPage),
            Text("### Recipe\n\n" + Cites));

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
        Assert.That(grade.Reason, Does.Contain("image"));
    }

    /// <summary>The linked page had a photo and the answer shows none: a fail.</summary>
    [Test]
    public async Task ItFails_WhenThePageTheAnswerLinksToHadAPhoto_AndTheAnswerShowsNone() =>
        Assert.That((await GradeAsync(Expected, Reads, SubToolResult(OtherPage),
            SubToolResult(CitedPage + "\n\n" + Photo), Text(Cites))).Outcome, Is.EqualTo(GradeOutcome.Fail));

    /// <summary>Showing the linked page's photo passes.</summary>
    [Test]
    public async Task ItPasses_WhenTheAnswerShowsThePhotoOfThePageItLinksTo() =>
        Assert.That((await GradeAsync(Expected, Reads, SubToolResult(OtherPage),
                SubToolResult(CitedPage + "\n\n" + Photo), Text(Photo + "\n\n" + Cites))).Outcome,
            Is.EqualTo(GradeOutcome.Pass));

    /// <summary>
    /// The reader decides what counts as an image, on the pages and in the answer alike: an image it leaves out is
    /// none to show.
    /// </summary>
    [Test]
    public async Task WhatCountsAsAnImage_IsTheReaders()
    {
        var check = new ImageShownCheck(new NoLogos(), Key);
        const string page = "# Pap\n\n![x](https://example.co.za/images/logo.png)\n\nIngredients";

        var grade = await check.GradeAsync(Trial(Mode, Expected, Turn(1, [Reads, SubToolResult(page), Text("Pap.")])),
            CancellationToken.None);

        Assert.That(grade.Outcome, Is.EqualTo(GradeOutcome.Unknown));
    }

    /// <summary>
    /// The entry builds the check from a file's settings, through the reader the file names, and lists its key; left
    /// out, the key is none.
    /// </summary>
    [Test]
    public void TheEntry_BuildsTheCheck_ThroughTheNamedReader()
    {
        var keyed = Build("""{"reader": "markdown-images", "key": "recipe_image", "required": false}""");
        var always = Build("""{"reader": "markdown-images"}""");

        Assert.That(keyed, Is.TypeOf<ImageShownCheck>());
        Assert.That((keyed.Name, keyed.Version, keyed.Required), Is.EqualTo(("image-shown", 1, false)));
        Assert.That(keyed.ExpectedKeys, Is.EqualTo(new[] { Key }));
        Assert.That(always.ExpectedKeys, Is.Empty);
    }

    /// <summary>A reader of the wrong kind, or a blank one, is refused, naming what was wrong.</summary>
    [TestCase("""{"reader": "digits"}""", "digits")]
    [TestCase("""{"reader": " "}""", "reader")]
    [TestCase("""{"reader": "markdown-images", "key": " "}""", "key")]
    public void ABadSetting_IsRefused(string settings, string named) =>
        Assert.That(Assert.Throws<InvalidDataException>(() => Build(settings))!.Message, Does.Contain(named));

    /// <summary>
    /// Builds the check the way an eval file does: by its type, with the general readers to name from.
    /// </summary>
    private static ICheck Build(string settings) =>
        new CheckCatalog(GeneralChecks.All).Get("image-shown")
            .Create(JsonDocument.Parse(settings).RootElement.Clone(),
                new CheckContext(string.Empty, [], new ReaderCatalog(GeneralReaders.All)));

    /// <summary>A reader that, as Puls's does, leaves a page's logos out.</summary>
    private sealed class NoLogos : IImageReader
    {
        /// <inheritdoc />
        public string Name => "no-logos";

        /// <inheritdoc />
        public IReadOnlyList<string> Images(string text) =>
            [.. new MarkdownImages().Images(text).Where(url => !url.Contains("logo", StringComparison.Ordinal))];
    }
}
