using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Gathering;
using LouisAgent.Packs.Recipes.Sources.Sites;
using LouisAgent.Packs.Recipes.Tests.Library;
using LouisAgent.Packs.Recipes.Tests.Tools;
using LouisAgent.Packs.Recipes.Tests.Tools.Reading;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Recipes.Tests.Sources.Gathering;

/// <summary>
/// The library is one more voice in a search, and the sites are still asked every time. That's Louis's rule: "we must
/// still make sure we get new recipes, like having different types of milktart".
/// </summary>
public class RecipeScatterLibraryTests
{
    private static readonly string[] Slugs =
        ["taste", "foodandhome", "bibbyskitchenat36", "tantalisemytastebuds", "wandercapetown", "thecinnamonjar"];

    /// <summary>A site with one new page on melktert.</summary>
    private static FakeRecipeSource Site(string slug) =>
        new(slug, [new RecipeHit($"{slug} melktert", new Uri($"https://example.com/{slug}/melktert"))]);

    /// <summary>A kept melktert on a site.</summary>
    private static StoredRecipe Kept(string slug, string name) =>
        KeptRecipes.Of($"https://example.com/kept/{name}", slug, $"{name} melktert");

    /// <summary>The scatter over these sites and a library holding these.</summary>
    private static RecipeScatter Scatter(IEnumerable<IRecipeSource> sites, FakeRecipeLibrary library, int seed = 1) =>
        new([.. sites], new Random(seed), new RecipeShelf(library, library, [.. RecipeSites.All], new FakeTimeProvider()));

    /// <summary>However full the library is, four sites are still asked, so a new melktert can turn up.</summary>
    [Test]
    public async Task TheSites_AreStillAsked_HoweverFullTheLibrary()
    {
        var library = new FakeRecipeLibrary();
        library.Kept.AddRange(Enumerable.Range(1, 30).Select(i => Kept("taste", $"kept{i}")));
        var sites = Slugs.Select(Site).ToList();

        var gather = await Scatter(sites, library).FindAsync("melktert", 8);

        Assert.That(gather.Asked, Has.Count.EqualTo(4));
        Assert.That(sites.Count(site => site.Query == "melktert"), Is.EqualTo(4));
        Assert.That(gather.Hits.Count(hit => hit.Hit.Url.Host == "library.example"),
            Is.LessThan(8), "new pages keep a place beside what we hold");
    }

    /// <summary>A kept page takes its turn like a site's, links to our API, and names the site it came from.</summary>
    [Test]
    public async Task AKeptPage_TakesItsTurn_WithOurLink_AndNamesItsSite()
    {
        var library = new FakeRecipeLibrary();
        var kept = Kept("foodandhome", "kept");
        library.Kept.Add(kept);

        var gather = await Scatter(Slugs.Take(1).Select(Site), library).FindAsync("melktert", 8);

        var hit = gather.Hits.Single(found => found.Hit.Url == library.AddressOf(kept));
        Assert.That(hit.Site.Slug, Is.EqualTo("foodandhome"));
        Assert.That(hit.Hit.Name, Is.EqualTo("kept melktert"));
        Assert.That(gather.AskedLibrary, Is.True);
    }

    /// <summary>A page we hold that a site also lists appears once, however spelled, and is read from us.</summary>
    [Test]
    public async Task APageFromBoth_AppearsOnce_WithOurLink()
    {
        var library = new FakeRecipeLibrary();
        var kept = KeptRecipes.Of("https://www.example.com/taste/melktert/", "taste", "melktert");
        library.Kept.Add(kept);

        var gather = await Scatter([Site("taste")], library).FindAsync("melktert", 8);

        Assert.That(gather.Hits.Select(hit => hit.Hit.Url), Is.EqualTo(new[] { library.AddressOf(kept) }));
    }

    /// <summary>Every kept page gets its chance, or the library would freeze on its first few.</summary>
    [Test]
    public async Task EveryKeptPage_GetsAChance()
    {
        var library = new FakeRecipeLibrary();
        library.Kept.AddRange(Enumerable.Range(1, 20).Select(i => Kept("taste", $"kept{i}")));
        var seen = new HashSet<Uri>();

        for (int seed = 0; seed < 40; seed++)
            foreach (var hit in (await Scatter([], library, seed).FindAsync("melktert", 3)).Hits)
                seen.Add(hit.Hit.Url);

        Assert.That(seen, Has.Count.GreaterThan(15));
    }

    /// <summary>A kept page from a site since taken out of the table isn't offered.</summary>
    [Test]
    public async Task AKeptPageFromAnUnknownSite_IsNotOffered()
    {
        var library = new FakeRecipeLibrary();
        library.Kept.Add(Kept("no-such-site", "gone"));

        var gather = await Scatter([Site("taste")], library).FindAsync("melktert", 8);

        Assert.That(gather.Hits.Select(hit => hit.Site.Slug), Is.EqualTo(new[] { "taste" }));
    }

    /// <summary>A library that's down costs the search nothing.</summary>
    [Test]
    public async Task ALibraryThatIsDown_CostsTheSearchNothing()
    {
        var gather = await Scatter([Site("taste")], new FakeRecipeLibrary(fails: true)).FindAsync("melktert", 8);

        Assert.That(gather.Hits, Has.Count.EqualTo(1));
    }

    /// <summary>With no library it works as it always did, and says it asked only the sites.</summary>
    [Test]
    public async Task WithNoLibrary_ItWorksAsBefore()
    {
        var gather = await new RecipeScatter([Site("taste")], new Random(1)).FindAsync("melktert", 8);

        Assert.That(gather.Hits, Has.Count.EqualTo(1));
        Assert.That(gather.AskedLibrary, Is.False);
    }
}
