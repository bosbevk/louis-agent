using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Excluded;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Sites;
using LouisAgent.Packs.Recipes.Tests.Providers;
using LouisAgent.Packs.Recipes.Tools;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Recipes.Tests.Tools.Reading;

/// <summary>
/// A page we opened and found no recipe on is never opened again: a read refuses it, and search leaves it out. Only a
/// page we truly read counts, so a bad minute can't hide a good recipe for good.
/// </summary>
public class ExcludedPagesTests
{
    private static readonly RecipeSite Taste = RecipeSites.Find("taste")!;
    private static readonly string Article = new Uri(Taste.Home, "not-a-recipe/").AbsoluteUri;
    private const string ArticlePage = "<html><head><title>Ideas</title></head><body><main><h1>Ideas</h1><p>Try a stew with boerewors some night.</p></main></body></html>";
    private const string HeadedPage = "<html><head><title>Pie</title></head><body><main><h1>Pie</h1><h2>Ingredients</h2><ul><li>1 egg</li><li>1 cup flour</li></ul><h2>Method</h2><ul><li>Bake it.</li></ul></main></body></html>";

    /// <summary>The list in memory, or failing the way a server that's down does.</summary>
    private sealed class FakeList(bool fails = false) : IExcludedPages
    {
        public List<ExcludedPage> Pages { get; } = [];

        public Task<IReadOnlySet<string>> IdsAsync(CancellationToken cancellationToken) => fails
            ? throw new TimeoutException("no Mongo")
            : Task.FromResult<IReadOnlySet<string>>(Pages.Select(page => page.Id).ToHashSet());

        public Task AddAsync(ExcludedPage page, CancellationToken cancellationToken)
        {
            if (fails) throw new TimeoutException("no Mongo");
            Pages.Add(page);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ExcludedPage>> AllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ExcludedPage>>(Pages);

        public Task<bool> RemoveAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult(Pages.RemoveAll(page => page.Id == id) > 0);
    }

    /// <summary>The tools over a browser with this page, this list and these sources.</summary>
    private static (RecipeTools Tools, FakeSiteBrowser Browser) Over(FakeList list, string page, Func<bool>? skip = null,
        Dictionary<string, IRecipeSource>? sources = null)
    {
        var browser = new FakeSiteBrowser(page);
        var library = new FakeRecipeLibrary();
        var shelf = new RecipeShelf(library, library, [Taste], new FakeTimeProvider(DateTimeOffset.UnixEpoch), skip, list);
        return (new RecipeTools(sources ?? new Dictionary<string, IRecipeSource>(), browser, new FakePhotoFetcher(), shelf), browser);
    }

    /// <summary>A page with no recipe is refused, telling the cook to move on, and goes on the list.</summary>
    [Test]
    public async Task APageWithNoRecipe_IsRefused_AndListed()
    {
        var list = new FakeList();

        string print = await Over(list, ArticlePage).Tools.ReadRecipe(Article);

        Assert.That(print, Does.Contain("Read another hit").And.Not.Contain("Try a stew"));
        Assert.That(list.Pages.Single().Page, Is.EqualTo(new Uri(Article)));
    }

    /// <summary>
    /// A page off our sites is refused too, but never listed: <c>ReadRecipe</c> reads any public link, a user's own
    /// included, and only our sites' pages are written down, as for the library (ADR 0082).
    /// </summary>
    [Test]
    public async Task APageOffOurSites_IsRefused_ButNotListed()
    {
        var list = new FakeList();

        string print = await Over(list, ArticlePage).Tools.ReadRecipe("https://example.com/someones-own-page/");

        Assert.That(print, Does.Contain("Read another hit"));
        Assert.That(list.Pages, Is.Empty);
    }

    /// <summary>Once it's listed nobody opens it again, however its link is spelled.</summary>
    [Test]
    public async Task AListedPage_OpensNoBrowser()
    {
        var list = new FakeList();
        list.Pages.Add(new ExcludedPage(new Uri(Article), DateTimeOffset.UnixEpoch));
        var (tools, browser) = Over(list, ArticlePage);

        string print = await tools.ReadRecipe(Article.Replace("https://", "http://").TrimEnd('/'));

        Assert.That(print, Does.Contain("Read another hit"));
        Assert.That(browser.Requested, Is.Empty);
    }

    /// <summary>A search over several sites leaves a listed page out, and its place goes to another.</summary>
    [Test]
    public async Task ASearch_LeavesAListedPageOut()
    {
        var list = new FakeList();
        list.Pages.Add(new ExcludedPage(new Uri(Article), DateTimeOffset.UnixEpoch));
        var site = new FakeRecipeSource("foodandhome",
            [new RecipeHit("Pie ideas", new Uri(Article)), new RecipeHit("Pie", new Uri(Taste.Home, "pie/"))]);

        string result = await Over(list, ArticlePage, sources: new Dictionary<string, IRecipeSource> { ["foodandhome"] = site }).Tools.FindRecipes("Pie");

        Assert.That(result, Does.Not.Contain("not-a-recipe").And.Contain("/pie/"));
    }

    /// <summary>A search that names its site leaves it out too.</summary>
    [Test]
    public async Task ASearchNamingASite_LeavesAListedPageOut()
    {
        var list = new FakeList();
        list.Pages.Add(new ExcludedPage(new Uri(Article), DateTimeOffset.UnixEpoch));
        var site = new FakeRecipeSource("foodandhome",
            [new RecipeHit("Pie ideas", new Uri(Article)), new RecipeHit("Pie", new Uri(Taste.Home, "pie/"))]);

        string result = await Over(list, ArticlePage, sources: new Dictionary<string, IRecipeSource> { ["foodandhome"] = site }).Tools.FindRecipes("Pie", "foodandhome");

        Assert.That(result, Does.Not.Contain("not-a-recipe").And.Contain("/pie/"));
    }

    /// <summary>An excluded page costs no place: the site is asked for extra, so the list still comes to ten.</summary>
    [Test]
    public async Task ASearchNamingASite_StillGivesTen()
    {
        var list = new FakeList();
        var hits = Enumerable.Range(1, 14).Select(i => new RecipeHit($"Pie {i}", new Uri(Taste.Home, $"pie-{i}/"))).ToList();
        list.Pages.Add(new ExcludedPage(hits[0].Url, DateTimeOffset.UnixEpoch));
        list.Pages.Add(new ExcludedPage(hits[1].Url, DateTimeOffset.UnixEpoch));
        var site = new FakeRecipeSource("foodandhome", hits);

        string result = await Over(list, ArticlePage, sources: new Dictionary<string, IRecipeSource> { ["foodandhome"] = site }).Tools.FindRecipes("Pie", "foodandhome");

        Assert.That(site.MaxResults, Is.EqualTo(12));
        Assert.That(result, Does.Contain("10. Pie 12").And.Not.Contain("Pie 13").And.Not.Contain("/pie-1/"));
    }

    /// <summary>A page whose headings show a recipe is read and never listed.</summary>
    [Test]
    public async Task APageWithARecipe_IsReadAndNotListed()
    {
        var list = new FakeList();

        string print = await Over(list, HeadedPage).Tools.ReadRecipe(Article);

        Assert.That(print, Does.Contain("1 egg"));
        Assert.That(list.Pages, Is.Empty);
    }

    /// <summary>An empty page is more likely our trouble than the page's, so it stays off the list.</summary>
    [Test]
    public async Task AnEmptyPage_IsNotListed()
    {
        var list = new FakeList();

        await Over(list, "<html><body></body></html>").Tools.ReadRecipe(Article);

        Assert.That(list.Pages, Is.Empty);
    }

    /// <summary>A refusal says nothing about the page, so it never goes on the list.</summary>
    [Test]
    public async Task ARefusedVisit_IsNotListed()
    {
        var list = new FakeList();
        var library = new FakeRecipeLibrary();
        var browser = new FakeSiteBrowser(fails: new WebBrowserException("robots.txt says no"));
        var shelf = new RecipeShelf(library, library, [Taste], new FakeTimeProvider(), null, list);

        await new RecipeTools(new Dictionary<string, IRecipeSource>(), browser, new FakePhotoFetcher(), shelf).ReadRecipe(Article);

        Assert.That(list.Pages, Is.Empty);
    }

    /// <summary>A list that's down costs nothing: the page is looked at, and the cook still gets its answer.</summary>
    [Test]
    public async Task AListThatIsDown_CostsNothing()
    {
        var (tools, browser) = Over(new FakeList(fails: true), ArticlePage);

        string print = await tools.ReadRecipe(Article);

        Assert.That(print, Does.Contain("Read another hit"));
        Assert.That(browser.Requested, Has.Count.EqualTo(1));
    }

    /// <summary>A trial that leaves the library alone leaves the list too, so one run can't change the next.</summary>
    [Test]
    public async Task WhenToldToSkip_TheListIsLeftAlone()
    {
        var list = new FakeList();

        await Over(list, ArticlePage, skip: () => true).Tools.ReadRecipe(Article);

        Assert.That(list.Pages, Is.Empty);
    }
}
