using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Imports;
using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Components.Screens;
using LouisAgent.Web.Pages.Admin;
using LouisAgent.Web.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Pages.Admin;

/// <summary>
/// F144, Louis's pick of three, a packs page: the packs area's own page, what the site's packs offer, each screen with
/// what it shows and the files they take, each the way to its page; the packs' card on the home opens it.
/// </summary>
public class PacksTests
{
    private static readonly ScreenColumnReply Name = new("Name", "text");

    /// <summary>Two screens, one of them the site can't draw, and one upload.</summary>
    private static readonly ScreensReply Screens = new(
    [
        new ScreenReply("usage", "Usage", "What was used.", [Name], Searchable: false),
        new ScreenReply("own", "Own", "Drawn by its own view only.", [], Searchable: false)
    ]);

    private static readonly ImportsReply OneUpload = new([new ImportRow("prices", "The shop's price list.", [])]);

    /// <summary>The page over a stub API answering the screens and the uploads, or refusing both.</summary>
    private static IRenderedComponent<Packs> Render(BunitContext ctx, ScreensReply screens, ImportsReply uploads,
        HttpStatusCode status = HttpStatusCode.OK)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var stub = new StubHandler(request => new HttpResponseMessage(status)
        {
            Content = request.RequestUri!.AbsolutePath.EndsWith("/imports")
                ? JsonContent.Create(uploads, options: AdminJson.SnakeCase)
                : JsonContent.Create(screens, options: AdminJson.SnakeCase)
        });
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(stub) { BaseAddress = new Uri("http://localhost/") },
            settings));
        ctx.Services.AddSingleton(new PackViews());

        var page = ctx.Render<Packs>();
        page.WaitForState(() => page.FindAll(".packs-loading").Count == 0, TimeSpan.FromSeconds(10));
        return page;
    }

    /// <summary>Each screen the site draws and the uploads, by name and what they're for, each to its page.</summary>
    [Test]
    public void ThePage_ListsTheScreensAndTheUploads()
    {
        using var ctx = new BunitContext();

        var items = Render(ctx, Screens, OneUpload).FindAll(".packs-item");

        Assert.Multiple(() =>
        {
            Assert.That(items.Select(item => item.QuerySelector("a")!.GetAttribute("href")),
                Is.EqualTo(new[] { "admin/screens/usage", "admin/imports" }), "a screen it can't draw isn't offered");
            Assert.That(items[0].TextContent, Does.Contain("Usage").And.Contain("What was used."));
            Assert.That(items[1].TextContent, Does.Contain("Uploads").And.Contain("The shop's price list."));
        });
    }

    /// <summary>Packs that offer nothing leave the page saying so.</summary>
    [Test]
    public void NothingOffered_SaysSo()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, new ScreensReply([]), new ImportsReply([]));

        Assert.That(page.Find(".packs-none").TextContent, Does.Contain("No pack"));
    }

    /// <summary>A refused key asks for it, as every admin page does.</summary>
    [Test]
    public void ARefusedKey_AsksForIt()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, Screens, OneUpload, HttpStatusCode.Unauthorized);

        Assert.That(page.FindAll("input[type=password]"), Has.Count.EqualTo(1));
    }

    /// <summary>The packs' card counts what they offer and opens this page; refused, it stays quiet.</summary>
    [Test]
    public void TheCard_CountsWhatThePacksOffer_AndOpensThePage()
    {
        using var ctx = new BunitContext();
        using var refused = new BunitContext();
        Render(ctx, Screens, OneUpload);
        Render(refused, Screens, OneUpload, HttpStatusCode.Unauthorized);

        var card = ctx.Render<PacksCard>();
        var quiet = refused.Render<PacksCard>();

        card.WaitForAssertion(() => Assert.That(card.FindAll(".figure"), Has.Count.EqualTo(2)),
            TimeSpan.FromSeconds(10));
        quiet.WaitForAssertion(() => Assert.That(quiet.FindAll(".admin-card-body .muted"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        Assert.Multiple(() =>
        {
            Assert.That(card.Find(".admin-card-title").TextContent, Is.EqualTo("Packs"));
            Assert.That(card.FindAll(".figure-value").Select(value => value.TextContent),
                Is.EqualTo(new[] { "1", "1" }));
            Assert.That(card.FindAll(".figure-name").Select(name => name.TextContent),
                Is.EqualTo(new[] { "screens", "uploads" }));
            Assert.That(card.Find("a.admin-card-open").GetAttribute("href"), Is.EqualTo("admin/packs"));
        });
    }
}
