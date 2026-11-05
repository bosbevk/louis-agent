using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Sites;
using LouisAgent.Packs.Recipes.Tests.Providers;
using LouisAgent.Packs.Recipes.Tools;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Recipes.Tests.Tools.Reading;

/// <summary>
/// Louis's rule: "store each read from a site we do not have yet, and then we serve those to ourselves". A read looks
/// in the library first, and only a page from one of our sites is ever kept.
/// </summary>
public class ReadRecipeLibraryTests
{
    private static readonly RecipeSite Taste = RecipeSites.Find("taste")!;
    private static readonly string OnOurSite = new Uri(Taste.Home, "recipes/pie/").AbsoluteUri;
    private const string OffOurTable = "https://not-on-the-table.example/recipes/pie/";

    private const string DataPage =
        "<head><script type=\"application/ld+json\">{\"@type\":\"Recipe\",\"name\":\"Pie\","
        + "\"recipeIngredient\":[\"1 kg mince\"],\"recipeInstructions\":[\"Bake it.\"]}</script></head><body><h1>Pie</h1></body>";

    private const string TextPage = "<html><head><title>Pie</title></head><body><main><h1>Pie</h1><p>1 kg mince. Bake it.</p></main></body></html>";

    /// <summary>The tools over a browser with this page, and the library.</summary>
    private static (RecipeTools Tools, FakeSiteBrowser Browser) Over(FakeRecipeLibrary library, string page, Func<bool>? skip = null)
    {
        var browser = new FakeSiteBrowser(page);
        var shelf = new RecipeShelf(library, library, [Taste], new FakeTimeProvider(DateTimeOffset.UnixEpoch), skip);
        return (new RecipeTools(new Dictionary<string, IRecipeSource>(), browser, new FakePhotoFetcher(), shelf), browser);
    }

    /// <summary>The second read opens no browser, and the cook gets the same words, data page or text page.</summary>
    [TestCase(DataPage)]
    [TestCase(TextPage)]
    public async Task TheSecondRead_OpensNoBrowser_AndReadsTheSame(string page)
    {
        var library = new FakeRecipeLibrary();
        var (tools, browser) = Over(library, page);

        string first = await tools.ReadRecipe(OnOurSite);
        string second = await tools.ReadRecipe(OnOurSite);

        Assert.That(browser.Requested, Has.Count.EqualTo(1));
        Assert.That(second, Is.EqualTo(first));
        Assert.That(second, Does.StartWith("--- untrusted recipe data"), "a stored print is still framed as data");
        Assert.That(library.Kept.Single().SiteSlug, Is.EqualTo(Taste.Slug));
        Assert.That(library.Kept.Single().Title, Does.Contain("Pie"));
    }

    /// <summary>A search says it asked our library too, and a hit from it reads like any other.</summary>
    [Test]
    public async Task ASearch_SaysItAskedOurLibrary()
    {
        var library = new FakeRecipeLibrary();
        library.Kept.Add(new StoredRecipe(new Uri(OnOurSite), Taste.Slug, "Pie", "print", DateTimeOffset.UnixEpoch));
        var site = new FakeRecipeSource("foodandhome");
        var sources = new Dictionary<string, IRecipeSource> { ["foodandhome"] = site };
        var shelf = new RecipeShelf(library, library, [Taste], new FakeTimeProvider());

        string result = await new RecipeTools(sources, new FakeSiteBrowser(), new FakePhotoFetcher(), shelf).FindRecipes("Pie");

        Assert.That(result, Does.Contain("our library"));
        Assert.That(result, Does.Contain($"1. Pie · {Taste.Name} · {OnOurSite}"));
    }

    /// <summary>A page the print can't frame twice: what's kept holds no frame of its own.</summary>
    [Test]
    public async Task WhatIsKept_HoldsNoFrame()
    {
        var library = new FakeRecipeLibrary();

        await Over(library, DataPage).Tools.ReadRecipe(OnOurSite);

        Assert.That(library.Kept.Single().Print, Does.Not.Contain("untrusted recipe data"));
    }

    /// <summary>`ReadRecipe` reads any page, but a stored page reaches other people: only our sites' pages enter.</summary>
    [Test]
    public async Task APageOffOurTable_IsNeverKept()
    {
        var library = new FakeRecipeLibrary();

        string print = await Over(library, DataPage).Tools.ReadRecipe(OffOurTable);

        Assert.That(print, Does.Contain("1 kg mince"));
        Assert.That(library.Kept, Is.Empty);
    }

    /// <summary>A page with nothing on it isn't a recipe, so it isn't kept.</summary>
    [Test]
    public async Task AnEmptyPage_IsNeverKept()
    {
        var library = new FakeRecipeLibrary();

        await Over(library, "<html><body></body></html>").Tools.ReadRecipe(OnOurSite);

        Assert.That(library.Kept, Is.Empty);
    }

    /// <summary>A refused page comes back as its reason, and that isn't a recipe either.</summary>
    [Test]
    public async Task ARefusedPage_IsNeverKept()
    {
        var library = new FakeRecipeLibrary();
        var browser = new FakeSiteBrowser(fails: new WebBrowserException("robots.txt says no"));
        var shelf = new RecipeShelf(library, library, [Taste], new FakeTimeProvider(), null);

        string print = await new RecipeTools(new Dictionary<string, IRecipeSource>(), browser, new FakePhotoFetcher(), shelf)
            .ReadRecipe(OnOurSite);

        Assert.That(print, Does.Contain("robots.txt says no"));
        Assert.That(library.Kept, Is.Empty);
    }

    /// <summary>A copy never goes stale: however old, it's served, and it isn't read again.</summary>
    [Test]
    public async Task AnOldCopy_IsStillServed()
    {
        var library = new FakeRecipeLibrary();
        library.Kept.Add(new StoredRecipe(new Uri(OnOurSite), Taste.Slug, "Pie", "# Pie, from long ago", DateTimeOffset.UnixEpoch));
        var (tools, browser) = Over(library, DataPage);

        Assert.That(await tools.ReadRecipe(OnOurSite), Does.Contain("# Pie, from long ago"));
        Assert.That(browser.Requested, Is.Empty);
    }

    /// <summary>A library that's down costs the read nothing: the cook gets the page.</summary>
    [Test]
    public async Task ALibraryThatIsDown_CostsTheReadNothing()
    {
        var (tools, browser) = Over(new FakeRecipeLibrary(fails: true), DataPage);

        Assert.That(await tools.ReadRecipe(OnOurSite), Does.Contain("1 kg mince"));
        Assert.That(browser.Requested, Has.Count.EqualTo(1));
    }

    /// <summary>While it's told to skip, as an eval's trial is, the library neither answers nor keeps.</summary>
    [Test]
    public async Task WhenToldToSkip_ItNeitherAnswersNorKeeps()
    {
        var library = new FakeRecipeLibrary();
        library.Kept.Add(new StoredRecipe(new Uri(OnOurSite), Taste.Slug, "Pie", "# Stored", DateTimeOffset.UnixEpoch));
        var (tools, browser) = Over(library, DataPage, skip: () => true);

        string print = await tools.ReadRecipe(OnOurSite);

        Assert.That(print, Does.Not.Contain("# Stored"));
        Assert.That(browser.Requested, Has.Count.EqualTo(1));
        Assert.That(library.Kept, Has.Count.EqualTo(1));
    }
}
