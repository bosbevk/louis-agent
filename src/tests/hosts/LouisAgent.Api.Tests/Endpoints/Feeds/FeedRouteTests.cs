using System.Net;
using System.Text.Json;
using LouisAgent.Api.Endpoints;
using LouisAgent.Choices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Feeds;

/// <summary>
/// The packs' feeds over the API (ADR 0070): the list and each feed are open, so anyone reads them without the key;
/// the API serves the pack's JSON as it is, and a name no pack publishes is the API's 404. The feed is the framework's
/// fake (F166.1); what a real pack's feed holds is that pack's own tests'.
/// </summary>
public class FeedRouteTests
{
    /// <summary>The feed the host's one fake pack publishes.</summary>
    private const string Feed = "test-prices";

    /// <summary>The JSON the fake feed was given, which the API must serve as it is.</summary>
    private const string FeedJson = """{"notice":"Average prices","tables":[{"source":"A test"}]}""";

    /// <summary>The API, gated by a key the requests never send, with a pack that publishes the fake feed.</summary>
    private static async Task<(WebApplication App, HttpClient Client)> StartAsync()
    {
        var feeds = new FakePack("feeds", feeds: [new FakeFeed(Feed, FeedJson)]);
        var app = ApiTestHost.Build(apiKey: "secret", morePacks: [feeds]);
        await app.StartAsync();
        return (app, app.GetTestClient());
    }

    /// <summary>The list names each feed, what it is, and where to read it, without the key.</summary>
    [Test]
    public async Task TheList_IsOpen_AndNamesEachFeed()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await client.GetAsync("/v1/feeds");
        using var reply = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var feed = reply.RootElement.GetProperty("feeds").EnumerateArray()
            .Single(f => f.GetProperty("name").GetString() == Feed);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(feed.GetProperty("description").GetString(), Is.EqualTo($"The {Feed} feed"));
        Assert.That(feed.GetProperty("path").GetString(), Is.EqualTo($"/v1/feeds/{Feed}"));
    }

    /// <summary>A feed is the pack's JSON as the pack wrote it, without the key.</summary>
    [Test]
    public async Task AFeed_IsOpen_AndIsThePacksJson()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await client.GetAsync($"/v1/feeds/{Feed}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(FeedJson));
    }

    /// <summary>An item's address is open too; a feed with no such item is the API's 404, naming both.</summary>
    [Test]
    public async Task AFeedThatHasNoSuchItem_IsTheApisNotFound_WithoutTheKey()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await client.GetAsync($"/v1/feeds/{Feed}/no-such-thing");
        using var reply = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = reply.RootElement.GetProperty("error");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(error.GetProperty("type").GetString(), Is.EqualTo(ApiErrorKind.NotFound.Wire()));
        Assert.That(error.GetProperty("message").GetString(), Does.Contain(Feed).And.Contain("no-such-thing"));
    }

    /// <summary>An item of a feed no pack publishes is the same 404 as the feed itself.</summary>
    [Test]
    public async Task AnItemOfAnUnknownFeed_IsTheApisNotFound()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await client.GetAsync("/v1/feeds/rent-prices/one");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>A name no pack publishes is the API's 404, naming it.</summary>
    [Test]
    public async Task AnUnknownFeed_IsTheApisNotFound()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await client.GetAsync("/v1/feeds/rent-prices");
        using var reply = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = reply.RootElement.GetProperty("error");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(error.GetProperty("type").GetString(), Is.EqualTo(ApiErrorKind.NotFound.Wire()));
        Assert.That(error.GetProperty("message").GetString(), Does.Contain("rent-prices").And.Contain("/v1/feeds"));
    }
}
