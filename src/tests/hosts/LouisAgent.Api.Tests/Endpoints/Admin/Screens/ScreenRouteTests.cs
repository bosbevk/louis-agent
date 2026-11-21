using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Composition.Packs.Screens.Reading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.Tests.Endpoints.Admin.Screens;

/// <summary>
/// A pack's screens over the API, behind the admin key: what a pack describes comes back as it said, and the route
/// knows nothing of which pack it came from.
/// </summary>
public class ScreenRouteTests
{
    private const string AdminKey = "admin-secret";

    /// <summary>The API with a pack offering these screens, and a client carrying the admin key.</summary>
    private static async Task<(WebApplication App, HttpClient Client)> StartAsync(params FakeScreen[] screens)
    {
        var app = ApiTestHost.Build(adminKey: AdminKey,
            morePacks: screens.Length == 0 ? null : [new FakePack("screener", toolsets: [], screens: screens)]);
        await app.StartAsync();
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("x-api-key", AdminKey);
        return (app, client);
    }

    /// <summary>A screen's description comes back whole, with each column's kind by its wire name.</summary>
    [Test]
    public async Task AScreensDescription_ComesBack()
    {
        var (app, client) = await StartAsync(new FakeScreen("usage"));
        await using var _ = app;

        var reply = await client.GetFromJsonAsync<ScreensReply>("/v1/admin/screens", AdminJson.SnakeCase);

        var screen = reply!.Screens.Single();
        Assert.That(screen.Name, Is.EqualTo("usage"));
        Assert.That(screen.Title, Is.EqualTo("The usage"));
        Assert.That(screen.Description, Is.EqualTo("The usage screen"));
        Assert.That(screen.Searchable, Is.True);
        Assert.That(screen.Columns.Select(c => c.Kind), Is.EqualTo(new[] { "text", "number" }));
    }

    /// <summary>With no pack offering one, the list is empty, not an error.</summary>
    [Test]
    public async Task WithNoScreens_TheListIsEmpty()
    {
        var (app, client) = await StartAsync();
        await using var _ = app;

        var reply = await client.GetFromJsonAsync<ScreensReply>("/v1/admin/screens", AdminJson.SnakeCase);

        Assert.That(reply!.Screens, Is.Empty);
    }

    /// <summary>The rows come back, and the search and the page reach the screen; unknown stays null.</summary>
    [Test]
    public async Task TheRowsComeBack_AndTheSearchAndPageReachTheScreen()
    {
        var screen = new FakeScreen("usage", new ScreenRow("a", ["Apple", null]));
        var (app, client) = await StartAsync(screen);
        await using var _ = app;

        var reply = await client.GetFromJsonAsync<ScreenPageReply>("/v1/admin/screens/usage/rows?search=app&page=3",
            AdminJson.SnakeCase);

        Assert.That(screen.LastQuery, Is.EqualTo(new ScreenQuery("app", 3)));
        Assert.That(reply!.Total, Is.EqualTo(1));
        Assert.That(reply.PageSize, Is.EqualTo(ScreenQuery.PageSize));
        Assert.That(reply.Rows.Single().Id, Is.EqualTo("a"));
        Assert.That(reply.Rows.Single().Cells, Is.EqualTo(new string?[] { "Apple", null }));
    }

    /// <summary>A page before the first is the first, and no search is no search.</summary>
    [TestCase("", null, 1)]
    [TestCase("?page=0", null, 1)]
    [TestCase("?page=-4&search=x", "x", 1)]
    public async Task APageBeforeTheFirst_IsTheFirst(string query, string? search, int page)
    {
        var screen = new FakeScreen("usage");
        var (app, client) = await StartAsync(screen);
        await using var _ = app;

        await client.GetAsync("/v1/admin/screens/usage/rows" + query);

        Assert.That(screen.LastQuery, Is.EqualTo(new ScreenQuery(search, page)));
    }

    /// <summary>A screen no pack offers says so.</summary>
    [Test]
    public async Task AnUnknownScreen_IsNotFound()
    {
        var (app, client) = await StartAsync(new FakeScreen("usage"));
        await using var _ = app;

        var reply = await client.GetAsync("/v1/admin/screens/nothing/rows");

        Assert.That(reply.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await reply.Content.ReadAsStringAsync(), Does.Contain("nothing"));
    }

    /// <summary>No key, no screens.</summary>
    [TestCase("/v1/admin/screens")]
    [TestCase("/v1/admin/screens/usage/rows")]
    public async Task WithoutTheAdminKey_ItIsRefused(string address)
    {
        var (app, _) = await StartAsync(new FakeScreen("usage"));
        await using var __ = app;

        var noKey = await app.GetTestClient().GetAsync(address);

        Assert.That(noKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
