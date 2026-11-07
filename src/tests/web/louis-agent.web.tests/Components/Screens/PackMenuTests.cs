using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Imports;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Components.Screens;
using LouisAgent.Web.Components.Screens.Table;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Screens;

/// <summary>
/// The sidebar's link for each screen the packs offer, so a new screen appears with no change to any site. Without the
/// key it's quiet: the page you're on already says what's wrong.
/// </summary>
public class PackMenuTests
{
    private static readonly ScreenColumnReply Name = new("Name", "text");

    private static readonly ScreensReply TwoScreens = new(
    [
        new ScreenReply("usage", "Usage", "What was used.", [Name], Searchable: false),
        new ScreenReply("visits", "Visits", "Who came.", [Name], Searchable: false)
    ]);

    /// <summary>What the fake API says: the list, or a refusal until a key is sent.</summary>
    private sealed class FakeScreensApi(ScreensReply reply)
    {
        public bool Refuses { get; set; }

        /// <summary>How many uploads the packs take.</summary>
        public int Uploads { get; init; }

        /// <summary>Answers the list of screens or of uploads.</summary>
        public HttpResponseMessage Answer(HttpRequestMessage request)
        {
            if (Refuses)
                return new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = JsonContent.Create(new { }) };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = request.RequestUri!.AbsolutePath.EndsWith("/imports")
                    ? JsonContent.Create(new ImportsReply([.. Enumerable.Repeat(new ImportRow("prices", "", []), Uploads)]),
                        options: AdminJson.SnakeCase)
                    : JsonContent.Create(reply, options: AdminJson.SnakeCase)
            };
        }
    }

    /// <summary>The menu over that API, and the store a key is saved through.</summary>
    private static (IRenderedComponent<PackMenu> Menu, ChatStore Store) Render(BunitContext ctx, FakeScreensApi api,
        PackViews? views = null)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var store = new ChatStore(ctx.JSInterop.JSRuntime, settings);
        ctx.Services.AddSingleton(store);
        ctx.Services.AddSingleton(views ?? new PackViews());
        ctx.Services.AddSingleton(new AgentApi(
            new HttpClient(new StubHandler(api.Answer)) { BaseAddress = new Uri("http://localhost/") }, settings));
        return (ctx.Render<PackMenu>(), store);
    }

    /// <summary>A link per screen, in the packs' order, to the generic page.</summary>
    [Test]
    public void AScreen_GetsALink()
    {
        using var ctx = new BunitContext();

        var (menu, _) = Render(ctx, new FakeScreensApi(TwoScreens));

        menu.WaitForAssertion(() => Assert.That(menu.FindAll("a.side-link"), Has.Count.EqualTo(2)),
            TimeSpan.FromSeconds(10));
        var links = menu.FindAll("a.side-link");
        Assert.That(links.Select(link => link.GetAttribute("href")),
            Is.EqualTo(new[] { "admin/screens/usage", "admin/screens/visits" }));
        Assert.That(links.Select(link => link.TextContent), Is.EqualTo(new[] { "Usage", "Visits" }));
    }

    /// <summary>Uploads gets its link when a pack takes one, and none when no pack does.</summary>
    [Test]
    public void Uploads_ShowsOnlyWhenAPackTakesOne()
    {
        using var with = new BunitContext();
        using var without = new BunitContext();

        var (taking, _) = Render(with, new FakeScreensApi(TwoScreens) { Uploads = 1 });
        var (none, _) = Render(without, new FakeScreensApi(TwoScreens));

        taking.WaitForAssertion(() => Assert.That(taking.FindAll("a.side-link").Select(link => link.GetAttribute("href")),
            Is.EqualTo(new[] { "admin/screens/usage", "admin/screens/visits", "admin/imports" })), TimeSpan.FromSeconds(10));
        none.WaitForAssertion(() => Assert.That(none.FindAll("a.side-link"), Has.Count.EqualTo(2)), TimeSpan.FromSeconds(10));
    }

    /// <summary>A pack's own-view screen is listed only by a site that carries the view.</summary>
    [Test]
    public void AScreenWithNoColumns_IsListedOnlyWithItsView()
    {
        var mixed = new ScreensReply(
        [
            new ScreenReply("usage", "Usage", "", [Name], Searchable: false),
            new ScreenReply("own", "Own", "", [], Searchable: false)
        ]);
        using var without = new BunitContext();
        using var with = new BunitContext();

        var (plain, _) = Render(without, new FakeScreensApi(mixed));
        var (carrying, _) = Render(with, new FakeScreensApi(mixed), new PackViews().UseScreen<ScreenTable>("own"));

        plain.WaitForAssertion(() => Assert.That(plain.FindAll("a.side-link").Select(link => link.TextContent),
            Is.EqualTo(new[] { "Usage" })), TimeSpan.FromSeconds(10));
        carrying.WaitForAssertion(() => Assert.That(carrying.FindAll("a.side-link").Select(link => link.TextContent),
            Is.EqualTo(new[] { "Usage", "Own" })), TimeSpan.FromSeconds(10));
    }

    /// <summary>A name goes in the address escaped.</summary>
    [Test]
    public void AName_IsEscapedInTheAddress()
    {
        using var ctx = new BunitContext();
        var odd = new ScreensReply([new ScreenReply("a b", "Odd", "", [Name], Searchable: false)]);

        var (menu, _) = Render(ctx, new FakeScreensApi(odd));

        menu.WaitForAssertion(
            () => Assert.That(menu.Find("a.side-link").GetAttribute("href"), Is.EqualTo("admin/screens/a%20b")),
            TimeSpan.FromSeconds(10));
    }

    /// <summary>With no screens there's no heading left on its own.</summary>
    [Test]
    public void NoScreens_LeaveNoGap()
    {
        using var ctx = new BunitContext();

        var (menu, _) = Render(ctx, new FakeScreensApi(new ScreensReply([])));

        menu.WaitForState(() => menu.Markup.Trim().Length == 0, TimeSpan.FromSeconds(10));
        Assert.That(menu.Markup.Trim(), Is.Empty);
    }

    /// <summary>A refused key shows no links and no error, and a saved key fills it.</summary>
    [Test]
    public void ARefusedKey_ShowsNothing_UntilAKeyIsSaved()
    {
        using var ctx = new BunitContext();
        var api = new FakeScreensApi(TwoScreens) { Refuses = true };
        var (menu, store) = Render(ctx, api);
        Assert.That(menu.Markup.Trim(), Is.Empty);

        api.Refuses = false;
        menu.InvokeAsync(() => store.SaveAdminKeyAsync("the-key"));

        menu.WaitForAssertion(() => Assert.That(menu.FindAll("a.side-link"), Has.Count.EqualTo(2)),
            TimeSpan.FromSeconds(10));
    }

    /// <summary>Leaving the page stops listening, so an old menu never redraws.</summary>
    [Test]
    public void AMenuThatIsGone_StopsListening()
    {
        using var ctx = new BunitContext();
        var api = new FakeScreensApi(TwoScreens);
        var (menu, store) = Render(ctx, api);
        menu.WaitForAssertion(() => Assert.That(menu.FindAll("a.side-link"), Has.Count.EqualTo(2)),
            TimeSpan.FromSeconds(10));

        menu.Instance.Dispose();

        Assert.DoesNotThrowAsync(() => store.SaveAdminKeyAsync("the-key"));
    }
}
