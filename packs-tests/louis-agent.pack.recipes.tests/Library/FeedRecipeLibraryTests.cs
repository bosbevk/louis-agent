using System.Net;
using LouisAgent.Packs.Recipes.Library;
using LouisAgent.Packs.Recipes.Library.Feed;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Packs.Recipes.Tests.Library;

/// <summary>
/// Our cook reads the library the way anyone would: over HTTP, from the open feed, with no key. It holds what it read
/// for five minutes, finds pages by their one spelling, and searches with the sites' own word rule. If the feed is down
/// it has nothing to offer, and nothing breaks.
/// </summary>
public class FeedRecipeLibraryTests
{
    private static readonly Uri Api = new("http://127.0.0.1:8080/");

    /// <summary>The feed's reply holding these recipes.</summary>
    private static string Feed(params (string Url, string Title)[] recipes) => new RecipeFeedReply("notice",
    [
        .. recipes.Select(recipe => new RecipeFeedRow(recipe.Url, "taste", "TASTE", recipe.Title, "print of " + recipe.Title,
            DateTimeOffset.UnixEpoch))
    ]).Write();

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

    /// <summary>A page is found however the link spells it, and a page we don't hold isn't.</summary>
    [Test]
    public async Task APage_IsFound_HoweverItsSpelled()
    {
        var (library, _, _) = Over(Feed(("https://www.taste.co.za/recipes/bobotie/", "Bobotie")));

        var found = await library.FindAsync(new Uri("http://taste.co.za/recipes/bobotie"), default);

        Assert.That(found?.Print, Is.EqualTo("print of Bobotie"));
        Assert.That(await library.FindAsync(new Uri("https://taste.co.za/recipes/other"), default), Is.Null);
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
        await library.FindAsync(new Uri("https://taste.co.za/x"), default);
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
        Assert.That(await library.FindAsync(new Uri("https://taste.co.za/x"), default), Is.Null);
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
