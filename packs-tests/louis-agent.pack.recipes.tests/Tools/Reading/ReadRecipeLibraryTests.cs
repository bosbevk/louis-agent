using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Sites;
using LouisAgent.Packs.Recipes.Tests.Library;
using LouisAgent.Packs.Recipes.Tests.Providers;
using LouisAgent.Packs.Recipes.Tools;
using LouisAgent.Packs.Recipes.Library;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Recipes.Tests.Tools.Reading;

/// <summary>
/// Louis's rule: "store each read from a site we do not have yet, and then we serve those to ourselves". A read keeps a
/// proper recipe from one of our sites, a search hands out our API's link to it, and that link opens no browser.
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

    /// <summary>Our link opens no browser, and the cook gets the same words, with the site's link for credit.</summary>
    [Test]
    public async Task OurLink_OpensNoBrowser_AndReadsLikeThePage()
    {
        var library = new FakeRecipeLibrary();
        var (tools, browser) = Over(library, DataPage);

        string first = await tools.ReadRecipe(OnOurSite);
        string second = await tools.ReadRecipe(library.AddressOf(library.Kept.Single()).AbsoluteUri);

        Assert.That(browser.Requested, Has.Count.EqualTo(1));
        Assert.That(second, Is.EqualTo(first));
        Assert.That(second, Does.StartWith("--- untrusted recipe data"), "a kept recipe is still framed as data");
        Assert.That(second, Does.Contain(OnOurSite), "the link in the print stays the site's");
        Assert.That(second, Does.Not.Contain("library.example"));
    }

    /// <summary>A search hands out our link, and still says whose recipe it is and that it asked our library.</summary>
    [Test]
    public async Task ASearch_GivesOurLink_AndNamesTheSite()
    {
        var library = new FakeRecipeLibrary();
        library.Kept.Add(KeptRecipes.Of(OnOurSite));
        var sources = new Dictionary<string, IRecipeSource> { ["foodandhome"] = new FakeRecipeSource("foodandhome") };
        var shelf = new RecipeShelf(library, library, [Taste], new FakeTimeProvider());

        string result = await new RecipeTools(sources, new FakeSiteBrowser(), new FakePhotoFetcher(), shelf).FindRecipes("Pie");

        Assert.That(result, Does.Contain("our library"));
        Assert.That(result, Does.Contain($"1. Pie · {Taste.Name} · {library.AddressOf(library.Kept.Single())}"));
    }

    /// <summary>What's kept is the recipe itself, line by line, not the words we printed about it.</summary>
    [Test]
    public async Task WhatIsKept_IsAProperRecipe()
    {
        var library = new FakeRecipeLibrary();

        await Over(library, DataPage).Tools.ReadRecipe(OnOurSite);

        var kept = library.Kept.Single();
        Assert.That(kept.SiteSlug, Is.EqualTo(Taste.Slug));
        Assert.That(kept.Recipe.Name, Is.EqualTo("Pie"));
        Assert.That(kept.Recipe.Ingredients.Select(line => line.Text), Is.EqualTo(new[] { "1 kg mince" }));
        Assert.That(kept.Recipe.Steps.Select(line => line.Text), Is.EqualTo(new[] { "Bake it." }));
        Assert.That(kept.Page, Is.EqualTo(new Uri(OnOurSite)));
    }

    /// <summary>A page with no recipe data is read for the cook, but isn't a proper recipe, so it stays out.</summary>
    [Test]
    public async Task APageWithNoRecipeData_IsReadButNotKept()
    {
        var library = new FakeRecipeLibrary();

        string print = await Over(library, TextPage).Tools.ReadRecipe(OnOurSite);

        Assert.That(print, Does.Contain("1 kg mince"));
        Assert.That(library.Kept, Is.Empty);
    }

    /// <summary>A stored page reaches other people, so only our own sites' pages enter.</summary>
    [Test]
    public async Task APageOffOurTable_IsNeverKept()
    {
        var library = new FakeRecipeLibrary();

        string print = await Over(library, DataPage).Tools.ReadRecipe(OffOurTable);

        Assert.That(print, Does.Contain("1 kg mince"));
        Assert.That(library.Kept, Is.Empty);
    }

    /// <summary>A refused page comes back as its reason, and that isn't a recipe either.</summary>
    [Test]
    public async Task ARefusedPage_IsNeverKept()
    {
        var library = new FakeRecipeLibrary();
        var browser = new FakeSiteBrowser(fails: new WebBrowserException("robots.txt says no"));
        var shelf = new RecipeShelf(library, library, [Taste], new FakeTimeProvider());

        string print = await new RecipeTools(new Dictionary<string, IRecipeSource>(), browser, new FakePhotoFetcher(), shelf)
            .ReadRecipe(OnOurSite);

        Assert.That(print, Does.Contain("robots.txt says no"));
        Assert.That(library.Kept, Is.Empty);
    }

    /// <summary>A copy never goes stale: however old, it's served, and the site isn't read again.</summary>
    [Test]
    public async Task AnOldCopy_IsStillServed()
    {
        var library = new FakeRecipeLibrary();
        library.Kept.Add(KeptRecipes.Of(OnOurSite, name: "Pie from long ago"));
        var (tools, browser) = Over(library, DataPage);

        string print = await tools.ReadRecipe(library.AddressOf(library.Kept.Single()).AbsoluteUri);

        Assert.That(print, Does.Contain("Pie from long ago"));
        Assert.That(browser.Requested, Is.Empty);
    }

    /// <summary>The site's own address still goes to the site, and our copy stays as it was first read.</summary>
    [Test]
    public async Task TheSitesOwnAddress_GoesToTheSite_AndTheFirstCopyStays()
    {
        var library = new FakeRecipeLibrary();
        library.Kept.Add(KeptRecipes.Of(OnOurSite, name: "First copy"));
        var (tools, browser) = Over(library, DataPage);

        await tools.ReadRecipe(OnOurSite);

        Assert.That(browser.Requested, Has.Count.EqualTo(1));
        Assert.That(library.Kept.Single().Title, Is.EqualTo("First copy"));
    }

    /// <summary>A library that's down costs a read of a site nothing: the cook gets the page.</summary>
    [Test]
    public async Task ALibraryThatIsDown_CostsASiteReadNothing()
    {
        var (tools, browser) = Over(new FakeRecipeLibrary(fails: true), DataPage);

        Assert.That(await tools.ReadRecipe(OnOurSite), Does.Contain("1 kg mince"));
        Assert.That(browser.Requested, Has.Count.EqualTo(1));
    }

    /// <summary>Our link to a recipe we can't get says so, so the cook takes another hit; no browser opens.</summary>
    [TestCase(true)]
    [TestCase(false)]
    public async Task OurLinkThatCantBeRead_SaysSo(bool down)
    {
        var library = new FakeRecipeLibrary(fails: down);
        var (tools, browser) = Over(library, DataPage);
        var gone = library.AddressOf(KeptRecipes.Of(OnOurSite));

        string print = await tools.ReadRecipe(gone.AbsoluteUri);

        Assert.That(print, Does.Contain("another hit"));
        Assert.That(browser.Requested, Is.Empty);
    }

    /// <summary>While it's told to skip, as an eval's trial is, the library neither answers nor keeps.</summary>
    [Test]
    public async Task WhenToldToSkip_ItNeitherAnswersNorKeeps()
    {
        var library = new FakeRecipeLibrary();
        library.Kept.Add(KeptRecipes.Of(OnOurSite, name: "Stored"));
        var (tools, browser) = Over(library, DataPage, skip: () => true);

        string ours = await tools.ReadRecipe(library.AddressOf(library.Kept.Single()).AbsoluteUri);
        await tools.ReadRecipe(new Uri(Taste.Home, "recipes/another/").AbsoluteUri);

        Assert.That(ours, Does.Not.Contain("Stored"));
        Assert.That(browser.Requested, Has.Count.EqualTo(1));
        Assert.That(library.Kept, Has.Count.EqualTo(1));
    }
}
