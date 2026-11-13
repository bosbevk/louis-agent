using System.Net;
using System.Net.Http.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Overview;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Areas.Overview;
using LouisAgent.Web.Pages.Admin;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Pages.Admin;

/// <summary>
/// F127.8, Louis's pick of three designs, A. Overview first: the day's and the month's figures (talks, cost, and each
/// tool the site counts, by its own name), the month's talks by part and by who answered, and the latest talks; a
/// refused key asks for it.
/// </summary>
public class OverviewTests
{
    private static readonly DateTimeOffset Evening = new(2026, 11, 12, 19, 0, 0, TimeSpan.Zero);

    /// <summary>An evening's overview, in dollars as the ledger charged them.</summary>
    private static readonly OverviewReply Evening3 = new("USD",
        new OverviewPeriod(3, 0.012m, [new OverviewBar("AskFor", 2), new OverviewBar("SendLead", 1)]),
        new OverviewPeriod(4, 0.022m, [new OverviewBar("AskFor", 2), new OverviewBar("SendLead", 1)]),
        [new OverviewBar("selling", 2), new OverviewBar("demos", 1), new OverviewBar("framework", 1)],
        [new OverviewBar("louis-agent-guide", 3), new OverviewBar("koringland-attorneys", 1)],
        [
            new OverviewTalk(Evening.AddMinutes(9), "/framework/packs", "framework", "louis-agent-guide", 1, null, []),
            new OverviewTalk(Evening.AddMinutes(2), "/pricing", "selling", "louis-agent-guide", 2, 0.008m,
                ["Offer", "AskFor", "SendLead"]),
        ]);

    /// <summary>The page over a stub API answering this, the site counting SendLead as leads.</summary>
    private static IRenderedComponent<Overview> Render(BunitContext ctx, HttpStatusCode status = HttpStatusCode.OK)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        // The overview answers; anything else (the rates) is refused, so the costs stay as the ledger charged them.
        var stub = new StubHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/overview")
            ? new HttpResponseMessage(status) { Content = JsonContent.Create(Evening3, options: AdminJson.SnakeCase) }
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        var api = new AgentApi(new HttpClient(stub) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new OverviewCounts([new CountedTool("SendLead", "leads")]));

        var page = ctx.Render<Overview>();
        page.WaitForState(() => page.FindAll(".overview-loading").Count == 0, TimeSpan.FromSeconds(10));
        return page;
    }

    /// <summary>
    /// Today and the month: talks, the cost, and the counted tool by its site's name; others not shown.
    /// </summary>
    [Test]
    public void TheFigures_ShowTalksCostAndTheCountedTools()
    {
        using var ctx = new BunitContext();

        var figures = Render(ctx).FindAll(".overview-figures").Select(period => period.TextContent).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(figures, Has.Count.EqualTo(2));
            Assert.That(figures[0], Does.Contain("Today").And.Contain("3").And.Contain("$0.012").And.Contain("leads"));
            Assert.That(figures[1], Does.Contain("This month").And.Contain("4").And.Contain("$0.022"));
            Assert.That(figures[0], Does.Not.Contain("AskFor"), "a tool the site doesn't count isn't shown");
        });
    }

    /// <summary>The month's talks by part and by who answered, and the latest talks with their page and cost.</summary>
    [Test]
    public void TheBarsAndTheLatestTalks_AreDrawn()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(page.FindAll(".overview-bars .bar-name").Select(name => name.TextContent),
                Is.EqualTo(new[] { "selling", "demos", "framework", "louis-agent-guide", "koringland-attorneys" }));
            var rows = page.FindAll(".latest-talks tbody tr");
            Assert.That(rows, Has.Count.EqualTo(2));
            Assert.That(rows[0].TextContent, Does.Contain("/framework/packs").And.Contain("unknown"),
                "an unpriced talk's cost is unknown, never 0 (ADR 0007)");
            Assert.That(rows[1].TextContent, Does.Contain("/pricing").And.Contain("$0.008").And.Contain("leads"));
        });
    }

    /// <summary>A refused key shows the box to enter one, as every admin page does.</summary>
    [Test]
    public void ARefusedKey_AsksForIt()
    {
        using var ctx = new BunitContext();

        var page = Render(ctx, HttpStatusCode.Unauthorized);

        Assert.That(page.FindAll(".admin-gate input"), Is.Not.Empty);
    }
}
