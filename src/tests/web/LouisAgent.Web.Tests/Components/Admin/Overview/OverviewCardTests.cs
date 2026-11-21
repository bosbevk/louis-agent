using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Overview;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Admin.Overview;
using LouisAgent.Web.Components.Areas.Overview;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Admin.Overview;

/// <summary>
/// F144: the overview's card on the admin's home, today's talks, their cost and the tools the site counts, and the way
/// to the whole overview; its link in the sidebar.
/// </summary>
public class OverviewCardTests
{
    /// <summary>Today, in dollars as the ledger charged them.</summary>
    private static readonly OverviewReply Today = new("USD",
        new OverviewPeriod(3, 0.012m, [new OverviewBar("AskFor", 2), new OverviewBar("SendLead", 1)]),
        new OverviewPeriod(4, 0.022m, []), [], [], []);

    /// <summary>A context over a stub API answering the overview with this, or with the status given.</summary>
    private static BunitContext Context(HttpStatusCode status = HttpStatusCode.OK) => Context(() => status);

    /// <summary>A context over a stub API answering the overview with the status it is given at the time.</summary>
    private static BunitContext Context(Func<HttpStatusCode> status)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        // The overview answers; anything else (the rates) is refused, so the cost stays as the ledger charged it.
        var stub = new StubHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/overview")
            ? new HttpResponseMessage(status()) { Content = JsonContent.Create(Today, options: AdminJson.SnakeCase) }
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        var api = new AgentApi(new HttpClient(stub) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new OverviewCounts([new CountedTool("SendLead", "leads")]));
        ctx.Services.AddSingleton(new AdminAccess());
        return ctx;
    }

    /// <summary>Today's talks, cost and the counted tool by its site's name, and Open to the overview.</summary>
    [Test]
    public void TheCard_ShowsToday_AndOpensTheOverview()
    {
        using var ctx = Context();

        var card = ctx.Render<OverviewCard>();

        card.WaitForAssertion(() => Assert.That(card.Find(".admin-card-body").TextContent, Does.Contain("$0.012")),
            TimeSpan.FromSeconds(10));
        string body = card.Find(".admin-card-body").TextContent;
        Assert.Multiple(() =>
        {
            Assert.That(card.Find(".admin-card-title").TextContent, Is.EqualTo("Overview"));
            Assert.That(body, Does.Contain("3").And.Contain("talks").And.Contain("1").And.Contain("leads"));
            Assert.That(body, Does.Not.Contain("AskFor"), "a tool the site doesn't count isn't shown");
            Assert.That(card.Find("a.admin-card-open").GetAttribute("href"), Is.EqualTo("admin/overview"));
        });
    }

    /// <summary>
    /// A refused overview leaves the card quiet, the way to the overview still there, and tells the admin's access, so
    /// the home asks for the key (F144).
    /// </summary>
    [Test]
    public void ARefusedOverview_LeavesTheCardQuiet_AndTellsTheHome()
    {
        using var ctx = Context(HttpStatusCode.Unauthorized);

        var card = ctx.Render<OverviewCard>();

        card.WaitForAssertion(() => Assert.That(card.FindAll(".admin-card-body .muted"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        Assert.Multiple(() =>
        {
            Assert.That(card.FindAll("a.admin-card-open"), Has.Count.EqualTo(1));
            Assert.That(ctx.Services.GetRequiredService<AdminAccess>().Error, Is.Not.Null);
        });
    }

    /// <summary>Once a key is saved, the card reads again and shows what it couldn't (F144).</summary>
    [Test]
    public void ASavedKey_ReadsTheCardAgain()
    {
        var status = HttpStatusCode.Unauthorized;
        using var ctx = Context(() => status);
        var card = ctx.Render<OverviewCard>();
        card.WaitForAssertion(() => Assert.That(card.FindAll(".admin-card-body .muted"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));

        status = HttpStatusCode.OK;
        card.InvokeAsync(() => ctx.Services.GetRequiredService<ChatStore>().SaveAdminKeyAsync("the-key"));

        card.WaitForAssertion(() => Assert.That(card.Find(".admin-card-body").TextContent, Does.Contain("$0.012")),
            TimeSpan.FromSeconds(10));
    }

    /// <summary>The overview's link in the admin's sidebar.</summary>
    [Test]
    public void ItsLink_GoesToTheOverview()
    {
        using var ctx = new BunitContext();

        var links = ctx.Render<OverviewLinks>();

        Assert.That(links.Find("a.side-link").GetAttribute("href"), Is.EqualTo("admin/overview"));
    }
}
