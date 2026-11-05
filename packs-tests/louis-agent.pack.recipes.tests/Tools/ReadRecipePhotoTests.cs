using System.Net;
using LouisAgent.Composition.Web.Fetching;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Tests.Providers;
using LouisAgent.Packs.Recipes.Tools;

namespace LouisAgent.Packs.Recipes.Tests.Tools;

/// <summary>
/// <c>ReadRecipe</c> prints one photo, and only one that loads. The Food Fox's photo sat on a dead server and at a 403,
/// and the answers showed a broken image, because nobody had checked.
/// </summary>
public class ReadRecipePhotoTests
{
    private const string Dead = "http://old.example.org/pie.jpg";
    private const string Good = "https://example.com/pie.jpg";

    /// <summary>A page with no recipe data, declaring the dead address, with a working copy in its body.</summary>
    private static readonly string TextPage =
        $"""
         <head><meta property="og:image" content="{Dead}"></head>
         <body><main><h1>Pie</h1><p>Mince and pastry.</p>
         <img src="{Good}" alt="">
         <p><img src="https://example.com/other-dish.jpg" alt="Another recipe"></p></main></body>
         """;

    /// <summary>A page with recipe data naming the dead address, with the working copy in its body.</summary>
    private static readonly string DataPage =
        "<head><script type=\"application/ld+json\">{\"@type\":\"Recipe\",\"name\":\"Pie\",\"image\":\"" + Dead + "\","
        + "\"recipeIngredient\":[\"1 kg mince\"],\"recipeInstructions\":[\"Bake it.\"]}</script></head>"
        + $"<body><h1>Pie</h1><img src=\"{Good}\"></body>";

    /// <summary>Reads a page through a fetcher that gives the dead address this answer.</summary>
    private static Task<string> ReadAsync(string html, Func<HttpResponseMessage> dead) =>
        new RecipeTools(new Dictionary<string, IRecipeSource>(), new FakeSiteBrowser(html),
                new FakePhotoFetcher(new() { [Dead] = dead }))
            .ReadRecipe("https://example.com/recipes/pie/");

    /// <summary>A refused address, and a login page that answers OK but isn't an image, are both no photo.</summary>
    [TestCase(HttpStatusCode.Forbidden, "text/html")]
    [TestCase(HttpStatusCode.NotFound, "image/jpeg")]
    [TestCase(HttpStatusCode.OK, "text/html")]
    public async Task APhotoThatDoesntLoad_GivesWayToTheNext(HttpStatusCode status, string type)
    {
        foreach (string page in new[] { TextPage, DataPage })
        {
            string print = await ReadAsync(page, FakePhotoFetcher.Answers(status, type));

            Assert.That(print, Does.Contain(Good).And.Not.Contain(Dead));
        }
    }

    /// <summary>
    /// Food &amp; Home's photos sit on a host whose robots.txt answers 500, so the rules say we may not fetch them, yet
    /// they load fine in the user's browser. Not being able to check isn't the same as gone, so the photo stays: we
    /// fetch nothing, and the user's browser does the loading.
    /// </summary>
    [Test]
    public async Task APhotoWeMayNotCheck_StaysInThePrint()
    {
        foreach (string page in new[] { TextPage, DataPage })
        {
            string print = await ReadAsync(page, () => throw new BlockedAddressException("robots.txt couldn't be read"));

            Assert.That(print, Does.Contain(Dead));
        }
    }

    /// <summary>A server error says the server had trouble, not that the photo is gone, so it stays too.</summary>
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    public async Task AServerError_KeepsThePhoto(HttpStatusCode status) =>
        Assert.That(await ReadAsync(TextPage, FakePhotoFetcher.Answers(status, "text/html")), Does.Contain(Dead));

    /// <summary>An internal address is another matter: it's refused for safety, and never reaches the user.</summary>
    [Test]
    public async Task AnInternalAddress_IsNeverShown()
    {
        var fetcher = new FakePhotoFetcher(internalAddresses: [Dead]);

        string print = await new RecipeTools(new Dictionary<string, IRecipeSource>(), new FakeSiteBrowser(TextPage), fetcher)
            .ReadRecipe("https://example.com/recipes/pie/");

        Assert.That(print, Does.Contain(Good).And.Not.Contain(Dead));
        Assert.That(fetcher.Asked, Has.None.EqualTo(Dead), "nothing is sent to it");
    }

    /// <summary>A server that never answers costs the check its time limit, and nothing else.</summary>
    [Test]
    public async Task AServerThatDoesntAnswer_GivesWayToTheNext()
    {
        string print = await ReadAsync(TextPage, () => throw new TaskCanceledException("timed out"));

        Assert.That(print, Does.Contain(Good).And.Not.Contain(Dead));
    }

    /// <summary>With nothing that loads there's no image line at all, and the recipe still reads.</summary>
    [Test]
    public async Task NoPhotoThatLoads_MeansNoImage()
    {
        var gone = FakePhotoFetcher.Answers(HttpStatusCode.NotFound, "text/html");
        var fetcher = new FakePhotoFetcher(new() { [Dead] = gone, [Good] = gone });

        string print = await new RecipeTools(new Dictionary<string, IRecipeSource>(), new FakeSiteBrowser(DataPage), fetcher)
            .ReadRecipe("https://example.com/recipes/pie/");

        Assert.That(print, Does.Not.Contain("!["));
        Assert.That(print, Does.Contain("1 kg mince"));
    }

    /// <summary>A text print has one photo, on its own line at the top, and none of the related recipes'.</summary>
    [Test]
    public async Task ATextPrint_ShowsOnlyTheCheckedPhoto()
    {
        string print = await ReadAsync(TextPage, FakePhotoFetcher.Answers(HttpStatusCode.Forbidden, "text/html"));

        Assert.That(print, Does.Not.Contain("other-dish.jpg").And.Not.Contain("Another recipe"));
        Assert.That(print.Split("![").Length - 1, Is.EqualTo(1), "one image");
        Assert.That(print, Does.Contain("Mince and pastry."));
    }

    /// <summary>A photo that loads is taken at once, so the check costs one small request.</summary>
    [Test]
    public async Task APhotoThatLoads_EndsTheCheck()
    {
        var fetcher = new FakePhotoFetcher();

        await new RecipeTools(new Dictionary<string, IRecipeSource>(), new FakeSiteBrowser(TextPage), fetcher)
            .ReadRecipe("https://example.com/recipes/pie/");

        Assert.That(fetcher.Asked, Is.EqualTo(new[] { Dead }));
    }
}
