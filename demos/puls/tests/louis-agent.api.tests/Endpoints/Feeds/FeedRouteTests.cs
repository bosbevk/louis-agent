using System.Net;
using System.Text.Json;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Testing;
using LouisAgent.Choices;
using LouisAgent.Packs.ShopPrices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Feeds;

/// <summary>
/// The packs' feeds over the API (ADR 0070): the list and each feed are open, so anyone reads them without the key;
/// the API serves the pack's JSON as it is, and a name no pack publishes is the API's 404. The test host carries
/// Puls's packs, so the shop-prices feed is there.
/// </summary>
public class FeedRouteTests
{
    /// <summary>The API, gated by a key the requests never send.</summary>
    private static async Task<(WebApplication App, HttpClient Client)> StartAsync()
    {
        var app = ApiTestHost.Build(apiKey: "secret");
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
            .Single(f => f.GetProperty("name").GetString() == ShopPricesToolset.ShopPrices.Wire());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(feed.GetProperty("description").GetString(), Does.Contain("Stats SA"));
        Assert.That(feed.GetProperty("path").GetString(), Is.EqualTo($"/v1/feeds/{ShopPricesToolset.ShopPrices.Wire()}"));
    }

    /// <summary>A feed is the pack's JSON as the pack wrote it, without the key.</summary>
    [Test]
    public async Task AFeed_IsOpen_AndIsThePacksJson()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await client.GetAsync($"/v1/feeds/{ShopPricesToolset.ShopPrices.Wire()}");
        using var reply = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        Assert.That(reply.RootElement.GetProperty("notice").GetString(), Does.StartWith("Average prices"));
        Assert.That(reply.RootElement.GetProperty("tables")[0].GetProperty("source").GetString(), Does.StartWith("Stats SA"));
    }

    /// <summary>An item's address is open too; a feed with no such item is the API's 404, naming both.</summary>
    [Test]
    public async Task AFeedThatHasNoSuchItem_IsTheApisNotFound_WithoutTheKey()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var response = await client.GetAsync($"/v1/feeds/{ShopPricesToolset.ShopPrices.Wire()}/no-such-thing");
        using var reply = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var error = reply.RootElement.GetProperty("error");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(error.GetProperty("type").GetString(), Is.EqualTo(ApiErrorKind.NotFound.Wire()));
        Assert.That(error.GetProperty("message").GetString(),
            Does.Contain(ShopPricesToolset.ShopPrices.Wire()).And.Contain("no-such-thing"));
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
