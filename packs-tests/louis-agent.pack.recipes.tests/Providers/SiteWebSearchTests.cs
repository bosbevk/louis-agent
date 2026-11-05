using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Web.Search;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Web.Search;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Providers;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Tests.Providers;

/// <summary>
/// Checks that a site with no search we may use is searched through the web search provider, asked for that site
/// only, and that what comes back is kept to that site.
/// </summary>
public class SiteWebSearchTests
{
    /// <summary>A search provider that remembers what it was asked and answers with what it was given.</summary>
    private sealed class FakeSearch(IReadOnlyList<WebSearchResult>? results = null, Exception? fails = null)
        : IWebSearchProvider
    {
        /// <summary>The questions it was asked, in order.</summary>
        public List<WebSearchQuery> Asked { get; } = [];

        /// <summary>A name for the logs; nothing here reads it.</summary>
        public string Name => "Fake";

        /// <summary>Notes the question, then answers with its results, or fails the way it was told.</summary>
        public Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query,
            CancellationToken cancellationToken = default)
        {
            Asked.Add(query);
            return fails is null
                ? Task.FromResult(results ?? [])
                : Task.FromException<IReadOnlyList<WebSearchResult>>(fails);
        }
    }

    private static readonly RecipeSite Daley = RecipeSites.Find("thedaleyplate")!;

    /// <summary>The provider is asked for the site only: its host, with no www.</summary>
    [Test]
    public async Task TheQuestion_IsLimitedToTheSitesHostWithoutWww()
    {
        var search = new FakeSearch();

        await new SiteWebSearch(Daley, search).FindAsync("bobotie", 5);

        Assert.That(search.Asked.Single(), Is.EqualTo(new WebSearchQuery("bobotie", 5, Site: "thedaleyplate.com")));
    }

    /// <summary>
    /// Each result on the site becomes a hit with its title and address, in the order the provider gave.
    /// </summary>
    [Test]
    public async Task EachResultOnTheSite_BecomesAHit_InTheProvidersOrder()
    {
        var search = new FakeSearch([
            new WebSearchResult("Chicken livers peri-peri", "https://www.thedaleyplate.com/blog/chicken-livers", "..."),
            new WebSearchResult("Bobotie", "https://thedaleyplate.com/blog/bobotie", "...")
        ]);

        var hits = await new SiteWebSearch(Daley, search).FindAsync("bobotie", 5);

        Assert.That(hits,
            Is.EqualTo(new[]
            {
                new RecipeHit("Chicken livers peri-peri",
                    new Uri("https://www.thedaleyplate.com/blog/chicken-livers")),
                new RecipeHit("Bobotie", new Uri("https://thedaleyplate.com/blog/bobotie"))
            }));
    }

    /// <summary>
    /// The site limit is only a hint, so a result from another host, or with no real address, is dropped.
    /// </summary>
    [Test]
    public async Task AResultOnAnotherHost_IsDropped_TheSiteLimitIsAHintNotAPromise()
    {
        var search = new FakeSearch([
            new WebSearchResult("Elsewhere", "https://other.example.com/bobotie", "..."),
            new WebSearchResult("Not an address", "not a url", "..."),
            new WebSearchResult("Here", "https://www.thedaleyplate.com/blog/bobotie", "...")
        ]);

        var hits = await new SiteWebSearch(Daley, search).FindAsync("bobotie", 5);

        Assert.That(hits.Select(h => h.Name), Is.EqualTo(new[] { "Here" }));
    }

    /// <summary>
    /// Rooi Rose lives in one section of its publisher's site, so the publisher's other sections aren't Rooi Rose,
    /// nor is a section whose name only starts the same way.
    /// </summary>
    [Test]
    public async Task ASiteWithAPathInItsHome_KeepsOnlyResultsBeneathIt()
    {
        var search = new FakeSearch([
            new WebSearchResult("Another title", "https://novanews.co.za/vrouekeur/hoender", "..."),
            new WebSearchResult("Same start", "https://novanews.co.za/rooirose-extra/hoender", "..."),
            new WebSearchResult("Rooi Rose's own", "https://novanews.co.za/rooirose/hoenderpasteitjies/", "..."),
            new WebSearchResult("Its home", "https://www.novanews.co.za/rooirose", "...")
        ]);

        var hits = await new SiteWebSearch(RecipeSites.Find("rooirose")!, search).FindAsync("hoender", 5);

        Assert.Multiple(() =>
        {
            Assert.That(search.Asked.Single().Site, Is.EqualTo("novanews.co.za"));
            Assert.That(hits.Select(h => h.Name), Is.EqualTo(new[] { "Rooi Rose's own", "Its home" }));
        });
    }

    /// <summary>When the provider fails, the cook gets a message that names the site and the reason.</summary>
    [Test]
    public void AProviderThatFails_BecomesAMessageNamingTheSiteAndTheReason()
    {
        var search = new FakeSearch(fails: new WebSearchException("quota used up"));

        var thrown =
            Assert.ThrowsAsync<RecipeSourceException>(() => new SiteWebSearch(Daley, search).FindAsync("a", 5));

        Assert.That(thrown!.Message, Is.EqualTo(MessageText.Format(RecipeToolMessage.SourceRefused,
            new
            {
                Source = Daley.Name,
                Query = "a",
                Reason = "quota used up"
            })));
    }
}
