using System.Net;
using LouisAgent.Packs.Recipes.Library.Feed;
using LouisAgent.Packs.Recipes.Sources.Sites;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Recipes.Tests.Library;

/// <summary>
/// Our cook reads the library the way anyone would: over HTTP, from the open feed, with no key. A search holds the list
/// for five minutes and uses the sites' own word rule; a read asks for one recipe at its own address. If the feed is
/// down a search has nothing to offer, and nothing breaks.
/// </summary>
public class FeedRecipeLibraryTests
{
    private static readonly Uri Api = new("http://127.0.0.1:8080/");
    private static readonly RecipeSite Taste = RecipeSites.Find("taste")!;

    /// <summary>A row for a recipe on this page with this title.</summary>
    private static RecipeFeedRow Row(string url, string title) => RecipeFeedRow.Of(KeptRecipes.Of(url, name: title), Taste);

    /// <summary>The feed's reply holding these recipes.</summary>
    private static string Feed(params (string Url, string Title)[] recipes) =>
        new RecipeFeedReply("notice", [.. recipes.Select(recipe => Row(recipe.Url, recipe.Title))]).Write();

    /// <summary>The library over a feed that answers with this, and the handler to see what was asked.</summary>
    private static (FeedRecipeLibrary Library, StubHandler Handler, FakeTimeProvider Clock) Over(string json)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        return (new FeedRecipeLibrary(new HttpClient(handler), Api, clock), handler, clock);
    }

    /// <summary>It asks the open feed's own address, and sends no key.</summary>
    [Test]
    public async Task ItReadsTheOpenFeed()
    {
        var (library, handler, _) = Over(Feed());

        await library.SearchAsync("bobotie", 8, default);

        Assert.That(handler.Requests.Single().Url, Is.EqualTo("http://127.0.0.1:8080/v1/feeds/recipes"));
    }

    /// <summary>A recipe's address is on our API, and the id comes back out of it.</summary>
    [Test]
    public void ARecipesAddress_IsOnOurApi()
    {
        var (library, _, _) = Over(Feed());
        var recipe = KeptRecipes.Of("https://taste.co.za/recipes/bobotie/");

        var address = library.AddressOf(recipe);

        Assert.That(address.AbsoluteUri, Is.EqualTo($"http://127.0.0.1:8080/v1/feeds/recipes/{recipe.Id}"));
        Assert.That(library.IdIn(address), Is.EqualTo(recipe.Id));
    }

    /// <summary>
    /// The model hands us an address, so only our own recipes path with a real id counts: anything else goes through
    /// the browser's guards, and never gets a request from here.
    /// </summary>
    [TestCase("https://taste.co.za/recipes/bobotie")]
    [TestCase("http://127.0.0.1:9999/v1/feeds/recipes/0123456789abcdef")]
    [TestCase("http://127.0.0.1:8080/v1/feeds/shop-prices/0123456789abcdef")]
    [TestCase("http://127.0.0.1:8080/v1/feeds/recipes/")]
    [TestCase("http://127.0.0.1:8080/v1/feeds/recipes/0123456789abcdef/more")]
    [TestCase("http://127.0.0.1:8080/v1/feeds/recipes/0123456789abcdef?x=1")]
    [TestCase("http://127.0.0.1:8080/v1/feeds/recipes/not-an-id-at-all")]
    public void AnyOtherAddress_IsNotOurs(string address)
    {
        var (library, _, _) = Over(Feed());

        Assert.That(library.IdIn(new Uri(address)), Is.Null);
    }

    /// <summary>A read asks for that one recipe, every time, so a page kept a moment ago can be read.</summary>
    [Test]
    public async Task ARead_AsksForTheOneRecipe()
    {
        var row = Row("https://taste.co.za/recipes/bobotie/", "Bobotie");
        var (library, handler, _) = Over(row.Write());

        var first = await library.ReadAsync(row.Id, default);
        await library.ReadAsync(row.Id, default);

        Assert.That(first?.Title, Is.EqualTo("Bobotie"));
        Assert.That(first?.Page, Is.EqualTo(new Uri("https://taste.co.za/recipes/bobotie/")));
        Assert.That(handler.Requests.Select(request => request.Url),
            Is.EqualTo(Enumerable.Repeat($"http://127.0.0.1:8080/v1/feeds/recipes/{row.Id}", 2)));
    }

    /// <summary>A recipe the feed doesn't hold is nothing to read.</summary>
    [Test]
    public async Task ARecipeTheFeedDoesntHold_IsNull()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var library = new FeedRecipeLibrary(new HttpClient(handler), Api, new FakeTimeProvider());

        Assert.That(await library.ReadAsync("0123456789abcdef", default), Is.Null);
    }

    /// <summary>A search uses the sites' own rule, so a plural and a dish's other name both find it.</summary>
    [TestCase("melktert", "Traditional milk tart")]
    [TestCase("koeksister", "Koeksisters")]
    public async Task ASearch_UsesTheSitesWordRule(string query, string title)
    {
        var (library, _, _) = Over(Feed(("https://taste.co.za/x", title), ("https://taste.co.za/y", "Apple crumble")));

        var hits = await library.SearchAsync(query, 8, default);

        Assert.That(hits.Select(hit => hit.Title), Is.EqualTo(new[] { title }));
    }

    /// <summary>A search gives at most what was asked for.</summary>
    [Test]
    public async Task ASearch_GivesAtMostWhatWasAsked()
    {
        var (library, _, _) = Over(Feed(("https://taste.co.za/a", "Bobotie one"), ("https://taste.co.za/b", "Bobotie two")));

        Assert.That(await library.SearchAsync("bobotie", 1, default), Has.Count.EqualTo(1));
    }

    /// <summary>It reads the feed once in five minutes, however many searches come, then reads it again.</summary>
    [Test]
    public async Task TheFeed_IsHeldForFiveMinutes()
    {
        var (library, handler, clock) = Over(Feed());

        await library.SearchAsync("bobotie", 8, default);
        await library.SearchAsync("melktert", 8, default);
        clock.Advance(TimeSpan.FromMinutes(4));
        await library.SearchAsync("bobotie", 8, default);
        Assert.That(handler.Requests, Has.Count.EqualTo(1));

        clock.Advance(TimeSpan.FromMinutes(2));
        await library.SearchAsync("bobotie", 8, default);
        Assert.That(handler.Requests, Has.Count.EqualTo(2));
    }

    /// <summary>A feed that's down or says something odd has nothing to offer, and costs a search nothing.</summary>
    [TestCase(HttpStatusCode.InternalServerError, "oops")]
    [TestCase(HttpStatusCode.OK, "not json")]
    public async Task AFeedThatFails_HasNothingToOffer(HttpStatusCode status, string body)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(status) { Content = new StringContent(body) });
        var library = new FeedRecipeLibrary(new HttpClient(handler), Api, new FakeTimeProvider());

        Assert.That(await library.SearchAsync("bobotie", 8, default), Is.Empty);
    }

    /// <summary>After a failure it doesn't try on every call, nor wait the full five minutes.</summary>
    [Test]
    public async Task AfterAFailure_ItTriesAgainSoon()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var clock = new FakeTimeProvider(DateTimeOffset.UnixEpoch);
        var library = new FeedRecipeLibrary(new HttpClient(handler), Api, clock);

        await library.SearchAsync("bobotie", 8, default);
        await library.SearchAsync("bobotie", 8, default);
        Assert.That(handler.Requests, Has.Count.EqualTo(1));

        clock.Advance(TimeSpan.FromMinutes(2));
        await library.SearchAsync("bobotie", 8, default);
        Assert.That(handler.Requests, Has.Count.EqualTo(2));
    }
}
