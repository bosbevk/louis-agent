using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Gathering;
using LouisAgent.Packs.Recipes.Sources.Sites;
using LouisAgent.Packs.Recipes.Tests.Tools;

namespace LouisAgent.Packs.Recipes.Tests.Sources.Gathering;

/// <summary>
/// One search goes to a few sites picked at random, all at once, and their hits come back in a random order, so no site
/// gets to be the first answer every time and one failing site doesn't sink the search.
/// </summary>
public class RecipeScatterTests
{
    private static readonly string[] Slugs =
        ["taste", "foodandhome", "bibbyskitchenat36", "tantalisemytastebuds", "wandercapetown", "thecinnamonjar"];

    /// <summary>A hit on a site, named so a test can tell where it came from.</summary>
    private static RecipeHit Hit(string slug, int number) =>
        new($"{slug} {number}", new Uri($"https://example.com/{slug}/{number}"));

    /// <summary>A site with this many hits.</summary>
    private static FakeRecipeSource Site(string slug, int hits = 1) =>
        new(slug, [.. Enumerable.Range(1, hits).Select(number => Hit(slug, number))]);

    /// <summary>Six sites, four asked: the words reach those four, and the other two are left alone.</summary>
    [Test]
    public async Task FourSites_AreAsked()
    {
        var sites = Slugs.Select(slug => Site(slug)).ToList();

        var gather = await new RecipeScatter(sites, new Random(1)).FindAsync("bobotie", 8);

        Assert.That(gather.Asked, Has.Count.EqualTo(4));
        Assert.That(sites.Where(site => site.Query == "bobotie").Select(site => site.Site),
            Is.EquivalentTo(gather.Asked));
    }

    /// <summary>With fewer than four sites turned on, all of them are asked.</summary>
    [Test]
    public async Task FewerThanFour_AreAllAsked()
    {
        var sites = Slugs.Take(2).Select(slug => Site(slug)).ToList();

        var gather = await new RecipeScatter(sites, new Random(1)).FindAsync("bobotie", 8);

        Assert.That(gather.Asked, Has.Count.EqualTo(2));
    }

    /// <summary>No site in the draw is an empty answer, not a failure: the tool says what to do about it.</summary>
    [Test]
    public async Task NoSites_IsAnEmptyGather()
    {
        var gather = await new RecipeScatter([], new Random(1)).FindAsync("bobotie", 8);

        Assert.That(gather.Asked, Is.Empty);
        Assert.That(gather.Hits, Is.Empty);
    }

    /// <summary>The same seed draws the same sites, so a test can repeat.</summary>
    [Test]
    public async Task TheSameSeed_DrawsTheSameSites()
    {
        var sites = Slugs.Select(slug => Site(slug)).ToList();

        var first = await new RecipeScatter(sites, new Random(7)).FindAsync("bobotie", 8);
        var second = await new RecipeScatter(sites, new Random(7)).FindAsync("bobotie", 8);

        Assert.That(second.Asked, Is.EqualTo(first.Asked));
    }

    /// <summary>
    /// Many searches reach every site, and each site gets the first place sometimes: this is the bug, one site always
    /// coming first, as a test.
    /// </summary>
    [Test]
    public async Task ManySearches_ReachEverySite_AndNoSiteAlwaysComesFirst()
    {
        var sites = Slugs.Select(slug => Site(slug)).ToList();
        var random = new Random(1);
        var firsts = new HashSet<string>();
        var asked = new HashSet<string>();

        foreach (int _ in Enumerable.Range(0, 100))
        {
            var gather = await new RecipeScatter(sites, random).FindAsync("bobotie", 8);
            firsts.Add(gather.Hits[0].Site.Slug);
            asked.UnionWith(gather.Asked.Select(site => site.Slug));
        }

        Assert.That(asked, Is.EquivalentTo(Slugs));
        Assert.That(firsts, Is.EquivalentTo(Slugs));
    }

    /// <summary>
    /// The list is in a random order, whole: the first place can go to any hit of any site, and two hits of one site
    /// can sit together. The cook tends to read from the top, so the top mustn't favour anyone.
    /// </summary>
    [Test]
    public async Task TheFirstPlace_CanGoToAnyHit()
    {
        var sites = new[] { Site("taste", hits: 6) };
        var random = new Random(1);
        var firsts = new HashSet<string>();

        foreach (int _ in Enumerable.Range(0, 100))
            firsts.Add((await new RecipeScatter(sites, random).FindAsync("bobotie", 8)).Hits[0].Hit.Name);

        Assert.That(firsts, Has.Count.EqualTo(6));
    }

    /// <summary>Which hits make the list is random too: a page far down a site's list gets its chance.</summary>
    [Test]
    public async Task WhichHitsMakeTheList_IsRandomToo()
    {
        var sites = new[] { Site("taste", hits: 20) };
        var random = new Random(1);
        var shown = new HashSet<string>();

        foreach (int _ in Enumerable.Range(0, 100))
            shown.UnionWith((await new RecipeScatter(sites, random).FindAsync("bobotie", 8)).Hits.Select(hit => hit.Hit.Name));

        Assert.That(shown, Has.Count.EqualTo(20));
    }

    /// <summary>The sites a search has already asked for this dish aren't asked again, by either name.</summary>
    [Test]
    public async Task TheSitesToSkip_AreNotAsked_ByNameOrShortName()
    {
        var sites = Slugs.Select(slug => Site(slug)).ToList();

        var gather = await new RecipeScatter(sites, new Random(1))
            .FindAsync("bobotie", 8, skip: ["TASTE", "Food & Home"]);

        Assert.That(gather.Asked.Select(site => site.Slug), Has.None.EqualTo("taste").And.None.EqualTo("foodandhome"));
        Assert.That(gather.Asked, Has.Count.EqualTo(4));
    }

    /// <summary>With too few sites left it asks those; a skip can't leave a search with nowhere to look.</summary>
    [Test]
    public async Task SkippingEverySite_AsksThemAllAgain_NoSiteIsLeftOutForGood()
    {
        var sites = Slugs.Take(2).Select(slug => Site(slug)).ToList();

        var gather = await new RecipeScatter(sites, new Random(1)).FindAsync("bobotie", 8, skip: [.. Slugs]);

        Assert.That(gather.Asked, Has.Count.EqualTo(2));
    }

    /// <summary>A site that has fewer hits just has fewer in the list.</summary>
    [Test]
    public async Task ASiteWithFewerHits_JustHasFewerInTheList()
    {
        var sites = new[] { Site("taste", hits: 1), Site("foodandhome", hits: 3) };

        var gather = await new RecipeScatter(sites, new Random(1)).FindAsync("bobotie", 8);

        Assert.That(gather.Hits, Has.Count.EqualTo(4));
        Assert.That(gather.Hits.Count(hit => hit.Site.Slug == "foodandhome"), Is.EqualTo(3));
    }

    /// <summary>A site that refuses is skipped and the rest are used; its reason is kept in case all refuse.</summary>
    [Test]
    public async Task ARefusingSite_IsSkipped_AndItsReasonKept()
    {
        var refusing = new FakeRecipeSource("taste", fails: new RecipeSourceException("TASTE said no."));
        var sites = new IRecipeSource[] { refusing, Site("foodandhome") };

        var gather = await new RecipeScatter(sites, new Random(1)).FindAsync("bobotie", 8);

        Assert.That(gather.Hits.Select(hit => hit.Site.Slug), Is.EqualTo(new[] { "foodandhome" }));
        Assert.That(gather.Refusals, Is.EqualTo(new[] { "TASTE said no." }));
        Assert.That(gather.Asked, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// A site whose page hangs times out instead of refusing. That sank the whole search, and the cook went to the web
    /// for bobotie when other sites had it.
    /// </summary>
    [Test]
    public async Task ASiteThatTimesOut_IsSkipped_AndTheOthersStillAnswer()
    {
        var hanging = new FakeRecipeSource("taste", fails: new TimeoutException("The operation has timed out."));
        var sites = new IRecipeSource[] { hanging, Site("foodandhome") };

        var gather = await new RecipeScatter(sites, new Random(1)).FindAsync("bobotie", 8);

        Assert.That(gather.Hits.Select(hit => hit.Site.Slug), Is.EqualTo(new[] { "foodandhome" }));
        Assert.That(gather.Refusals, Is.EqualTo(new[] { "The operation has timed out." }));
    }

    /// <summary>
    /// The sites are asked at once: each of these only answers when all four have been asked, so asking them one after
    /// another would never finish.
    /// </summary>
    [Test]
    public async Task TheSites_AreAskedAtOnce()
    {
        var everyoneAsked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int asked = 0;
        var sites = Slugs.Take(4).Select(slug => new WaitingSource(slug, () =>
        {
            if (Interlocked.Increment(ref asked) == 4) everyoneAsked.SetResult();
            return everyoneAsked.Task;
        })).ToList();

        var gather = await new RecipeScatter(sites, new Random(1)).FindAsync("bobotie", 8)
            .WaitAsync(TimeSpan.FromSeconds(10));

        Assert.That(gather.Hits, Has.Count.EqualTo(4));
    }

    /// <summary>A site that waits for something before it answers with one hit.</summary>
    private sealed class WaitingSource(string slug, Func<Task> waitFor) : IRecipeSource
    {
        /// <inheritdoc />
        public RecipeSite Site => RecipeSites.Find(slug)!;

        /// <inheritdoc />
        public async Task<IReadOnlyList<RecipeHit>> FindAsync(string query, int maxResults,
            CancellationToken cancellationToken = default)
        {
            await waitFor();
            return [Hit(slug, 1)];
        }
    }
}
